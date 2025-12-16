namespace Spire.Bot;

public static class Config
{
    public static ushort BotCount { get; }
    public static string BotPrefix { get; }
    
    public static bool TrustServerCertificate { get; }
    
    public static string LobbyHost { get; }
    public static ushort LobbyPort { get; }
    public static string LobbyAddress => $"https://{LobbyHost}:{LobbyPort}";
    
    public static string GameHost { get; }
    public static ushort GamePort { get; }

    static Config()
    {
        var data = File.ReadAllText("Config.yaml");
        var deserializer = new YamlDotNet.Serialization.Deserializer();
        var config = deserializer.Deserialize<Dictionary<string, object>>(data);

        BotCount = Convert.ToUInt16(config["bot_count"]);
        BotPrefix = Convert.ToString(config["bot_prefix"])!;
        
        TrustServerCertificate = Convert.ToBoolean(config["trust_server_certificate"]);
        
        LobbyHost = Convert.ToString(config["lobby_host"])!;
        LobbyPort = Convert.ToUInt16(config["lobby_port"]);
        
        GameHost = Convert.ToString(config["game_host"])!;
        GamePort = Convert.ToUInt16(config["game_port"]);
    }
}
