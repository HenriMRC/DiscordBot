using System.Text.Json;
using System.Threading.Tasks;

namespace discordbot.services;

internal sealed class WiseRateProvider : IRateProvider
{
    private readonly WiseClient _client = new();

    public async Task<decimal> GetRateAsync()
    {
        string response = await _client.Request();
        using JsonDocument jsonDocument = JsonDocument.Parse(response);
        dynamic jsonObject = jsonDocument.ToExpandoObject();
        return (decimal)jsonObject.rate;
    }
}
