using Humanizer;
using System.Text.Json;

namespace Spire.Generator;

public class ProtocolGenerator(DirectoryInfo schemaDir, DirectoryInfo genDir)
{
    private const string TAB = "    ";
    
    public void Generate()
    {
        Console.WriteLine("Generating protocol...");
        Console.WriteLine($"Schema dir: {schemaDir.FullName}");
        Console.WriteLine($"Gen dir: {genDir.FullName}");
        
        Directory.CreateDirectory(genDir.FullName);
        
        GenerateCode();
    }

    private void GenerateCode()
    {
        var categoryFiles = Directory.GetFiles(schemaDir.FullName, "*.json");
        
        List<CategorySchema> categories = [];
        List<(string category, string protocolName, ushort number)> protocols = [];
        
        // Read all category files
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        foreach (var categoryFile in categoryFiles)
        {
            Console.WriteLine(categoryFile);
            
            var json = File.ReadAllText(categoryFile);
            var category = JsonSerializer.Deserialize<CategorySchema>(json, options)!;
            categories.Add(category);
        }
        categories.Sort((a, b) => a.Offset < b.Offset ? -1 : 1);
        
        // Build protocol list with numbers
        foreach (var category in categories)
        {
            var number = category.Offset;
            foreach (var protocol in category.Protocols)
            {
                protocols.Add((category.Category, protocol, number));
                number += 1;
            }
        }
        
        List<string> decodes = [];
        List<string> cases = [];
        
        // Generate code fragments
        foreach (var (category, protocol, number) in protocols)
        {
            var categoryName = category.Pascalize();
            var recordName = $"{protocol}Protocol";
            
            decodes.Add($"{number} => new {recordName}({categoryName}.{protocol}.Parser.ParseFrom(data)),");
            
            cases.Add($@"
public record {recordName}({categoryName}.{protocol} Value) : IProtocol
{{
    public ushort ProtocolId => {number};
    public int Size => Value.CalculateSize();
    
    public void Encode(Span<byte> buffer)
    {{
        Value.WriteTo(buffer);
    }}
}}");
        }

        var code = $@"// Generated file
using Google.Protobuf;

namespace Spire.Protocol;

public interface IProtocol
{{
    public ushort ProtocolId {{ get; }}
    public int Size {{ get; }}

    public void Encode(Span<byte> buffer);

    public static IProtocol Decode(ushort id, ReadOnlySpan<byte> data) {{
        return id switch {{
            {string.Join($"\n{TAB}{TAB}{TAB}", decodes)}
            _ => throw new ProtocolException($""Unknown protocol id: {{id}}"")
        }};
    }}
}}

public class ProtocolException(string message) : Exception(message);

{string.Join("\n", cases)}
";
        
        File.WriteAllText(Path.Combine(genDir.FullName, "Protocol.impl.cs"), code);
    }

    private class CategorySchema
    {
        public string Category { get; set; } = string.Empty;
        public ushort Offset { get; set; }
        public List<string> Protocols { get; set; } = [];
    }
}