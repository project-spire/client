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
	
	[Signal] public delegate void AccountRequestCompletedEventHandler();
	[Signal] public delegate void AccountRequestFailedEventHandler();

	public async Task RequestDevAuthAsync(string devId)
	{
		if (Config.Mode != Mode.Dev)
		{
			Logger.LogWarning("Dev mode is not enabled!");
			return;
		} 
		
		try
		{
			var client = GetDevAuthClient();
			
			var request = new DevAccountRequest
			{
				DevId = devId
			};

			var deadline = DateTime.UtcNow.AddSeconds(10);
			var response = await client.GetDevAccountAsync(request, deadline: deadline);
			
			Account = new DevAccount
			{
				AccountId = response.AccountId,
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

	private DevAuth.DevAuthClient GetDevAuthClient()
	{
		var options = new GrpcChannelOptions
		{
			HttpHandler = new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (_, _, _, _) => true
			}
		};
		var channel = GrpcChannel.ForAddress(Config.LobbyAddress, options);
		
		return new DevAuth.DevAuthClient(channel);
	}
}
