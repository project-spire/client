using YamlDotNet.Serialization;

namespace Spire.Core;

public static class Config
{
    public static Mode Mode { get; }
    
    public static string LobbyHost { get; }
    public static ushort LobbyPort { get; }
    public static string LobbyAddress => $"https://{LobbyHost}:{LobbyPort}";
    
    public static string GameHost { get; }
    public static ushort GamePort { get; }

    static Config()
    {
        var data = File.ReadAllText("Config.yaml");
        var deserializer = new Deserializer();
        var config = deserializer.Deserialize<Dictionary<string, object>>(data);
        
        LobbyHost = Convert.ToString(config["lobby_host"])!;
        LobbyPort = Convert.ToUInt16(config["lobby_port"]);
        
        GameHost = Convert.ToString(config["game_host"])!;
        GamePort = Convert.ToUInt16(config["game_port"]);

        Mode = config["mode"] switch
        {
            "dev" => Mode.Dev,
            "release" => Mode.Release,
            _ => Mode.Release
        };
    }
}

public enum Mode
{
    Dev,
    Release
}
