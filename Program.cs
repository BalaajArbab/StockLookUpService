
using MongoDB.Driver;
using StockPriceLookUpService.Models;
using System.Text.Json;

namespace StockPriceLookUpService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------");
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.ConfigureSwaggerGen(setup =>
            {
                setup.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Stock Prices",
                    Version = "v1"
                });
            });

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            HttpClient client = new HttpClient() { BaseAddress = new Uri("https://www.alphavantage.co") };
            //string? alphavantageApiKey = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY");
            string? alphaVantageApiKey = "4IL79R6TL3K26YPM";

            CachingService cachingService = new CachingService();

            app.MapGet("/StockPrice", async (string? symbol) =>
            {
                if (string.IsNullOrEmpty(symbol))
                {
                    return "Symbol is required!!!";
                }

                symbol = symbol.ToUpper();

                // Check if the stock price is in the cache
                StockPrice? stockPrice = await cachingService.GetAsync(symbol);

                if (stockPrice != null)
                {
                    Console.WriteLine("Returning to caller from cache: " + stockPrice);
                    return stockPrice.ToString();
                }

                // If not in the cache, fetch from the API
                Console.WriteLine($"{symbol} not found in cache. Fetching from API.");
                string url = $"/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={alphaVantageApiKey}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode().WriteRequestToConsole();

                string responseBody = await response.Content.ReadAsStringAsync();

                var document = JsonDocument.Parse(responseBody);
                var innerJson = document.RootElement.GetProperty("Global Quote").GetRawText();

                stockPrice = JsonSerializer.Deserialize<StockPrice>(innerJson);

                // Save the stock price in the cache
                Console.WriteLine($"Saving to cache: {stockPrice}");
                await cachingService.CreateAsync(stockPrice);

                Console.WriteLine("Returning to caller: " + stockPrice);

                return stockPrice.ToString();
            });

            app.MapGet("/StockPriceHistorical", async (string? symbol) =>
            {
                string err = "";
                if (string.IsNullOrEmpty(symbol)) err += "Symbol missing ";
                if (!string.IsNullOrEmpty(err)) return err;


                string url = $"/query?function=TIME_SERIES_DAILY&outputsize=compact&symbol={symbol}&apikey={alphaVantageApiKey}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode().WriteRequestToConsole();

                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            });

            /*await Task.Run(async () =>
            {
                Console.WriteLine("Sending Heartbeat");
            }, );
*/
            /*await PeriodicAsync(async () =>
            {
                Console.WriteLine("Sending Heartbeat");
            });*/

            app.Run();
        }

        public static async Task PeriodicAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            TimeSpan interval = new TimeSpan(0, 0, 30);
            using PeriodicTimer timer = new(interval);
            while (true)
            {
                await action();
                await timer.WaitForNextTickAsync(cancellationToken);
            }
        }

    }

    static class Extensions
    {
        internal static void WriteRequestToConsole(this HttpResponseMessage response)
        {
            if (response is null)
            {
                return;
            }

            var request = response.RequestMessage;
            Console.Write($"{request?.Method} ");
            Console.Write($"{request?.RequestUri} ");
            Console.WriteLine($"HTTP/{request?.Version}");
        }
    }
}
