using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;

namespace StockPriceLookUpService.Models
{
    public class StockPrice
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Symbol")]
        [JsonPropertyName("01. symbol")]
        public string Symbol { get; set; } = null!;

        [JsonPropertyName("02. open")]
        public string Open { get; set; } = null!;

        [JsonPropertyName("03. high")] 
        public string High { get; set; } = null!;

        [JsonPropertyName("04. low")]
        public string Low { get; set; } = null!;

        [JsonPropertyName("05. price")]
        public string Price { get; set; } = null!;

        [JsonPropertyName("06. volume")]
        public string Volume { get; set; } = null!;

        [JsonPropertyName("07. latest trading day")]
        public string LatestTradingDay { get; set; } = null!;

        [JsonPropertyName("08. previous close")]
        public string PreviousClose { get; set; } = null!;

        [JsonPropertyName("09. change")]
        public string Change { get; set; } = null!;

        [JsonPropertyName("10. change percent")]
        public string ChangePercent { get; set; } = null!;

        public override string ToString()
        {
            return $"{{ Symbol: {Symbol}, Open: {Open}, High: {High}, Low: {Low}, Price: {Price}, Volume: {Volume}, Latest Trading Day: {LatestTradingDay}, Previous Close: {PreviousClose}, Change: {Change}, Change Percent: {ChangePercent} }}";
        }

    }
}
