using System.CommandLine;
using System.CommandLine.Parsing;
using Spire.Generator;

Option<DirectoryInfo> schemaDirOption = new("--schema_dir")
{
    Description = "The directory to read schema files"
};
Option<DirectoryInfo> genDirOption = new("--gen_dir")
{
    Description = "The directory to write generated files"
};

Command protocolCommand = new("protocol", "Generate protocol files")
{
    schemaDirOption,
    genDirOption,
};
protocolCommand.SetAction(parsed => GenerateProtocol(
    parsed.GetRequiredValue(schemaDirOption),
    parsed.GetRequiredValue(genDirOption)
));

Command dataCommand = new("data", "Generate data files")
{
    schemaDirOption,
    genDirOption,
};

RootCommand rootCommand = new("Spire client code generator")
{
    protocolCommand,
    dataCommand,
};

return rootCommand.Parse(args).Invoke();

void GenerateProtocol(DirectoryInfo schemaDir, DirectoryInfo genDir)
{
    ProtocolGenerator generator = new(schemaDir, genDir);
    generator.Generate();
}

void GenerateData()
{
    
}
