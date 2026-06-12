using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Western_Union_Automation_Task_Simple_Code.Services
{
    public static class ExchangeRateService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string ApiUrl = "https://api.exchangerate-api.com/v4/latest/USD";

        public static async Task<decimal> GetUsdToEurRateAsync()
        {
            try
            {
                var response = await httpClient.GetStringAsync(ApiUrl);
                using var doc = JsonDocument.Parse(response);
                var rates = doc.RootElement.GetProperty("rates");
                if (rates.TryGetProperty("EUR", out var eurRate))
                    return eurRate.GetDecimal();
            }
            catch
            {
                // Offline/network fallback.
            }
            return 0.92m;
        }
    }
}
