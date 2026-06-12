using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Western_Union_Automation_Task_Simple_Code.Models;

namespace Western_Union_Automation_Task_Simple_Code.Automation
{
    public class ParaBankAutomation
    {
        private IWebDriver? driver;
        private readonly Action<string> logAction;

        public ParaBankAutomation(Action<string> log)
        {
            logAction = log;
        }

        private void EnsureBrowserStarted()
        {
            if (driver != null) return;

            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            logAction("Browser opened.");
        }

        public async Task<List<AutomationStep>> RunForCustomerAsync(Customer customer, decimal eurRate)
        {
            var steps = new List<AutomationStep>();

            try
            {
                EnsureBrowserStarted();

                // Step 1: Register
                await RegisterCustomer(customer, steps);
                // Step 2: Login - after registration, ParaBank is already logged in
                // Step 3: Open New Account
                string newAccountId = await OpenNewAccount(customer, steps);
                // Step 4: Request Loan
                await RequestLoan(customer, newAccountId, steps);
                // Step 5: Get Operations Report converted to EUR
                await AddOperationsReport(customer, steps, eurRate);
                // Step 6: Logout before moving to the next customer
                await Logout();

                foreach (var step in steps)
                {
                    step.DOB = customer.DOB;
                    step.DebitCard = customer.DebitCard;
                    step.CVV = customer.CVV;
                }
            }
            catch (Exception ex)
            {
                logAction($"ERROR for {customer.Username}: {ex.Message}");
                steps.Add(new AutomationStep
                {
                    Customer = $"{customer.FirstName} {customer.LastName}",
                    Step = "Automation Failed",
                    Details = ex.Message,
                    Timestamp = DateTime.Now,
                    DOB = customer.DOB,
                    DebitCard = customer.DebitCard,
                    CVV = customer.CVV
                });

                try
                {
                    await Logout();
                }
                catch
                {
                    
                }
            }

            return steps;
        }

        private async Task RegisterCustomer(Customer cust, List<AutomationStep> steps)
        {
            logAction($"Registering {cust.FirstName} {cust.LastName}...");
            driver!.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/index.htm");
            driver.FindElement(By.LinkText("Register")).Click();

            driver.FindElement(By.Id("customer.firstName")).SendKeys(cust.FirstName);
            driver.FindElement(By.Id("customer.lastName")).SendKeys(cust.LastName);
            driver.FindElement(By.Id("customer.address.street")).SendKeys(string.IsNullOrEmpty(cust.Address) ? "N/A" : cust.Address);
            driver.FindElement(By.Id("customer.address.city")).SendKeys(string.IsNullOrEmpty(cust.City) ? "N/A" : cust.City);
            driver.FindElement(By.Id("customer.address.state")).SendKeys(string.IsNullOrEmpty(cust.State) ? "N/A" : cust.State);
            driver.FindElement(By.Id("customer.address.zipCode")).SendKeys(string.IsNullOrEmpty(cust.ZipCode) ? "00000" : cust.ZipCode);
            driver.FindElement(By.Id("customer.phoneNumber")).SendKeys(string.IsNullOrEmpty(cust.PhoneNumber) ? "0000000000" : cust.PhoneNumber);
            driver.FindElement(By.Id("customer.ssn")).SendKeys(string.IsNullOrEmpty(cust.SSN) ? "000-00-0000" : cust.SSN);
            driver.FindElement(By.Id("customer.username")).SendKeys(cust.Username);
            driver.FindElement(By.Id("customer.password")).SendKeys(cust.Password);
            driver.FindElement(By.Id("repeatedPassword")).SendKeys(cust.Password);
            driver.FindElement(By.XPath("//input[@value='Register']")).Click();

            new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                .Until(d => d.PageSource.Contains("Welcome") || d.Url.Contains("overview"));

            steps.Add(new AutomationStep
            {
                Customer = $"{cust.FirstName} {cust.LastName}",
                Step = "Registration",
                Details = $"User {cust.Username} registered successfully",
                Timestamp = DateTime.Now
            });
            logAction("Registration completed.");
        }

