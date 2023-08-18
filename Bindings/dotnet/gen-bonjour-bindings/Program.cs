using System.IO;
using System.Runtime.CompilerServices;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;

var outputDirectory = GetOutputDirectory();
ConsoleDriver.Run(new Bonjour(outputDirectory.FullName));
var symbols = new FileInfo(Path.Combine(outputDirectory.FullName, "BonjourSharp-symbols.cpp"));
var std = new FileInfo(Path.Combine(outputDirectory.FullName, "Std.cs"));
symbols.Delete();
std.Delete();

static DirectoryInfo GetOutputDirectory([CallerFilePath] string path = "") => new(Path.Combine(Path.GetDirectoryName(path)!, "..", "BonjourSharp"));

class Bonjour : ILibrary
{
    private readonly string _outputDirectory;

    public Bonjour(string outputDirectory) => _outputDirectory = outputDirectory;

    public void Setup(Driver driver)
    {
        var options = driver.Options;
        options.OutputDir = _outputDirectory;
        options.GeneratorKind = GeneratorKind.CSharp;
        options.UseSpan = true;
        options.GenerateDefaultValuesForArguments = true;
        var module = options.AddModule("BonjourSharp");
        module.IncludeDirs.Add(@"C:\Program Files\Bonjour SDK\Include");
        module.Headers.Add("dns_sd.h");
        module.LibraryDirs.Add(@"C:\Program Files\Bonjour SDK\Lib\x64");
        module.Libraries.Add("dnssd.lib");
    }

    public void SetupPasses(Driver driver)
    {
        driver.Context.TranslationUnitPasses.RenameDeclsUpperCase(RenameTargets.Any);
    }

    public void Preprocess(Driver driver, ASTContext ctx)
    {
        foreach (var unit in ctx.TranslationUnits)
        {
            foreach (var @class in unit.Classes)
            {
                @class.Ignore = @class.Name is not ("dns_sd" or "_DNSServiceRef_t" or "_DNSRecordRef_t");
            }

            foreach (var @enum in unit.Enums)
            {
                @enum.Ignore = @enum.Name != "";
            }
        }
    }

    public void Postprocess(Driver driver, ASTContext ctx)
    {
    }
}