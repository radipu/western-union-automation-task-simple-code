using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Western_Union_Automation_Task_Simple_Code.Models
{
    public class AutomationStep
    {
        public string Customer { get; set; } = "";
        public string Step { get; set; } = "";
        public string Details { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public decimal? AmountUSD { get; set; }
        public decimal? AmountEUR { get; set; }
        public string DOB { get; set; } = "";
        public string DebitCard { get; set; } = "";
        public string CVV { get; set; } = "";
    }
}