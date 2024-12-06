
using MongoDB.Driver;
using StockPriceLookUpService.Models;
using System;
using System.Net.Http;
using System.Reflection;
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

            HttpClient serviceRegistryClient = new HttpClient() { BaseAddress = new Uri("https://service-registry-cs4471.1p2lshm2wxjn.us-east.codeengine.appdomain.cloud/") };
            
            await Login(serviceRegistryClient);

            HttpClient alphavantageClient = new HttpClient() { BaseAddress = new Uri("https://www.alphavantage.co") };
            /*string? alphaVantageApiKey = "AXSEJJ5A1R7KCYV3";*/
            string? alphaVantageApiKey = "demo";

            CachingService cachingService = new CachingService();

            app.MapGet("/StockPrice", async (string? symbol) =>
            {
                if (string.IsNullOrEmpty(symbol))
                {
                    Console.WriteLine("No symbol provided.");
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
                HttpResponseMessage response = await alphavantageClient.GetAsync(url);
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
                HttpResponseMessage response = await alphavantageClient.GetAsync(url);
                response.EnsureSuccessStatusCode().WriteRequestToConsole();

                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            });


            app.MapGet("/", (HttpContext httpContext) =>
            {
                httpContext.Response.Redirect("/swagger/index.html");
            }).ExcludeFromDescription();

            await Register(serviceRegistryClient);

            Task.Run(async () =>
            {
                while (true)
                {
                    Console.WriteLine("Sending Heartbeat");
                    await Reregister(serviceRegistryClient);
                    await Task.Delay(15000);
                }
            });


            app.Run();
        }


        public static async Task<bool> Login(HttpClient httpClient)
        {
            Console.WriteLine("Logging in to the service registry");
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/login", new {username = "admin", password = "admin"});
            response.EnsureSuccessStatusCode().WriteRequestToConsole();

            string responseBody = await response.Content.ReadAsStringAsync();

            var document = JsonDocument.Parse(responseBody);
            var token = document.RootElement.GetProperty("accessToken").GetString();

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            /*httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");*/
            Console.WriteLine("Authorized to use service registry");

            return true;
        }

        public static async Task<bool> Register(HttpClient httpClient)
        {
            Console.WriteLine("Registering with the service registry");
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/register", new { serviceName = "MS1-StockLookUp", port = "443", description = "A Microservice to query stock pricing data based on ticker", version = "1.0", instanceId = "1", url = "https://stocklookupservice20241204134818.azurewebsites.net" });
            response.EnsureSuccessStatusCode().WriteRequestToConsole();

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody);

            Console.WriteLine("Registered with service registry");

            return true;
        }

        public static async Task<bool> Reregister(HttpClient httpClient)
        {
            Console.WriteLine("Sending heartbeat to Service Registry.");
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/reregister", new { serviceName = "MS1-StockLookUp", instanceId = "1" });
            response.EnsureSuccessStatusCode().WriteRequestToConsole();

            return true;
        }

        public static async Task<bool> Deregister(HttpClient httpClient)
        {
            Console.WriteLine("Degistering with the service registry");
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/degister", new { serviceName = "MS1-StockLookUp", instanceId = "1" });
            response.EnsureSuccessStatusCode().WriteRequestToConsole();

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody);

            Console.WriteLine("Degistered with service registry");

            return true;
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
