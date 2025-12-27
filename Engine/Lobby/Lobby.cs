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
using Enum = System.Enum;
using Error = Godot.Error;

namespace Spire.Lobby;

public partial class Lobby : LoggableNode
{
    [Export] public required Control AccountLayer;
    [Export] public required LineEdit DevAccountInput;
    [Export] public required Button AccountConnectButton;

    [Export] public required Control CharacterLayer;
    [Export] public required Button GameStartButton;
    [Export] public required Control CharacterSlotsContainer;
    [Export] public required PackedScene CharacterSlotScene;
    
    [Export] public required Button CharacterCreateButton;
    [Export] public required LineEdit CharacterCreateNameInput;
    [Export] public required OptionButton CharacterCreateRaceSelect;

    private GrpcChannel _lobbyChannel = null!;
    private Account? _account;
    
    private const string ConfigFilePath = "user://lobby.cfg";
    private ConfigFile _config = new();
    
    private DateTime LobbyRequestDeadline => DateTime.UtcNow.AddSeconds(10);

    public override void _Ready()
    {
        AccountConnectButton.Pressed += OnAccountConnectButtonPressed;
        CharacterCreateButton.Pressed += OnCharacterCreateButtonPressed;
        GameStartButton.Pressed += OnGameStartButtonPressed;
        
        Logger.LogInformation("Loading config file \"{ConfigFilePath}\"", ProjectSettings.GlobalizePath(ConfigFilePath));
        if (_config.Load(ConfigFilePath) == Error.Ok)
        {
            string lastDevId = (string)_config.GetValue("Dev", "LastId", "");
            if (lastDevId != "")
            {
                DevAccountInput.Text = lastDevId;
            }
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
        _lobbyChannel = GrpcChannel.ForAddress(Config.LobbyAddress, options);
    }

    private void OnAccountConnectButtonPressed()
    {
        _ = RequestDevAuthAsync(DevAccountInput.Text);
        
        _config.SetValue("Dev", "LastId", DevAccountInput.Text);
        _config.Save(ConfigFilePath);
    }

    private void OnGameStartButtonPressed()
    {
        
    }

    private void OnCharacterCreateButtonPressed()
    {
        var name = CharacterCreateNameInput.Text;
        var raceString = CharacterCreateRaceSelect.Text;

        if (!Enum.TryParse<Race>(raceString, out var race))
        {
            Logger.LogError("Invalid race selected");
            return;
        }
        
        _ = RequestCreateCharacterAsync(name, race);
    }

    private async Task RequestDevAuthAsync(string devId)
    {
        if (Config.Mode != Mode.Dev)
        {
            Logger.LogWarning("Dev mode is not enabled!");
            return;
        }
        
        try
        {
            var client = new DevAuth.DevAuthClient(_lobbyChannel);
			
            var accountRequest = new GetDevAccountRequest
            {
                DevId = devId
            };
            var accountResponse = await client.GetDevAccountAsync(accountRequest, deadline: LobbyRequestDeadline);

            var tokenRequest = new GetDevTokenRequest
            {
                AccountId = accountResponse.AccountId
            };
            var tokenResponse = await client.GetDevTokenAsync(tokenRequest, deadline: LobbyRequestDeadline);
			
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

        await RequestListCharactersAsync();
    }

    private async Task RequestListCharactersAsync()
    {
        try
        {
            var client = new Characters.CharactersClient(_lobbyChannel);

            var charactersResponse = await client.ListCharactersAsync(new Empty(), deadline: LobbyRequestDeadline);

            List<CharacterSlot> characterSlots = [];
            foreach (var characterData in charactersResponse.Characters)
            {
                var characterSlot = CharacterSlotScene.Instantiate<CharacterSlot>();
                characterSlot.Init(characterData);
                characterSlots.Add(characterSlot);
            }

            CallDeferred(MethodName.OnCharacterSlots, characterSlots.ToArray());
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to list characters: {}", e.Message);
            return;
        }
    }

    private async Task RequestCreateCharacterAsync(string name, Race race)
    {
        Logger.LogInformation("Creating a character: name={name}, race={race}", name, race);
        
        try
        {
            var client = new Characters.CharactersClient(_lobbyChannel);

            var createRequest = new CreateCharacterRequest
            {
                Name = name,
                Race = race
            };
            var createResponse = await client.CreateCharacterAsync(createRequest, deadline: LobbyRequestDeadline);
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to create a character: {}", e.Message);
            throw;
        }
        
        // TODO: Just add a new character instead of requesting whole.
        await RequestListCharactersAsync();
    }
    
    private void OnCharacterSlots(CharacterSlot[] characterSlots)
    {
        foreach (var characterSlot in characterSlots)
        {
            CharacterSlotsContainer.AddChild(characterSlot);
        }
        
        AccountLayer.Hide();
        CharacterLayer.Show();
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
