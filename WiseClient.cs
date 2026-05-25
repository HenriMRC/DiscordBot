using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace discordbot;

internal struct WiseClient
{
    private const string URL = "https://wise.com/gateway/v3/quotes";
    private const string CONTENT = @"{""sourceAmount"":1000,""sourceCurrency"":""EUR"",""targetCurrency"":""BRL"",""guaranteedTargetAmount"":false,""type"":""REGULAR""}";

    private readonly HttpClient _client;
    private readonly StringContent _content;

    public WiseClient()
    {
        _client = new();
        _content = new(CONTENT, Encoding.UTF8, "application/json");
    }

    internal readonly async Task<string> Request()
    {
        using HttpResponseMessage response = await _client.PostAsync(URL, _content);
        return await response.Content.ReadAsStringAsync();
    }
}
