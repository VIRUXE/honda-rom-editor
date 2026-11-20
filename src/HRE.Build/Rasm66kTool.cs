using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HRE.Build;

public enum DiagnosticSeverity { Warning, Error }

/// <summary>A message from the assembler, pointing at a source position.</summary>
public sealed record Diagnostic(string File, int Line, int Column, DiagnosticSeverity Severity, string Message) {
    public override string ToString() => $"{File}:{Line}:{Column}: {(Severity == DiagnosticSeverity.Error ? "error" : "warning")}: {Message}";
}

/// <summary>The outcome of running the assembler: whether it produced an image, and what it said.</summary>
public sealed record AssembleResult(bool Succeeded, IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>The assembler could not be started, or failed for a reason that is not a source error.</summary>
public sealed class BuildToolException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Runs the rasm66k assembler as an external process.</summary>
public sealed partial class Rasm66kTool(string executablePath) {
    public string ExecutablePath { get; } = executablePath;

    /// <summary>
    /// Finds the assembler: <c>HRE_RASM66K</c>, then next to the application, then the PATH.
    /// Null when it is nowhere to be found.
    /// </summary>
    public static Rasm66kTool? Locate() {
        var name = OperatingSystem.IsWindows() ? "rasm66k.exe" : "rasm66k";
        var candidates = new List<string?> { Environment.GetEnvironmentVariable("HRE_RASM66K"), Path.Combine(AppContext.BaseDirectory, name) };
        candidates.AddRange((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Select(dir => Path.Combine(dir, name)));
        return candidates.FirstOrDefault(File.Exists) is { } path ? new Rasm66kTool(path) : null;
    }

    /// <summary>
    /// Assembles <paramref name="entryPath"/> into <paramref name="outputPath"/> and writes the
    /// symbol map to <paramref name="mapPath"/>. A define overrides an EQU of the same name in the
    /// source, which is how feature flags are passed without rewriting any file.
    /// </summary>
    public async Task<AssembleResult> AssembleAsync(string entryPath, string outputPath, string mapPath, IReadOnlyDictionary<string, long> defines, int romSize, CancellationToken cancel = default) {
        var args = new List<string> { "asm", entryPath, outputPath, "--map", mapPath, "--rom-size", romSize.ToString(CultureInfo.InvariantCulture), "--json-diagnostics" };
        foreach (var (name, value) in defines.OrderBy(d => d.Key, StringComparer.Ordinal)) {
            args.Add("-D");
            args.Add($"{name}={value.ToString(CultureInfo.InvariantCulture)}");
        }
        var (exitCode, _, stderr) = await RunAsync(args, Path.GetDirectoryName(Path.GetFullPath(entryPath))!, cancel);
        return exitCode switch {
            0 => new AssembleResult(true, ParseDiagnostics(stderr)),
            1 => new AssembleResult(false, ParseDiagnostics(stderr)),
            _ => throw new BuildToolException($"the assembler failed (exit code {exitCode}): {stderr.Trim()}"),
        };
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(IEnumerable<string> args, string workingDirectory, CancellationToken cancel) {
        var start = new ProcessStartInfo(ExecutablePath) {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        try {
            process.Start();
        } catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) {
            throw new BuildToolException($"could not start the assembler at {ExecutablePath}: {e.Message}", e);
        }
        var stdout = process.StandardOutput.ReadToEndAsync(cancel);
        var stderr = process.StandardError.ReadToEndAsync(cancel);
        try {
            await process.WaitForExitAsync(cancel);
        } catch (OperationCanceledException) {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            throw;
        }
        return (process.ExitCode, await stdout, await stderr);
    }

    /// <summary>
    /// Reads the assembler's messages: a JSON array when it reports errors, or
    /// <c>file:line:col: severity: message</c> lines when it only has warnings.
    /// </summary>
    internal static IReadOnlyList<Diagnostic> ParseDiagnostics(string text) {
        var trimmed = text.Trim();
        if (trimmed.Length == 0) return [];
        if (trimmed.StartsWith('[')) {
            try {
                using var doc = JsonDocument.Parse(trimmed);
                return [.. doc.RootElement.EnumerateArray().Select(d => new Diagnostic(
                    d.GetProperty("file").GetString() ?? "",
                    d.GetProperty("line").GetInt32(),
                    d.GetProperty("col").GetInt32(),
                    ParseSeverity(d.GetProperty("severity").GetString()),
                    d.GetProperty("message").GetString() ?? ""))];
            } catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException) {
                // Not the JSON form after all: fall through and read it as text.
            }
        }
        var diagnostics = new List<Diagnostic>();
        foreach (var line in trimmed.Split('\n')) {
            var m = TextDiagnostic().Match(line.TrimEnd('\r'));
            if (m.Success) {
                diagnostics.Add(new Diagnostic(m.Groups[1].Value, int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), ParseSeverity(m.Groups[4].Value), m.Groups[5].Value));
            } else if (line.Trim().Length > 0) {
                diagnostics.Add(new Diagnostic("", 0, 0, DiagnosticSeverity.Error, line.Trim()));
            }
        }
        return diagnostics;
    }

    private static DiagnosticSeverity ParseSeverity(string? severity) =>
        severity == "warning" ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error;

    [GeneratedRegex(@"^(.*?):(\d+):(\d+): (warning|error): (.*)$")]
    private static partial Regex TextDiagnostic();
}
