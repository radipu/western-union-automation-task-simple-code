using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Western_Union_Automation_Task_Simple_Code.Automation;
using Western_Union_Automation_Task_Simple_Code.Models;
using Western_Union_Automation_Task_Simple_Code.Services;

namespace Western_Union_Automation_Task_Simple_Code.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private List<Customer>? customers;
        private bool isRunning;

        public ObservableCollection<Customer> Customers { get; set; } = new();
        public ObservableCollection<string> LogMessages { get; set; } = new();

        private string csvPath = "";
        public string CsvPath
        {
            get => csvPath;
            set { csvPath = value; OnPropertyChanged(); }
        }

        private string status = "Ready";
        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(); }
        }

        public ICommand SelectCsvCommand { get; }
        public ICommand StartAutomationCommand { get; }

        public MainViewModel()
        {
            SelectCsvCommand = new RelayCommand(_ => SelectCsv());
            StartAutomationCommand = new RelayCommand(async _ => await StartAutomationAsync(), _ => !isRunning && customers != null && customers.Any());
        }

        private void SelectCsv()
        {
            var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
            if (dialog.ShowDialog() == true)
            {
                CsvPath = dialog.FileName;
                try
                {
                    customers = CsvReaderService.ReadCustomers(CsvPath);
                    Customers.Clear();
                    foreach (var c in customers) Customers.Add(c);
                    Status = $"Loaded {customers.Count} customers. Ready to start.";
                    LogMessages.Add($"Loaded CSV: {Path.GetFileName(CsvPath)}");
                    CommandManager.InvalidateRequerySuggested();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading CSV: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Status = "Load failed";
                }
            }
        }

        private async Task StartAutomationAsync()
        {
            if (customers == null || customers.Count == 0) return;

            isRunning = true;
            CommandManager.InvalidateRequerySuggested();
            Status = "Running automation...";
            LogMessages.Add("Start Automation clicked.");

            var allSteps = new List<AutomationStep>();

            try
            {
                var eurRate = await ExchangeRateService.GetUsdToEurRateAsync();
                LogMessages.Add($"USD/EUR rate fetched: {eurRate}");

                var automation = new ParaBankAutomation(msg => Application.Current.Dispatcher.Invoke(() =>
                {
                    LogMessages.Add(msg);
                }));

                try
                {
                    foreach (var cust in customers)
                    {
                        LogMessages.Add($"--- Starting for {cust.FirstName} {cust.LastName} ({cust.Username}) ---");
                        var steps = await automation.RunForCustomerAsync(cust, eurRate);
                        allSteps.AddRange(steps);
                        LogMessages.Add($"--- Finished {cust.FirstName} {cust.LastName} ({cust.Username}) ---");
                    }
                }
                finally
                {
                    automation.CloseBrowser();
                }

                var reportDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"ParaBank_Operator_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    InitialDirectory = !string.IsNullOrWhiteSpace(CsvPath) && Directory.Exists(Path.GetDirectoryName(CsvPath))
                        ? Path.GetDirectoryName(CsvPath)
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Title = "Choose where to save the ParaBank operator report"
                };

                if (reportDialog.ShowDialog() == true)
                {
                    string reportPath = reportDialog.FileName;
                    ExcelReportService.GenerateReport(customers, allSteps, reportPath, eurRate);
                    LogMessages.Add($"Excel report saved to: {reportPath}");
                    Status = "Completed. Report generated.";
                    MessageBox.Show($"Automation completed!\nReport saved to:\n{reportPath}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    LogMessages.Add("Report save cancelled by user.");
                    Status = "Completed. Report was not saved.";
                    MessageBox.Show("Automation completed, but the report was not saved.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessages.Add($"FATAL ERROR: {ex.Message}");
                Status = "Failed. See automation log.";
                MessageBox.Show($"Automation failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isRunning = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Func<object?, bool>? canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => canExecute == null || canExecute(parameter);
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
