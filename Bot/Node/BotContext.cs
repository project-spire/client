using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spire.Core.BehaviorTree;

namespace Spire.Bot.Node;

public class BotContext : IContext
{
    private readonly ILogger<BotContext> _logger;
    
    public ushort BotId { get; }
    public Account? Account { get; set; }
    
    public BotContext(ushort botId)
    {
        BotId = botId;
    }

    public async ValueTask<JsonElement?> Request(Dictionary<string, string> data)
    {
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        using var client = new HttpClient(handler);

        try
        {
            var resp = await client.PostAsync(Settings.LobbyUrl, new StringContent(
                JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(body).RootElement;

            return root;
        }
        catch (Exception e)
        {
            _logger.LogError("Error requesting: {}", e.Message);
        }

        return null;
    }
}

public struct Account
{
    public long Id;
    public string Token;
}
