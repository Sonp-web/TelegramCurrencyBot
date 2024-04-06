using System.Text.Json.Nodes;

namespace ConsoleApp19;

public static class CoinMarket
{
    private static readonly string API_KEY = ApiConstants.COIN_MARKET_API;

    public static async Task<decimal> GetPriceAsync(string currecyCode)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY",API_KEY);
            var response = await httpClient.GetAsync(
                $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/quotes/latest?symbol={currecyCode}&convert=USD");
            var responseString = await response.Content.ReadAsStreamAsync();
            var jsonResponce = JsonNode.Parse(responseString);
            var price = (decimal)jsonResponce["data"][currecyCode]["quote"]["USD"]["price"];
            return price;
        }
    }
}