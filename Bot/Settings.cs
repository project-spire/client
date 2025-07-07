namespace Spire.Bot;

public static class Settings
{
    public static ushort NumBots { get; }
    
    public static string LobbyHost { get; }
    public static ushort LobbyPort { get; }
    public static string LobbyUrl => $"https://{LobbyHost}:{LobbyPort}";
    
    public static string GameHost { get; }
    public static ushort GamePort { get; }

    static Settings()
    {
        var data = File.ReadAllText("settings.yaml");
        var deserializer = new YamlDotNet.Serialization.Deserializer();
        var settings = deserializer.Deserialize<Dictionary<string, object>>(data);

        NumBots = Convert.ToUInt16(settings["num_bots"]);
        
        LobbyHost = Convert.ToString(settings["lobby_host"])!;
        LobbyPort = Convert.ToUInt16(settings["lobby_port"]);
        
        GameHost = Convert.ToString(settings["game_host"])!;
        GamePort = Convert.ToUInt16(settings["game_port"]);
    }
}
