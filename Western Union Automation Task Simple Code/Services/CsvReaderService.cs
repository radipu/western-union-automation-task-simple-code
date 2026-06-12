using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Western_Union_Automation_Task_Simple_Code.Models;

namespace Western_Union_Automation_Task_Simple_Code.Services
{
    public static class CsvReaderService
    {
        public static List<Customer> ReadCustomers(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim
            });

            var records = csv.GetRecords<dynamic>().ToList();
            var customers = new List<Customer>();

            foreach (var record in records)
            {
                var cust = new Customer
                {
                    FirstName = GetString(record, "FirstName", "First Name"),
                    LastName = GetString(record, "LastName", "Last Name"),
                    Address = GetString(record, "Address"),
                    City = GetString(record, "City"),
                    State = GetString(record, "State"),
                    ZipCode = GetString(record, "ZipCode", "Zip Code"),
                    PhoneNumber = GetString(record, "PhoneNumber", "Phone Number"),
                    SSN = GetString(record, "SSN"),
                    Username = GetString(record, "Username"),
                    Password = GetString(record, "Password"),
                    AccountType = DefaultIfBlank(GetString(record, "AccountType", "Account Type"), "Checking"),
                    InitialDeposit = ParseDecimal(GetString(record, "InitialDeposit", "Initial Deposit")),
                    DOB = GetString(record, "DOB"),
                    DebitCard = GetString(record, "DebitCard", "Debit Card"),
                    CVV = GetString(record, "CVV")
                };

                if (string.IsNullOrWhiteSpace(cust.Username))
                    cust.Username = $"{cust.FirstName}{cust.LastName}{DateTime.Now.Ticks}".ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(cust.Password))
                    cust.Password = "Password123!";

                customers.Add(cust);
            }
            return customers;
        }

        private static string GetString(dynamic record, params string[] fields)
        {
            if (record is IDictionary<string, object> dict)
            {
                foreach (var field in fields)
                {
                    if (dict.TryGetValue(field, out var value))
                        return value?.ToString()?.Trim() ?? "";

                    var key = dict.Keys.FirstOrDefault(k => string.Equals(Normalize(k), Normalize(field), StringComparison.OrdinalIgnoreCase));
                    if (key != null)
                        return dict[key]?.ToString()?.Trim() ?? "";
                }
            }
            return "";
        }

        private static string Normalize(string value) => value.Replace(" ", "").Replace("_", "").Trim();
        private static string DefaultIfBlank(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

        private static decimal ParseDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            input = input.Replace("$", "").Replace(",", "").Trim();
            decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
            return result;
        }
    }
}
