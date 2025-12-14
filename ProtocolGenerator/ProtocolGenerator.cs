using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Spire.ProtocolGenerator;

public class CategorySchema
{
    public string Category { get; set; }
    public ushort Offset { get; set; }
    public List<ProtocolSchema> Protocols { get; set; } = [];
}

public class ProtocolSchema
{
    public string Protocol { get; set; }
    public ProtocolTarget Target { get; set; }
}

public enum ProtocolTarget
{
    Client,
    Server,
    All
}

public class CategoryParseResult
{
    public CategorySchema Schema { get; set; }
    public string FileName { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
}

[Generator]
public class ProtocolGenerator : IIncrementalGenerator
{
    private const string Tab = "    ";

    private static readonly DiagnosticDescriptor FileError = new DiagnosticDescriptor(
        "FILE001", "File error", "Invalid file: '{0}': {1}", "Gen", DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        
        var pipeline = context.AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".json"))
            .Select((text, cancellationToken) =>
            {
                var content = text.GetText(cancellationToken)?.ToString();
                var fileName = Path.GetFileName(text.Path);
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new CategoryParseResult { IsSuccess = false, FileName = fileName };
                }

                try
                {
                    var schema = JsonSerializer.Deserialize<CategorySchema>(content!, options);
                    return schema == null 
                        ? new CategoryParseResult { IsSuccess = false, FileName = fileName }
                        : new CategoryParseResult { IsSuccess = true, FileName = fileName, Schema = schema };
                }
                catch (Exception ex)
                {
                    return new CategoryParseResult { IsSuccess = false, FileName = fileName, ErrorMessage = ex.Message };
                }
            })
            .Collect();
        
        context.RegisterSourceOutput(pipeline, GenerateProtocolCode);
    }

    private static void GenerateProtocolCode(
        SourceProductionContext context,
        ImmutableArray<CategoryParseResult> results)
    {
        List<CategorySchema> categorySchemas = [];
        foreach (var result in results)
        {
            if (!result.IsSuccess)
            {
                context.ReportDiagnostic(Diagnostic.Create(FileError, Location.None, result.FileName, result.ErrorMessage));
                return;
            }
            
            categorySchemas.Add(result.Schema);
        }
        categorySchemas.Sort((x, y) => x.Offset.CompareTo(y.Offset));
        
        List<(string category, string protocol, ushort number)> protocols = [];
        foreach (var categorySchema in categorySchemas)
        {
            var number = categorySchema.Offset;
            foreach (var protocolSchema in categorySchema.Protocols)
            {
                protocols.Add((categorySchema.Category, protocolSchema.Protocol, number));
                number += 1;
            }
        }
        
        var code = GenerateProtocolCodeInternal(protocols);
        context.AddSource("Protocol.impl.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static string GenerateProtocolCodeInternal(List<(string category, string protocol, ushort number)> protocols)
    {
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

        return $@"// Generated file
using Google.Protobuf;

namespace Spire.Protocol.Game;

public interface IProtocol
{{
    public ushort ProtocolId {{ get; }}
    public int Size {{ get; }}

    public void Encode(Span<byte> buffer);

    public static IProtocol Decode(ushort id, ReadOnlySpan<byte> data) {{
        return id switch {{
            {string.Join($"\n{Tab}{Tab}{Tab}", decodes)}
            _ => throw new ProtocolException($""Unknown protocol id: {{id}}"")
        }};
    }}
}}

public class ProtocolException(string message) : Exception(message);

{string.Join("\n", cases)}
";
    }
}
