using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Options;
using StockPriceLookUpService.Models;

namespace StockPriceLookUpService
{
    public class CachingService
    {
        static readonly string connectionString = "mongodb+srv://fusingorb:nAv2NddrouorfF72@cluster0.xy4vg.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";
        static readonly string databaseName = "Stocks";
        static readonly string stockPricesCollectionName = "StockPrices";

        private readonly IMongoCollection<StockPrice> _stockPricesCollection;

        public CachingService()
        {
            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(databaseName);

            _stockPricesCollection = mongoDatabase.GetCollection<StockPrice>(stockPricesCollectionName);
        }

        public async Task<StockPrice?> GetAsync(string symbol) {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var yesterday = DateTime.Now.Subtract(TimeSpan.FromDays(1)).ToString("yyyy-MM-dd");

            var filter = Builders<StockPrice>.Filter.Or([
                Builders<StockPrice>.Filter.And(
                    Builders<StockPrice>.Filter.Eq(sp => sp.Symbol, symbol),
                    Builders<StockPrice>.Filter.Eq(sp => sp.LatestTradingDay, yesterday)),
                Builders<StockPrice>.Filter.And(
                    Builders<StockPrice>.Filter.Eq(sp => sp.Symbol, symbol),
                    Builders<StockPrice>.Filter.Eq(sp => sp.LatestTradingDay, date))
            ]);

            return await _stockPricesCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(StockPrice stock) =>
        await _stockPricesCollection.InsertOneAsync(stock);

    }

    
}
