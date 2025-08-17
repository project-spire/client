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

        List<string> imports = [];
        
        // Build protocol list with numbers
        foreach (var category in categories)
        {
            imports.Add($"using Spire.Protocol.{category.Category.Pascalize()};");
            
            var number = category.Offset;
            foreach (var protocol in category.Protocols)
            {
                protocols.Add((category.Category, protocol, number));
                number += 1;
            }
        }
        
        List<string> decodes = [];
        List<string> cases = [];
        List<string> extensions = [];
        
        // Generate code fragments
        foreach (var (category, protocol, number) in protocols)
        {
            decodes.Add($"{number} => {protocol}.Parser.ParseFrom(data),");
            
            cases.Add($"public record {protocol}({protocol} Value) : Protocol;");
        
            extensions.Add($"public static ushort ProtocolId(this {protocol} _) => {number};");
        }

        var code = $@"// Generated file
{string.Join("\n", imports)}

namespace Spire.Protocol;

public abstract record Protocol
{{
    public static Protocol Decode(ushort id, ReadOnlySpan<byte> data) {{
        return id switch {{
            {string.Join($"\n{TAB}{TAB}{TAB}", decodes)}
            _ => throw new ProtocolException($""Unknown protocol id: {{id}}"")
        }};
    }}

    {string.Join($"\n{TAB}", cases)}
}}

public static class ProtocolExtensions
{{
    {string.Join($"\n{TAB}", extensions)}
}}
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