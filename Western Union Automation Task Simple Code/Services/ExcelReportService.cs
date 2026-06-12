using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using Western_Union_Automation_Task_Simple_Code.Models;

namespace Western_Union_Automation_Task_Simple_Code.Services
{
    public static class ExcelReportService
    {
        public static void GenerateReport(List<Customer> customers, List<AutomationStep> steps, string outputPath, decimal eurRate)
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
            AddEntry(archive, "[Content_Types].xml", ContentTypesXml());
            AddEntry(archive, "_rels/.rels", RootRelsXml());
            AddEntry(archive, "xl/workbook.xml", WorkbookXml());
            AddEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelsXml());
            AddEntry(archive, "xl/styles.xml", StylesXml());
            AddEntry(archive, "xl/worksheets/sheet1.xml", OperationsWorksheetXml(customers, steps, eurRate));
            AddEntry(archive, "xl/worksheets/sheet2.xml", SummaryWorksheetXml(customers, steps, eurRate));
        }

        public static void GenerateReport(List<AutomationStep> steps, string outputPath)
        {
            var customers = steps
                .Select(s => new Customer
                {
                    FirstName = (s.Customer ?? "").Split(' ').FirstOrDefault() ?? "",
                    LastName = string.Join(" ", (s.Customer ?? "").Split(' ').Skip(1)),
                    DOB = s.DOB,
                    DebitCard = s.DebitCard,
                    CVV = s.CVV
                })
                .GroupBy(c => $"{c.FirstName} {c.LastName}".Trim())
                .Select(g => g.First())
                .ToList();

            GenerateReport(customers, steps, outputPath, 0m);
        }

        private static void AddEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }

        private static string ContentTypesXml() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "</Types>";

        private static string RootRelsXml() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";

        private static string WorkbookXml() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets>" +
            "<sheet name=\"Operations\" sheetId=\"1\" r:id=\"rId1\"/>" +
            "<sheet name=\"Summary\" sheetId=\"2\" r:id=\"rId2\"/>" +
            "</sheets></workbook>";

        private static string WorkbookRelsXml() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "</Relationships>";

        private static string StylesXml() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<numFmts count=\"3\">" +
            "<numFmt numFmtId=\"164\" formatCode=\"$#,##0.00\"/>" +
            "<numFmt numFmtId=\"165\" formatCode=\"€#,##0.00\"/>" +
            "<numFmt numFmtId=\"166\" formatCode=\"yyyy-mm-dd hh:mm\"/>" +
            "</numFmts>" +
            "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
            "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
            "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
            "<cellXfs count=\"9\">" +
            "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
            "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
            "<xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
            "<xf numFmtId=\"166\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
            "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
            "<xf numFmtId=\"164\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
            "<xf numFmtId=\"165\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
            "<xf numFmtId=\"166\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
            "<xf numFmtId=\"22\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
            "</cellXfs>" +
            "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
            "</styleSheet>";

        private static string OperationsWorksheetXml(List<Customer> customers, List<AutomationStep> steps, decimal eurRate)
        {
            string[] headers =
            {
                "CSV Row", "Customer Name", "Username", "Account Type", "Initial Deposit USD", "Initial Deposit EUR",
                "Loan Amount USD", "Loan Amount EUR", "Down Payment USD", "Down Payment EUR", "Opened Account",
                "Loan Requested", "Loan Status", "DOB", "Debit Card", "CVV", "Automation Status", "Notes", "Processed At"
            };

            var rows = new List<string> { RowXml(1, headers.Select((h, i) => TextCell(1, i + 1, h, 4)).ToArray()) };
            var processedAt = DateTime.Now;

            for (int i = 0; i < customers.Count; i++)
            {
                var cust = customers[i];
                var customerName = $"{cust.FirstName} {cust.LastName}".Trim();
                var custSteps = steps.Where(s => string.Equals(s.Customer, customerName, StringComparison.OrdinalIgnoreCase)).ToList();
                var failedStep = custSteps.FirstOrDefault(s => s.Step.Equals("Automation Failed", StringComparison.OrdinalIgnoreCase));
                var openStep = custSteps.FirstOrDefault(s => s.Step.Equals("Open New Account", StringComparison.OrdinalIgnoreCase));
                var loanStep = custSteps.FirstOrDefault(s => s.Step.Equals("Request Loan", StringComparison.OrdinalIgnoreCase));

                decimal initialUsd = cust.InitialDeposit;
                decimal loanUsd = 10000m;
                decimal downPaymentUsd = Math.Round(cust.InitialDeposit * 0.20m, 2);
                int rowNumber = i + 2;
                var notes = BuildNotes(failedStep, cust.DOB);

                rows.Add(RowXml(rowNumber, new[]
                {
                    NumberCell(rowNumber, 1, i + 2),
                    TextCell(rowNumber, 2, customerName),
                    TextCell(rowNumber, 3, cust.Username),
                    TextCell(rowNumber, 4, cust.AccountType),
                    NumberCell(rowNumber, 5, initialUsd, 1),
                    NumberCell(rowNumber, 6, initialUsd * eurRate, 2),
                    NumberCell(rowNumber, 7, loanUsd, 1),
                    NumberCell(rowNumber, 8, loanUsd * eurRate, 2),
                    NumberCell(rowNumber, 9, downPaymentUsd, 1),
                    NumberCell(rowNumber, 10, downPaymentUsd * eurRate, 2),
                    TextCell(rowNumber, 11, ExtractAccountId(openStep?.Details)),
                    TextCell(rowNumber, 12, loanStep == null ? "No" : "Yes"),
                    TextCell(rowNumber, 13, ExtractLoanStatus(loanStep?.Details)),
                    TextCell(rowNumber, 14, NormalizeDateText(cust.DOB)),
                    TextCell(rowNumber, 15, cust.DebitCard),
                    TextCell(rowNumber, 16, cust.CVV),
                    TextCell(rowNumber, 17, failedStep == null ? "Completed" : "Failed"),
                    TextCell(rowNumber, 18, notes),
                    DateCell(rowNumber, 19, processedAt, 3)
                }));
            }

            int lastRow = Math.Max(customers.Count + 1, 2);
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                   "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                   "<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
                   OperationsColumnsXml() +
                   "<sheetData>" + string.Join("", rows) + "</sheetData>" +
                   $"<autoFilter ref=\"A1:S{lastRow}\"/>" +
                   "</worksheet>";
        }

        private static string SummaryWorksheetXml(List<Customer> customers, List<AutomationStep> steps, decimal eurRate)
        {
            int completed = customers.Count(c => !steps.Any(s => s.Customer == $"{c.FirstName} {c.LastName}".Trim() && s.Step == "Automation Failed"));
            int failed = customers.Count - completed;
            int loanAttempts = customers.Count(c => steps.Any(s => s.Customer == $"{c.FirstName} {c.LastName}".Trim() && s.Step == "Request Loan"));
            int accountsOpened = customers.Count(c => steps.Any(s => s.Customer == $"{c.FirstName} {c.LastName}".Trim() && s.Step == "Open New Account"));

            var rows = new List<string>
            {
                RowXml(1, new[] { TextCell(1, 1, "ParaBank Automation Summary", 4) }),
                RowXml(3, new[] { TextCell(3, 1, "Total records"), NumberCell(3, 2, customers.Count) }),
                RowXml(4, new[] { TextCell(4, 1, "Completed"), NumberCell(4, 2, completed) }),
                RowXml(5, new[] { TextCell(5, 1, "Validation / Automation Failed"), NumberCell(5, 2, failed) }),
                RowXml(6, new[] { TextCell(6, 1, "Loan requests attempted"), NumberCell(6, 2, loanAttempts) }),
                RowXml(7, new[] { TextCell(7, 1, "Accounts opened"), NumberCell(7, 2, accountsOpened) }),
                RowXml(8, new[] { TextCell(8, 1, "USD/EUR rate used"), NumberCell(8, 2, eurRate) }),
                RowXml(9, new[] { TextCell(9, 1, "Generated at"), DateCell(9, 2, DateTime.Now, 3) })
            };

            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                   "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                   "<cols><col min=\"1\" max=\"1\" width=\"30\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"18\" customWidth=\"1\"/></cols>" +
                   "<sheetData>" + string.Join("", rows) + "</sheetData>" +
                   "<mergeCells count=\"1\"><mergeCell ref=\"A1:B1\"/></mergeCells>" +
                   "</worksheet>";
        }

        private static string OperationsColumnsXml() =>
            "<cols>" +
            Col(1, 11) + Col(2, 18) + Col(3, 14) + Col(4, 16) + Col(5, 20) + Col(6, 20) +
            Col(7, 18) + Col(8, 18) + Col(9, 20) + Col(10, 20) + Col(11, 16) + Col(12, 15) +
            Col(13, 30) + Col(14, 16) + Col(15, 24) + Col(16, 10) + Col(17, 18) + Col(18, 60) + Col(19, 20) +
            "</cols>";

        private static string Col(int index, int width) => $"<col min=\"{index}\" max=\"{index}\" width=\"{width}\" customWidth=\"1\"/>";

        private static string BuildNotes(AutomationStep? failedStep, string dob)
        {
            var notes = failedStep == null ? "Automation completed for this customer." : $"Automation failed: {failedStep.Details}";
            if (!string.IsNullOrWhiteSpace(dob) && NormalizeDateText(dob) == dob && !CanParseDate(dob))
                notes += $" DOB '{dob}' could not be converted into a valid date.";
            return notes;
        }

        private static string ExtractAccountId(string? details)
        {
            if (string.IsNullOrWhiteSpace(details)) return "";
            var match = Regex.Match(details, @"ID:\s*([^\s]+)");
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        private static string ExtractLoanStatus(string? details)
        {
            if (string.IsNullOrWhiteSpace(details)) return "Not attempted";
            var match = Regex.Match(details, @"Result:\s*(.*)$");
            var status = match.Success ? match.Groups[1].Value.Trim() : details.Trim();
            return string.IsNullOrWhiteSpace(status) ? "Processed" : status;
        }

        private static bool CanParseDate(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                   DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out _);
        }

        private static string NormalizeDateText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            string[] formats = { "MM-dd-yy", "MM/dd/yy", "MM-dd-yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "MMM d, yyyy", "MMMM d, yyyy" };
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date.ToString("yyyy-MM-dd");
            if (DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return date.ToString("yyyy-MM-dd");
            return value;
        }

        private static string RowXml(int rowNumber, string[] cells) => $"<row r=\"{rowNumber}\">{string.Join("", cells)}</row>";

        private static string TextCell(int row, int col, string? value, int style = 0)
        {
            var text = SecurityElement.Escape(value ?? "") ?? "";
            var styleAttr = style > 0 ? $" s=\"{style}\"" : "";
            return $"<c r=\"{CellRef(row, col)}\" t=\"inlineStr\"{styleAttr}><is><t>{text}</t></is></c>";
        }

        private static string NumberCell(int row, int col, decimal? value, int style = 0)
        {
            if (!value.HasValue) return TextCell(row, col, "", style);
            var styleAttr = style > 0 ? $" s=\"{style}\"" : "";
            return $"<c r=\"{CellRef(row, col)}\"{styleAttr}><v>{value.Value.ToString(CultureInfo.InvariantCulture)}</v></c>";
        }

        private static string NumberCell(int row, int col, int value, int style = 0)
        {
            var styleAttr = style > 0 ? $" s=\"{style}\"" : "";
            return $"<c r=\"{CellRef(row, col)}\"{styleAttr}><v>{value}</v></c>";
        }

        private static string DateCell(int row, int col, DateTime value, int style)
        {
            var styleAttr = style > 0 ? $" s=\"{style}\"" : "";
            return $"<c r=\"{CellRef(row, col)}\"{styleAttr}><v>{ToExcelSerialDate(value).ToString(CultureInfo.InvariantCulture)}</v></c>";
        }

        private static double ToExcelSerialDate(DateTime date)
        {
            return date.ToOADate();
        }

        private static string CellRef(int row, int col) => $"{ColumnName(col)}{row}";

        private static string ColumnName(int col)
        {
            var name = "";
            while (col > 0)
            {
                col--;
                name = (char)('A' + (col % 26)) + name;
                col /= 26;
            }
            return name;
        }
    }
}
