using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spire.Core;
using Spire.Core.BehaviorTree;

namespace Spire.Bot;

public class BotContext(ushort botId, ILogger<BotContext> logger) : INodeContext
{
    public readonly ushort BotId = botId;
    public string DevId => $"{Settings.DevIdPrefix}_{BotId:D5}";
    public Account? Account { get; set; }
    public Character? Character { get; set; }
    
    public readonly ILogger<BotContext> Logger = logger;

    public async ValueTask<JsonElement> Request(string url, string data, bool authorization = true)
    {
        Logger.LogInformation("Requesting to {url}", url);
        
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        
        using var client = new HttpClient(handler);
        if (authorization && Account != null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Account.Token);
        }

        try
        {
            var content = new StringContent(data, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(body).RootElement;

            return root;
        }
        catch (Exception e)
        {
            Logger.LogError("Error requesting to {url}: {message}", url, e.Message);
            throw;
        }
    }
}
