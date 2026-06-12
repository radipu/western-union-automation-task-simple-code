using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Western_Union_Automation_Task_Simple_Code.Models
{
    public class Customer
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string SSN { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string AccountType { get; set; } = "Checking";
        public decimal InitialDeposit { get; set; }
        public string DOB { get; set; } = "";
        public string DebitCard { get; set; } = "";
        public string CVV { get; set; } = "";
    }
}