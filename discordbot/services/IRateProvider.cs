using System.Threading.Tasks;

namespace discordbot.services;

internal interface IRateProvider
{
    Task<decimal> GetRateAsync();
}
