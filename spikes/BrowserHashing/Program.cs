using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: SupportedOSPlatform("browser")]

namespace Wireloom;

/// <summary>Runs a fixed IDL compilation through a JavaScript MD5 provider in a browser.</summary>
public static partial class BrowserSpike
{
    private const string SampleIdl = """
        @autoid(HASH)
        struct HashSample {
            long color;
            long shapesize;
            @hashid("é") long unicode;
        };
        """;

    /// <summary>Provides the WebAssembly executable entry point.</summary>
    public static void Main()
    {
    }

    /// <summary>Compiles the sample for both RTI profiles and returns its checks and generated documents.</summary>
    [JSExport]
    public static string Run()
    {
        var profiles = new List<ProfileReport>();
        foreach (var profile in new[] { IdlCompilerProfile.Rti731, IdlCompilerProfile.Rti770 })
        {
            var hashCalls = 0;
            var result = IdlCompiler.Compile(
                [new IdlInput("/spike/HashSample.idl", SampleIdl)],
                profile,
                bytes =>
                {
                    hashCalls++;
                    return ComputeMd5(bytes);
                });

            var plugin = result.Documents.SingleOrDefault(document => document.HintName.EndsWith("HashSamplePlugin.g.cs", StringComparison.Ordinal));
            var checks = new[]
            {
                CheckMember(plugin, "color", 262528368),
                CheckMember(plugin, "shapesize", 74944730),
                CheckMember(plugin, "unicode", 130932070)
            };

            profiles.Add(new ProfileReport(
                profile.ToString(),
                result.Success && hashCalls == 3 && checks.All(check => check.Matches),
                hashCalls,
                checks,
                result.Diagnostics.Select(diagnostic => $"{diagnostic.Id}: {diagnostic.Message}").ToArray(),
                result.Documents.Select(document => new DocumentReport(document.HintName, document.Source)).ToArray()));
        }

        var report = new SpikeReport(OperatingSystem.IsBrowser(), RuntimeInformation.FrameworkDescription, SampleIdl, profiles.ToArray());
        return JsonSerializer.Serialize(report, SpikeJsonContext.Default.SpikeReport);
    }

    [JSImport("computeMd5", "hash-provider")]
    private static partial byte[] ComputeMd5(byte[] input);

    private static HashCheck CheckMember(GeneratedIdlSource? plugin, string name, int expected)
    {
        var declaration = $"new StructMember(\"{name}\", dtf.GetPrimitiveType<int>(), id: {expected})";
        return new HashCheck(name, expected, plugin?.Source.Contains(declaration, StringComparison.Ordinal) == true);
    }
}

internal sealed record SpikeReport(bool IsBrowser, string Runtime, string Idl, ProfileReport[] Profiles);
internal sealed record ProfileReport(string Profile, bool Success, int HashCalls, HashCheck[] Checks, string[] Diagnostics, DocumentReport[] Documents);
internal sealed record HashCheck(string Member, int Expected, bool Matches);
internal sealed record DocumentReport(string Name, string Source);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SpikeReport))]
internal sealed partial class SpikeJsonContext : JsonSerializerContext;