        private async Task<string> OpenNewAccount(Customer cust, List<AutomationStep> steps)
        {
            logAction("Opening new account...");
            driver!.FindElement(By.LinkText("Open New Account")).Click();

            var accountTypeSelect = new SelectElement(driver.FindElement(By.Id("type")));
            string type = cust.AccountType.Equals("Savings", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
            accountTypeSelect.SelectByValue(type);

            var fromAccountSelect = new SelectElement(driver.FindElement(By.Id("fromAccountId")));
            fromAccountSelect.SelectByIndex(0);

            driver.FindElement(By.XPath("//input[@value='Open New Account']")).Click();

            var newAccountElement = new WebDriverWait(driver, TimeSpan.FromSeconds(15))
                .Until(d =>
                {
                    var element = d.FindElement(By.Id("newAccountId"));
                    return string.IsNullOrWhiteSpace(element.Text) ? null : element;
                });
            string newAccountId = newAccountElement.Text.Trim();

            steps.Add(new AutomationStep
            {
                Customer = $"{cust.FirstName} {cust.LastName}",
                Step = "Open New Account",
                Details = $"Opened {cust.AccountType} account. ID: {newAccountId}",
                Timestamp = DateTime.Now,
                AmountUSD = cust.InitialDeposit
            });
            logAction($"New account opened: {newAccountId}");
            return newAccountId;
        }

        private async Task RequestLoan(Customer cust, string accountId, List<AutomationStep> steps)
        {
            logAction($"Requesting loan of $10000 with down payment 20% of ${cust.InitialDeposit}...");
            new WebDriverWait(driver!, TimeSpan.FromSeconds(15))
                .Until(d => d.FindElement(By.LinkText("Request Loan"))).Click();

            var loanAmountField = driver!.FindElement(By.Id("amount"));
            loanAmountField.Clear();
            loanAmountField.SendKeys("10000");

            var downPaymentField = driver.FindElement(By.Id("downPayment"));
            decimal downPayment = cust.InitialDeposit * 0.2m;
            downPaymentField.Clear();
            downPaymentField.SendKeys(downPayment.ToString("F2"));

            var accountSelect = new SelectElement(driver.FindElement(By.Id("fromAccountId")));
            try
            {
                accountSelect.SelectByValue(accountId);
            }
            catch
            {
                accountSelect.SelectByIndex(0);
            }

            driver.FindElement(By.XPath("//input[@value='Apply Now']")).Click();

            string resultMessage;
            try
            {
                resultMessage = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                    .Until(d => d.FindElement(By.Id("loanStatus"))).Text;
            }
            catch
            {
                resultMessage = "Loan processed (status not captured)";
            }

            steps.Add(new AutomationStep
            {
                Customer = $"{cust.FirstName} {cust.LastName}",
                Step = "Request Loan",
                Details = $"Loan $10000, Down payment ${downPayment:F2}. Result: {resultMessage}",
                Timestamp = DateTime.Now,
                AmountUSD = 10000
            });
            logAction($"Loan request result: {resultMessage}");
        }

        private async Task AddOperationsReport(Customer cust, List<AutomationStep> steps, decimal eurRate)
        {
            foreach (var step in steps.Where(s => s.AmountUSD.HasValue))
            {
                step.AmountEUR = Math.Round(step.AmountUSD!.Value * eurRate, 2);
            }

            driver!.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/overview.htm");
            steps.Add(new AutomationStep
            {
                Customer = $"{cust.FirstName} {cust.LastName}",
                Step = "Operations Report in EUR",
                Details = "All amounts converted using live exchange rate. Summary snapshot taken.",
                Timestamp = DateTime.Now
            });
            logAction("Added EUR conversion for all financial steps.");
        }

        private async Task Logout()
        {
            logAction("Logging out...");
            try
            {
                driver!.FindElement(By.LinkText("Log Out")).Click();
            }
            catch
            {
                // Already logged out or link not available.
            }
        }

        public void CloseBrowser()
        {
            if (driver == null) return;

            try
            {
                driver.Quit();
                driver.Dispose();
                logAction("Browser closed.");
            }
            finally
            {
                driver = null;
            }
        }
    }
}
