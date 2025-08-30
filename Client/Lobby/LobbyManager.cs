using Godot;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Spire.Core;
using Spire.Core.Log;
using Spire.Protocol;
using Spire.Protocol.Lobby;

namespace Spire.Lobby;

public partial class LobbyManager : LoggableNode
{
    public Account? Account { get; private set; }
    
    private GrpcChannel _channel = null!;
    private Accountant.AccountantClient _client = null!;
    
    [Signal] public delegate void AccountRequestCompletedEventHandler();
    [Signal] public delegate void AccountRequestFailedEventHandler();
    
    public override void _Ready()
    {
        base._Ready();

        _channel = GrpcChannel.ForAddress(Config.LobbyAddress);
        _client = new Accountant.AccountantClient(_channel);
        Logger.LogInformation("GRPC client initialized.");
    }

    public async Task RequestDevAccountAsync(string devId)
    {
        try
        {
            var request = new DevAccountRequest
            {
                DevId = devId
            };

            var deadline = DateTime.UtcNow.AddSeconds(10);
            var response = await _client.GetDevAccountAsync(request, deadline: deadline);
            
            Account = new DevAccount
            {
                Id = response.Id.ToGuid(),
                DevId = devId
            };
            
            EmitSignalAccountRequestCompleted();
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to get dev account: {}", e.Message);
            EmitSignalAccountRequestFailed();
        }
    }
}
