using Godot;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Spire.Core;
using Spire.Core.Log;
using Spire.Core.Network;
using Spire.Protocol;
using Spire.Protocol.Game;
using Spire.Protocol.Game.Auth;
using Spire.Protocol.Lobby;
using Error = Godot.Error;

namespace Spire.Lobby;

public partial class Lobby : LoggableNode
{
    [Export] public required LineEdit DevIdInput;
    [Export] public required Button StartButton;
    [Export] private PackedScene CharacterSlotScene { get; set; } = null!;
    
    // private LobbyManager _lobbyManager = null!;
    
    // public Account? Account { get; private set; }
    private Account? _account;
    
    private const string ConfigFilePath = "user://lobby.cfg";
    private ConfigFile _config = new();

    public override void _Ready()
    {
        StartButton.Pressed += OnStartButtonPressed;
        
        Logger.LogInformation("Loading config file \"{ConfigFilePath}\"", ProjectSettings.GlobalizePath(ConfigFilePath));
        if (_config.Load(ConfigFilePath) == Error.Ok)
        {
            string lastDevId = (string)_config.GetValue("Dev", "LastId", "");
            if (lastDevId != "")
            {
                DevIdInput.Text = lastDevId;
            }
        }
    }

    private void OnStartButtonPressed()
    {
        _ = RequestDevAuthAsync(DevIdInput.Text);
        
        _config.SetValue("Dev", "LastId", DevIdInput.Text);
        _config.Save(ConfigFilePath);
    }

    private async Task RequestDevAuthAsync(string devId)
    {
        if (Config.Mode != Mode.Dev)
        {
            Logger.LogWarning("Dev mode is not enabled!");
            return;
        }
        
        var handler = new HttpClientHandler();
        if (Config.Mode == Mode.Dev)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        var credentials = ChannelCredentials.Create(
            ChannelCredentials.SecureSsl,
            CallCredentials.FromInterceptor((_, metadata) =>
            {
                if (_account?.Token is not null)
                    metadata.Add("authentication", _account!.Token);
            
                return Task.CompletedTask;
            }));

        var options = new GrpcChannelOptions
        {
            HttpHandler = handler,
            Credentials = credentials
        };
        var lobbyChannel = GrpcChannel.ForAddress(Config.LobbyAddress, options);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        
        try
        {
            var client = new DevAuth.DevAuthClient(lobbyChannel);
			
            var accountRequest = new GetDevAccountRequest
            {
                DevId = devId
            };
            var accountResponse = await client.GetDevAccountAsync(accountRequest, deadline: deadline);

            var tokenRequest = new GetDevTokenRequest
            {
                AccountId = accountResponse.AccountId
            };
            var tokenResponse = await client.GetDevTokenAsync(tokenRequest, deadline: deadline);
			
            _account = new DevAccount
            {
                AccountId = accountResponse.AccountId,
                Token = tokenResponse.Token,
                DevId = devId
            };
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to get dev account: {}", e.Message);
            return;
        }

        try
        {
            var client = new Characters.CharactersClient(lobbyChannel);

            var charactersResponse = await client.ListCharactersAsync(new Empty(), deadline: deadline);

            List<CharacterSlot> characterSlots = [];
            foreach (var characterData in charactersResponse.Characters)
            {
                var characterSlot = CharacterSlotScene.Instantiate<CharacterSlot>();
                characterSlot.Init(characterData);
                characterSlots.Add(characterSlot);
            }

            CallDeferred(MethodName.OnCharacterSlots, characterSlots);
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to get characters: {}", e.Message);
            return;
        }
    }

    private void OnCharacterSlots(List<CharacterSlot> characterSlots)
    {
        // Logger.LogInformation("Characters list response: {}", characters);
    }

    // private void OnAccountRequestCompleted()
    // {
    //     if (Config.Mode == Mode.Dev)
    //     {
    //         if (_lobbyManager.Account is DevAccount devAccount)
    //         {
    //             Logger.LogInformation("Dev account request completed: {DevId}, {AccountId}", devAccount.DevId, devAccount.AccountId);
    //         }
    //     }
    //
    //     Task.Run(async () =>
    //     {
    //         await NetworkManager.Session.ConnectAsync(Config.GameHost, Config.GamePort);
    //         
    //         var login = new LoginProtocol(new Login
    //         {
    //             Kind = Login.Types.Kind.Enter,
    //             Token = _lobbyManager.Account!.Token,
    //             CharacterId = ctx.Character!.Id
    //         });
    //         await NetworkManager.Session.LoginAsync(login);
    //     });
    // }
}
