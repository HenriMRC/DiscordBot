using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using discordbot.utils;

namespace discordbot.services;

internal sealed class WiseRateProvider : IRateProvider
{
    private const string Url = "https://wise.com/gateway/v3/quotes";
    private const string Content = @"{""sourceAmount"":1000,""sourceCurrency"":""EUR"",""targetCurrency"":""BRL"",""guaranteedTargetAmount"":false,""type"":""REGULAR""}";

    private readonly HttpClient _client = new();

    public async Task<decimal> GetRateAsync()
    {
        using StringContent content = new(Content, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _client.PostAsync(Url, content);
        string responseBody = await response.Content.ReadAsStringAsync();

        using JsonDocument jsonDocument = JsonDocument.Parse(responseBody);
        dynamic jsonObject = jsonDocument.ToExpandoObject();
        return (decimal)jsonObject.rate;
    }
}
