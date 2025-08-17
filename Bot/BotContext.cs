using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spire.Core;
using Spire.Core.BehaviorTree;
using Spire.Core.Network;

namespace Spire.Bot;

public class BotContext : INodeContext, ISessionContext
{
    private readonly TaskCompletionSource _stopped = new();
    
    public readonly ushort BotId;
    public readonly string DevId;
    public readonly ILogger<BotContext> Logger;
    
    public Session Session { get; }
    public Account? Account { get; set; }
    public Character? Character { get; set; }
    public Task Stopped => _stopped.Task;
    
    public BotContext(ushort botId, ILogger<BotContext> logger)
    {
        BotId = botId;
        DevId = $"{Config.BotPrefix}_{BotId:D5}";
        Logger = logger;
        
        Session = new Session(_ => this, Logger);
    }

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

    public void Stop(Exception? e = null)
    {
        if (e != null)
            Logger.LogError("Stopping: {message}", e.Message);
        
        Session.Stop();
        _stopped.TrySetResult();
    }

    public void HandleError(DispatchException e)
    {
        Logger.LogError("Error dispatching: {message}", e.Message);
        Stop();
    }
}
