#!/usr/bin/env dotnet
#:property TargetFramework=net10.0
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false

using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CodexToolkit;

public static class AgentTool
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    public const string Help = """
        codex-agent-tool — deterministic → JEV → Codex
        dotnet tools/AgentTool.cs -- <command> [options]

        install | update | uninstall [--home DIR] [--codex-home DIR] [--dry-run] [--bin]
        doctor
        repo changed-files [--base REF] | locate --query TEXT | health | affected-projects [--base REF]
        git state | prepare-commit | issue-start --issue NUMBER --branch NAME
        github pr-status | review-comments --pr NUMBER | prepare-pr
        dotnet verify [--base REF] [--project PATH] | format --project PATH [--apply]
        dotnet package-audit --project PATH | api-check --project PATH | release-verify --project PATH
        logs summarize --file PATH | sarif summarize --file PATH
        jev noul|choice|score --input PATH [--dry-run] [--safe-input]
        jev screen --input PATH [--dry-run] [--safe-input] | cache-clear
        upstream status | update [--dry-run]
        validate | eval [--skill NAME] [--results PATH] | release --output ZIP
        results init | new <audit|handoff|review|report> <name>
        results list [audit|handoff|review|report] [--json] | latest <type> [--json]
        results context <type> [--json] | clean [--dry-run]

        Common: --root DIR (target repository), --toolkit DIR, --json, --help
        JEV input: {"state":"sanitized excerpt","instructions":"bounded question","criteria":...}
        Screen input: {"query":"question","candidates":[{"id":"path","text":"safe excerpt"}]}
        JEV defaults to auto; missing/invalid/uncertain answers return REVIEW for Codex.
        No command merges PRs, commits, pushes, installs external tools, or pulls Git updates.
        """;

    public static async Task<int> Main(string[] args)
    {
        try
        {
            var c = Cli.Parse(args);
            if (c.Flag("help") || c.Words.Count == 0 || c.Words[0] == "help") { Console.WriteLine(Help); return 0; }
            var toolkit = FindToolkit(c.Get("toolkit"));
            var root = Path.GetFullPath(c.Get("root") ?? Environment.CurrentDirectory);
            var settings = Settings.Load(toolkit);
            var result = await Execute(c, toolkit, root, settings);
            var rendered = JsonSerializer.Serialize(result, Json);
            if (rendered.Length > settings.Output.MaxOutputChars)
            {
                var report = Path.Combine(root, ".agent-tool", $"result-{Guid.NewGuid():N}.json");
                SafeFiles.Atomic(report, rendered);
                rendered = JsonSerializer.Serialize(new { result.Status, result.ExitCode, truncated = true, characters = rendered.Length, artifact = report }, Json);
            }
            Console.WriteLine(rendered);
            return result.ExitCode;
        }
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException or JsonException or FormatException or System.Xml.XmlException or System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { status = "error", message = Secrets.Redact(e.Message) }, Json));
            return 2;
        }
    }

    public static string FindToolkit(string? explicitRoot = null, [System.Runtime.CompilerServices.CallerFilePath] string source = "")
    {
        var path = explicitRoot ?? Environment.GetEnvironmentVariable("CODEX_TOOLKIT_ROOT");
        if (path is null)
        {
            source = File.ResolveLinkTarget(source, true)?.FullName ?? source;
            for (var parent = Path.GetDirectoryName(source); parent is not null; parent = Path.GetDirectoryName(parent))
                if (File.Exists(Path.Combine(parent, "config", "toolkit.json"))) { path = parent; break; }
        }
        path ??= Environment.CurrentDirectory;
        path = Path.GetFullPath(path);
        if (!File.Exists(Path.Combine(path, "config", "toolkit.json"))) throw new ArgumentException("Toolkit root not found; pass --toolkit DIR or CODEX_TOOLKIT_ROOT.");
        return path;
    }

    public static async Task<Result> Execute(Cli c, string toolkit, string root, Settings settings)
    {
        var command = c.Words.FirstOrDefault() == "results" ? string.Join(' ', c.Words.Take(2)) : string.Join(' ', c.Words);
        c.ValidateCommand(command);
        var artifacts = Path.Combine(root, ".agent-tool");
        switch (command)
        {
            case "install":
            case "update":
            case "uninstall":
                return Installer.Run(toolkit, c.Get("home") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), c.Get("codex-home") ?? (c.Get("home") is null ? Environment.GetEnvironmentVariable("CODEX_HOME") : null), command, c.Flag("dry-run"), c.Flag("bin"));
            case "doctor": return await Doctor(toolkit, settings, c);
            case "git state": return Result.Ok(await Git.State(root));
            case "git prepare-commit":
            case "github prepare-pr":
                await Git.EnsureSafe(root, false);
                var diff = await Processes.Run("git", ["diff", "--check"], root);
                var staged = await Processes.Run("git", ["diff", "--cached", "--check"], root);
                return new(diff.ExitCode != 0 || staged.ExitCode != 0 ? "failed" : "ok", new { state = await Git.State(root), whitespace = Output.Compact(diff.Output + staged.Output, settings.Output), next = "Review explicit file scope before staging. Commit/push/PR creation remains caller-controlled; never merge without approval." }, diff.ExitCode != 0 || staged.ExitCode != 0 ? 1 : 0);
            case "git issue-start":
                await Git.EnsureSafe(root, true);
                var issue = c.PositiveInt("issue"); var branch = c.Require("branch");
                await Git.Require(root, "check-ref-format", "--branch", branch);
                var info = await Processes.Run("gh", ["issue", "view", issue.ToString(CultureInfo.InvariantCulture), "--json", "state"], root);
                if (info.ExitCode != 0 || JsonNode.Parse(info.Output)?["state"]?.GetValue<string>() != "OPEN") throw new InvalidOperationException("Issue is unavailable or not open; no branch created.");
                await Git.Require(root, "switch", "-c", branch);
                return Result.Ok(new { branch, issue });
            case "repo changed-files": return Result.Ok(await Git.Changed(root, c.Get("base")));
            case "repo locate":
                var files = await Git.Files(root);
                return Result.Ok(new { matches = files.Where(x => x.Contains(c.Require("query"), StringComparison.OrdinalIgnoreCase)).Take(settings.Output.MaxItems), total = files.Count(x => x.Contains(c.Require("query"), StringComparison.OrdinalIgnoreCase)), scope = "Git tracked + untracked, nonignored path names; use rg for symbols." });
            case "repo affected-projects": return Result.Ok(await Projects.Affected(root, await Git.Changed(root, c.Get("base"))));
            case "repo health":
                var health = await Projects.Health(root, settings.Health);
                return new(health.Count == 0 ? "ok" : "findings", health, health.Count == 0 ? 0 : 1);
            case "github pr-status": return await RunArtifact("gh", ["pr", "status", "--json", "currentBranch,createdBy,needsReview"], root, artifacts, settings.Output);
            case "github review-comments":
                var pr = c.PositiveInt("pr").ToString(CultureInfo.InvariantCulture);
                return await RunArtifact("gh", ["api", $"repos/{{owner}}/{{repo}}/pulls/{pr}/comments", "--paginate"], root, artifacts, settings.Output);
            case "dotnet verify":
            case "dotnet format":
            case "dotnet package-audit":
            case "dotnet api-check":
            case "dotnet release-verify":
                return await Dotnet(command, c, root, artifacts, settings);
            case "logs summarize":
                return Result.Ok(Output.SummarizeFile(c.Require("file"), settings.Output));
            case "sarif summarize": return Result.Ok(Output.Sarif(c.Require("file"), settings.Output));
            case "jev noul":
            case "jev choice":
            case "jev score":
            case "jev screen":
                return await JevCommand(c, command[4..], root, settings.Jev);
            case "jev cache-clear":
                var cachePath = Path.Combine(artifacts, "jev-cache");
                SafeFiles.NoLinks(cachePath);
                if (Directory.Exists(cachePath)) Directory.Delete(cachePath, true);
                return Result.Ok(new { cleared = cachePath });
            case "upstream status": return Result.Ok(new { plugins = JsonNode.Parse(File.ReadAllText(Path.Combine(toolkit, "upstream/dotnet-skills.json"))), tools = JsonNode.Parse(File.ReadAllText(Path.Combine(toolkit, "upstream/tools.json"))), versions = JsonNode.Parse(File.ReadAllText(Path.Combine(toolkit, "upstream/versions.json"))) });
            case "upstream update": return await Upstream(toolkit, artifacts, c.Flag("dry-run"));
            case "validate": return Validation.Run(toolkit);
            case "eval": return Evaluation.Run(toolkit, c.Get("skill"), c.Get("results"));
            case "release": return Release(toolkit, c.Require("output"));
            case "results init": Results.RequireWords(c.Words.Skip(2).ToArray(), 0, "Usage: results init."); return Results.Init(root);
            case "results new": return await Results.New(root, c.Words.Skip(2).ToArray());
            case "results list": return Results.List(root, c.Words.Skip(2).ToArray());
            case "results latest": return Results.Latest(root, c.Words.Skip(2).ToArray());
            case "results context": return Results.Context(root, c.Words.Skip(2).ToArray());
            case "results clean": Results.RequireWords(c.Words.Skip(2).ToArray(), 0, "Usage: results clean [--dry-run]."); return Results.Clean(root, c.Flag("dry-run"));
            default: throw new ArgumentException("Unknown command. Use --help.");
        }
    }

    static async Task<Result> Doctor(string toolkit, Settings settings, Cli c)
    {
        var checks = new List<object>(); bool requiredOk = true;
        foreach (var (tool, args, required) in new[] { ("dotnet", new[] { "--version" }, true), ("git", new[] { "--version" }, true), ("gh", new[] { "--version" }, false), ("codex", new[] { "--version" }, false) })
        {
            try
            {
                var r = await Processes.Run(tool, args, toolkit);
                var ok = r.ExitCode == 0 && (tool != "dotnet" || Version.TryParse(r.Output.Trim().Split('-')[0], out var v) && v.Major >= 10);
                checks.Add(new { tool, required, available = ok, summary = Output.Compact(r.Output, settings.Output) });
                if (required && !ok) requiredOk = false;
            }
            catch (System.ComponentModel.Win32Exception) { checks.Add(new { tool, required, available = false }); if (required) requiredOk = false; }
        }
        var home = c.Get("home") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var codex = c.Get("codex-home") ?? (c.Get("home") is null ? Environment.GetEnvironmentVariable("CODEX_HOME") : null) ?? Path.Combine(home, ".codex");
        return new(requiredOk ? "ok" : "failed", new { checks, toolkit, codex, skills = Path.Combine(home, ".agents/skills"), installation = Installer.Inspect(codex), jev = new { settings.Jev.Mode, credentials = JevCredentials.Status(), settings.Jev.Model }, upstream = "Run upstream status for integration policy; listed integrations are not automatically installed.", optionalTools = settings.Toolkit.OptionalTools.Select(t => new { name = t, available = Processes.OnPath(t) }) }, requiredOk ? 0 : 1);
    }

    static async Task<Result> Dotnet(string command, Cli c, string root, string artifacts, Settings settings)
    {
        var explicitProject = c.Get("project");
        if (command != "dotnet verify" && explicitProject is null) throw new ArgumentException("This command requires --project PATH (project or solution).");
        var targets = explicitProject is not null ? new[] { Path.GetFullPath(explicitProject, root) } : (await Projects.Affected(root, await Git.Changed(root, c.Get("base")))).Projects.ToArray();
        if (targets.Any(x => !File.Exists(x))) throw new ArgumentException("Project or solution does not exist.");
        var results = new List<Result>();
        foreach (var project in targets)
        {
            if (command == "dotnet format")
            {
                var changed = await Git.Changed(root, c.Get("base"));
                var code = changed.Where(x => x.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(root, x))).Select(x => Path.Combine(root, x)).ToArray();
                if (code.Length == 0) continue;
                var args = new List<string> { "format", project, "--include" }; args.AddRange(code);
                if (!c.Flag("apply")) args.Add("--verify-no-changes");
                results.Add(await RunArtifact("dotnet", args, root, artifacts, settings.Output));
            }
            else if (command == "dotnet package-audit")
            {
                var r = await RunArtifact("dotnet", ["package", "list", "--project", project, "--vulnerable", "--include-transitive", "--format", "json"], root, artifacts, settings.Output);
                if (r.ExitCode == 0 && r.Data is ProcessReport p)
                {
                    var report = JsonNode.Parse(File.ReadAllText(p.Artifact));
                    var vulnerabilities = Audit.Count(report);
                    r = new(vulnerabilities > 0 ? "vulnerable" : "ok", new { vulnerabilities, p.Artifact }, vulnerabilities > 0 ? 1 : 0);
                }
                results.Add(r);
            }
            else if (command == "dotnet api-check")
            {
                if (!Projects.HasApiChecks(project)) throw new InvalidOperationException("Configure PublicApiAnalyzers or EnablePackageValidation with a baseline first; api-check cannot certify an unconfigured project.");
                results.Add(await RunArtifact("dotnet", ["pack", project, "-p:EnablePackageValidation=true", "-p:TreatWarningsAsErrors=true"], root, artifacts, settings.Output));
            }
            else
            {
                var steps = new List<string[]> { new[] { "build", project, "--nologo" } };
                if (command == "dotnet release-verify") steps.Insert(0, ["restore", project, "-p:NuGetAudit=true", "-p:NuGetAuditMode=all"]);
                if (command == "dotnet release-verify") steps.Add(["format", project, "--verify-no-changes"]);
                var isSolution = project.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || project.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
                if (isSolution || await Projects.IsTest(root, project)) steps.Add(["test", project, "--no-build", "--nologo"]);
                foreach (var step in steps)
                {
                    var r = await RunArtifact("dotnet", step, root, artifacts, settings.Output); results.Add(r);
                    if (r.ExitCode != 0) break;
                }
                if (command == "dotnet release-verify" && results.All(r => r.ExitCode == 0))
                    results.Add(await Dotnet("dotnet package-audit", c, root, artifacts, settings));
            }
        }
        return new(results.Any(x => x.ExitCode != 0) ? "failed" : "ok", new { targets, results, scope = command == "dotnet release-verify" ? "restore, build, format, tests where detected, vulnerability audit; API/SBOM/reproducibility gates are separate on-demand skills" : "targeted" }, results.Any(x => x.ExitCode != 0) ? 1 : 0);
    }

    public static async Task<Result> RunArtifact(string executable, IEnumerable<string> args, string root, string artifacts, OutputSettings limits)
    {
        SafeFiles.NoLinks(artifacts); Directory.CreateDirectory(artifacts);
        var path = Path.Combine(artifacts, $"{executable}-{DateTime.UtcNow:yyyyMMddTHHmmss}-{Guid.NewGuid():N}.log");
        var r = await Processes.Run(executable, args, root, path, TimeSpan.FromMinutes(20));
        return new(r.ExitCode == 0 ? "ok" : "failed", new ProcessReport(r.ExitCode, Output.SummarizeFile(path, limits), path), r.ExitCode == 0 ? 0 : 1);
    }

    static async Task<Result> JevCommand(Cli c, string kind, string root, JevSettings settings)
    {
        var inputPath = c.Require("input");
        if (Path.GetFileName(inputPath).StartsWith(".env", StringComparison.OrdinalIgnoreCase)) return Result.Review("Sensitive input path refused.");
        if (new FileInfo(inputPath).Length > settings.MaxInputBytes) return Result.Review("Input exceeds configured limit.");
        var input = JsonNode.Parse(File.ReadAllText(inputPath)) ?? throw new ArgumentException("Empty input.");
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds) };
        var client = new JevClient(http, settings, Path.Combine(root, ".agent-tool/jev-cache"));
        if (kind == "screen")
        {
            var candidates = input["candidates"]?.AsArray() ?? throw new ArgumentException("candidates array required.");
            if (candidates.Count > settings.MaxCandidates) return Result.Review("Too many candidates; narrow deterministic search first.");
            var answers = new List<object>(); int exitCode = 0;
            foreach (var candidate in candidates)
            {
                var request = JevClient.Request("noul", candidate?["text"]?.GetValue<string>() ?? "", $"Is this candidate relevant to: {input["query"]?.GetValue<string>()}", null, settings.Model);
                var judgment = Secrets.LooksSensitive(request.ToJsonString()) ? Result.Review("Potential secret detected; request refused.") : c.Flag("dry-run") ? Result.Ok(request) : !c.Flag("safe-input") ? Result.Review("Use --safe-input only after minimizing and reviewing supplied text for external transmission.") : await client.Judge(request);
                exitCode = Math.Max(exitCode, judgment.ExitCode);
                answers.Add(new { id = candidate?["id"]?.GetValue<string>(), judgment });
            }
            return new(exitCode == 0 ? "ok" : "REVIEW", answers, exitCode);
        }
        var payload = JevClient.Request(kind, input["state"]?.GetValue<string>() ?? "", input["instructions"]?.GetValue<string>() ?? "", input["criteria"], settings.Model);
        if (c.Flag("dry-run")) return Secrets.LooksSensitive(payload.ToJsonString()) ? Result.Review("Potential secret detected; request refused.") : Result.Ok(payload);
        if (!c.Flag("safe-input")) return Result.Review("Use --safe-input only after reviewing and minimizing supplied text for external transmission.");
        return await client.Judge(payload);
    }

    static async Task<Result> Upstream(string toolkit, string artifacts, bool dryRun)
    {
        var versions = JsonNode.Parse(File.ReadAllText(Path.Combine(toolkit, "upstream/versions.json")))!["repositories"]!.AsArray();
        var rows = new List<object>();
        foreach (var entry in versions)
        {
            var repo = entry!["repository"]!.GetValue<string>(); var pinned = entry["revision"]?.GetValue<string>();
            if (dryRun) { rows.Add(new { repo, pinned, query = $"gh api repos/{repo}/commits/HEAD --jq .sha" }); continue; }
            var r = await Processes.Run("gh", ["api", $"repos/{repo}/commits/HEAD", "--jq", ".sha"], toolkit);
            rows.Add(new { repo, pinned, latest = r.ExitCode == 0 ? r.Output.Trim() : null, status = r.ExitCode != 0 ? "unavailable" : r.Output.Trim() == pinned ? "current" : "review-update" });
        }
        if (dryRun) return Result.Ok(rows);
        SafeFiles.NoLinks(artifacts); Directory.CreateDirectory(artifacts);
        var report = Path.Combine(artifacts, "upstream-drift.json"); File.WriteAllText(report, JsonSerializer.Serialize(rows, Json));
        return Result.Ok(new { rows, report, policy = "Report only; no downloads, manifest edits or merges." });
    }

    static Result Release(string toolkit, string output)
    {
        var valid = Validation.Run(toolkit); if (valid.ExitCode != 0) return valid;
        output = Path.GetFullPath(output); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        using var zip = System.IO.Compression.ZipFile.Open(output, System.IO.Compression.ZipArchiveMode.Create);
        var roots = new[] { "agents", "config", "docs", "evals", "global", "plugins", "schemas", "templates", "tools", "upstream" };
        foreach (var file in roots.SelectMany(x => SafeFiles.Enumerate(Path.Combine(toolkit, x))).Concat(new[] { "README.md", "LICENSE", "NOTICE.md", "THIRD-PARTY-NOTICES.md", "global.json", ".agents/plugins/marketplace.json" }.Select(x => Path.Combine(toolkit, x))))
            System.IO.Compression.ZipFileExtensions.CreateEntryFromFile(zip, file, Path.GetRelativePath(toolkit, file).Replace('\\', '/'));
        return Result.Ok(new { archive = output });
    }
}

public record Result(string Status, object? Data, int ExitCode = 0)
{
    public static Result Ok(object? data) => new("ok", data);
    public static Result Review(string reason) => new("REVIEW", new { reason, fallback = "Codex" });
}
public record ProcessReport(int ProcessExitCode, object Summary, string Artifact);
public record ProcessResult(int ExitCode, string Output);

public sealed class Cli
{
    public void ValidateCommand(string command)
    {
        var allowed = new HashSet<string>(new[] { "root", "toolkit", "json", "help" });
        string[] specific = command switch
        {
            "install" or "update" => ["home", "codex-home", "dry-run", "bin"],
            "uninstall" => ["home", "codex-home", "dry-run"],
            "doctor" => ["home", "codex-home"],
            "repo changed-files" or "repo affected-projects" => ["base"],
            "repo locate" => ["query"],
            "git issue-start" => ["issue", "branch"],
            "github review-comments" => ["pr"],
            "dotnet verify" => ["base", "project"],
            "dotnet format" => ["base", "project", "apply"],
            "dotnet package-audit" or "dotnet api-check" or "dotnet release-verify" => ["project"],
            "logs summarize" or "sarif summarize" => ["file"],
            "jev noul" or "jev choice" or "jev score" or "jev screen" => ["input", "dry-run", "safe-input"],
            "upstream update" => ["dry-run"],
            "eval" => ["skill", "results"],
            "release" => ["output"],
            "results clean" => ["dry-run"],
            _ => []
        };
        allowed.UnionWith(specific);
        foreach (var option in Options.Keys)
            if (!allowed.Contains(option)) throw new ArgumentException($"--{option} is not supported by this command.");
    }
    public List<string> Words { get; } = [];
    public Dictionary<string, string?> Options { get; } = new(StringComparer.Ordinal);
    static readonly HashSet<string> Flags = ["json", "help", "dry-run", "bin", "apply", "safe-input"];
    static readonly HashSet<string> Values = ["root", "toolkit", "home", "codex-home", "base", "query", "issue", "branch", "pr", "project", "file", "input", "output", "skill", "results"];
    public string? Get(string name) => Options.GetValueOrDefault(name);
    public bool Flag(string name) => Options.ContainsKey(name);
    public string Require(string name) => Get(name) is { Length: > 0 } v ? v : throw new ArgumentException($"--{name} is required.");
    public int PositiveInt(string name) => int.TryParse(Require(name), out var i) && i > 0 ? i : throw new ArgumentException($"--{name} must be a positive integer.");
    public static Cli Parse(string[] args)
    {
        var result = new Cli();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "-h") arg = "--help";
            if (!arg.StartsWith('-')) { result.Words.Add(arg); continue; }
            if (!arg.StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException("Only long options are supported.");
            var parts = arg[2..].Split('=', 2); var name = parts[0];
            if (!Flags.Contains(name) && !Values.Contains(name)) throw new ArgumentException($"Unknown option --{name}.");
            if (result.Options.ContainsKey(name)) throw new ArgumentException($"Duplicate --{name}.");
            if (Flags.Contains(name)) { if (parts.Length != 1) throw new ArgumentException($"--{name} takes no value."); result.Options[name] = null; }
            else
            {
                var value = parts.Length == 2 ? parts[1] : ++i < args.Length && !args[i].StartsWith("--", StringComparison.Ordinal) ? args[i] : throw new ArgumentException($"Missing value for --{name}.");
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"Empty --{name}.");
                result.Options[name] = value;
            }
        }
        return result;
    }
}

public static class Processes
{
    public static bool OnPath(string name) => (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).Any(p => File.Exists(Path.Combine(p, name)) || OperatingSystem.IsWindows() && File.Exists(Path.Combine(p, name + ".exe")));
    internal static ProcessStartInfo StartInfo(string exe, IEnumerable<string> args, string cwd)
    {
        var info = new ProcessStartInfo(exe) { WorkingDirectory = cwd, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var arg in args)
        {
            if (JevCredentials.Contains(arg)) throw new InvalidOperationException("Refusing to place JEV credentials in child-process arguments.");
            info.ArgumentList.Add(arg);
        }
        info.Environment.Remove(JevCredentials.EnvironmentVariable);
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        return info;
    }
    public static async Task<ProcessResult> Run(string exe, IEnumerable<string> args, string cwd, string? artifact = null, TimeSpan? timeout = null)
    {
        var info = StartInfo(exe, args, cwd);
        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {exe}.");
        using var timer = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(60));
        using var writer = artifact is null ? null : new StreamWriter(artifact, false, new UTF8Encoding(false));
        var gate = new SemaphoreSlim(1); var buffer = new StringBuilder(); bool overflow = false;
        async Task Drain(StreamReader reader)
        {
            var chars = new char[4096]; int count;
            while ((count = await reader.ReadAsync(chars, timer.Token)) > 0)
            {
                await gate.WaitAsync(timer.Token);
                try
                {
                    if (writer is not null) await writer.WriteAsync(chars.AsMemory(0, count), timer.Token);
                    else if (buffer.Length + count <= 16 * 1024 * 1024) buffer.Append(chars, 0, count);
                    else overflow = true;
                }
                finally { gate.Release(); }
            }
        }
        var reads = Task.WhenAll(Drain(process.StandardOutput), Drain(process.StandardError));
        try { await Task.WhenAll(reads, process.WaitForExitAsync(timer.Token)); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync();
            return new(124, "Operation timed out; process tree terminated.");
        }
        if (overflow) throw new InvalidOperationException("Structured command output exceeded 16 MiB; narrow scope.");
        return new(process.ExitCode, buffer.ToString());
    }
}

public static class Git
{
    public static async Task<string> Require(string root, params string[] args)
    {
        var r = await Processes.Run("git", args, root);
        if (r.ExitCode != 0) throw new InvalidOperationException($"Git {args[0]} failed: {Secrets.Redact(r.Output.Trim())}");
        return r.Output;
    }
    public static async Task<GitState> State(string root)
    {
        var actual = (await Require(root, "rev-parse", "--show-toplevel")).Trim();
        var operations = new List<string>();
        foreach (var op in new[] { "MERGE_HEAD", "CHERRY_PICK_HEAD", "REVERT_HEAD", "rebase-merge", "rebase-apply", "BISECT_LOG", "sequencer", "index.lock" })
        {
            var path = (await Require(root, "rev-parse", "--git-path", op)).Trim();
            path = Path.GetFullPath(path, root);
            if (File.Exists(path) || Directory.Exists(path)) operations.Add(op);
        }
        var status = await Require(actual, "status", "--porcelain=v1", "-z", "--untracked-files=all");
        var branch = await Processes.Run("git", ["symbolic-ref", "--quiet", "--short", "HEAD"], root);
        return new(actual, branch.ExitCode == 0 ? branch.Output.Trim() : null, status.Length == 0, operations, status.Split('\0', StringSplitOptions.RemoveEmptyEntries));
    }
    public static async Task EnsureSafe(string root, bool requireClean)
    {
        var state = await State(root);
        if (state.Operations.Count != 0) throw new InvalidOperationException("Unfinished Git operation detected; no mutation performed.");
        if (state.Branch is null) throw new InvalidOperationException("Detached HEAD; no mutation performed.");
        if (requireClean && !state.Clean) throw new InvalidOperationException("Working tree has changes; preserve them before starting an issue.");
    }
    public static async Task<string[]> Files(string root) => (await Require(root, "ls-files", "--cached", "--others", "--exclude-standard", "-z")).Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    public static async Task<string[]> Changed(string root, string? baseRef = null)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        async Task Add(params string[] args) { foreach (var p in (await Require(root, args)).Split('\0', StringSplitOptions.RemoveEmptyEntries)) names.Add(p); }
        if (baseRef is not null)
        {
            var sha = (await Require(root, "rev-parse", "--verify", "--end-of-options", baseRef + "^{commit}")).Trim();
            var ancestor = (await Require(root, "merge-base", sha, "HEAD")).Trim();
            await Add("diff", "--name-only", "--no-renames", "-z", ancestor, "--");
        }
        else
        {
            await Add("diff", "--name-only", "--no-renames", "-z", "--");
            await Add("diff", "--cached", "--name-only", "--no-renames", "-z", "--");
        }
        await Add("ls-files", "--others", "--exclude-standard", "-z");
        return names.Order(StringComparer.Ordinal).ToArray();
    }
}
public record GitState(string Root, string? Branch, bool Clean, List<string> Operations, string[] Entries);

public static class Results
{
    static readonly Dictionary<string, string> Persistent = new(StringComparer.Ordinal) { ["audit"] = "audits", ["handoff"] = "handoffs", ["review"] = "reviews", ["report"] = "reports" };
    static readonly string[] Directories = ["audits", "handoffs", "reviews", "reports", "evals", "logs", "traces", "sarif", "binlogs", "test-results", "tmp"];
    static readonly string[] Transient = ["evals/generated", "logs", "traces", "sarif", "binlogs", "test-results", "tmp"];
    static readonly Regex Name = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    static string Root(string root) => Path.Combine(Path.GetFullPath(root), ".agent-results");
    static string Type(string value) => Persistent.TryGetValue(value, out var directory) ? directory : throw new ArgumentException("Result type must be audit, handoff, review, or report.");
    public static void RequireWords(string[] words, int count, string usage) { if (words.Length != count) throw new ArgumentException(usage); }
    static string SafeName(string name) => Name.IsMatch(name) && name.Length <= 80 ? name : throw new ArgumentException("Result name must be 1-80 lowercase letters, numbers, and single hyphens.");
    static string Readme => """
        # Agent results

        Durable handoffs, audits, reviews, and reports live here. Large or transient output belongs in the named transient directories and is ignored. Normal repository discovery excludes this directory; reference an artifact by path when it matters to a later independent chat.
        """;
    public static Result Init(string root)
    {
        var results = Root(root); SafeFiles.NoLinks(results); Directory.CreateDirectory(results);
        foreach (var directory in Directories) Directory.CreateDirectory(Path.Combine(results, directory));
        Directory.CreateDirectory(Path.Combine(results, "evals", "generated"));
        var readme = Path.Combine(results, "README.md"); var created = !File.Exists(readme);
        if (created) SafeFiles.Atomic(readme, Readme);
        return Result.Ok(new { initialized = results, directories = Directories, readmeCreated = created });
    }
    public static async Task<Result> New(string root, string[] words)
    {
        RequireWords(words, 2, "Usage: results new <audit|handoff|review|report> <name>.");
        var type = words[0]; var directory = Type(type); var name = SafeName(words[1]); Init(root); var now = DateTimeOffset.UtcNow;
        var head = (await Git.Require(root, "rev-parse", "HEAD")).Trim(); var branch = (await Processes.Run("git", ["branch", "--show-current"], root)).Output.Trim();
        var file = Path.Combine(Root(root), directory, $"{now:yyyyMMddTHHmmssZ}-{name}.md"); if (File.Exists(file)) throw new IOException("A result already exists for this timestamp and name; retry.");
        var title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(type) + ": " + name.Replace('-', ' ');
        SafeFiles.Atomic(file, $"""
            # {title}

            - Time (UTC): {now:O}
            - HEAD: {head}
            - Branch: {branch}
            - Status: draft

            ## Purpose

            ## Findings or summary

            ## Decisions

            ## Unresolved

            ## Follow-up

            ## Artifact paths
            """);
        return Result.Ok(new { path = file, type, name, timestampUtc = now, head, branch, status = "draft" });
    }
    static ResultFile[] Files(string root, string? type = null)
    {
        var results = Root(root); if (!Directory.Exists(results)) return [];
        var folders = type is null ? Persistent.Select(x => x.Value) : [Type(type)];
        return folders.SelectMany(folder => Directory.Exists(Path.Combine(results, folder)) ? Directory.EnumerateFiles(Path.Combine(results, folder), "*.md") : [])
            .Select(ResultFile.From).OrderByDescending(x => x.TimestampUtc).ThenByDescending(x => x.Path, StringComparer.Ordinal).ToArray();
    }
    public static Result List(string root, string[] words) { if (words.Length > 1) throw new ArgumentException("Usage: results list [audit|handoff|review|report]."); var files = Files(root, words.FirstOrDefault()); return Result.Ok(new { count = files.Length, results = files }); }
    public static Result Latest(string root, string[] words) { RequireWords(words, 1, "Usage: results latest <audit|handoff|review|report>."); return Result.Ok(new { result = Files(root, words[0]).FirstOrDefault() }); }
    public static Result Context(string root, string[] words) { RequireWords(words, 1, "Usage: results context <audit|handoff|review|report>."); var file = Files(root, words[0]).FirstOrDefault(); return Result.Ok(new { result = file is null ? null : new { file.Path, file.Type, file.TimestampUtc, file.Status, carryForward = file.CarryForward } }); }
    public static Result Clean(string root, bool dryRun)
    {
        var results = Root(root); if (!Directory.Exists(results)) return Result.Ok(new { dryRun, removed = 0, paths = Array.Empty<string>() }); SafeFiles.NoLinks(results); var paths = new List<string>();
        foreach (var directory in Transient) { var target = Path.Combine(results, directory); SafeFiles.NoLinks(target); if (Directory.Exists(target)) paths.AddRange(EnumerateTransient(target)); }
        if (!dryRun) foreach (var path in paths) File.Delete(path); return Result.Ok(new { dryRun, removed = paths.Count, paths });
    }
    static IEnumerable<string> EnumerateTransient(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory)) if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0) yield return file;
        foreach (var child in Directory.EnumerateDirectories(directory)) if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) foreach (var file in EnumerateTransient(child)) yield return file;
    }
}
public record ResultFile(string Path, string Type, DateTimeOffset TimestampUtc, string? Status, string CarryForward)
{
    public static ResultFile From(string path)
    {
        var stamp = System.IO.Path.GetFileNameWithoutExtension(path).Split('-', 2)[0]; if (!DateTimeOffset.TryParseExact(stamp, "yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp)) timestamp = File.GetLastWriteTimeUtc(path);
        var text = File.ReadAllText(path); var statusMatch = Regex.Match(text, @"(?m)^- Status: (.+)$");
        var carry = string.Join("\n", Regex.Matches(text, @"(?ms)^## (?:Unresolved|Follow-up)\r?\n(.*?)(?=^## |\z)").SelectMany(x => x.Groups[1].Value.Split('\n')).Select(x => x.Trim()).Where(x => x.Length > 0).Take(8));
        var directory = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)!); var type = new Dictionary<string, string> { ["audits"] = "audit", ["handoffs"] = "handoff", ["reviews"] = "review", ["reports"] = "report" }.GetValueOrDefault(directory, directory);
        return new(path, type, timestamp, statusMatch.Success ? statusMatch.Groups[1].Value : null, carry);
    }
}

public static class Projects
{
    public static string[] Discover(string root) => SafeFiles.Enumerate(root).Where(x => Path.GetExtension(x) is ".csproj" or ".fsproj" or ".vbproj").Order(StringComparer.Ordinal).ToArray();
    public static string[] Solutions(string root) => SafeFiles.Enumerate(root).Where(x => Path.GetExtension(x) is ".sln" or ".slnx").Order(StringComparer.Ordinal).ToArray();
    public static async Task<JsonNode> Evaluate(string root, string project)
    {
        var result = await Processes.Run("dotnet", ["msbuild", project, "-nologo", "-getProperty:TargetFramework,TargetFrameworks,IsTestProject,Nullable,ManagePackageVersionsCentrally,Deterministic,EnableNETAnalyzers,RestorePackagesWithLockFile,EnablePackageValidation", "-getItem:ProjectReference,Compile"], root);
        if (result.ExitCode != 0) throw new InvalidOperationException($"MSBuild evaluation failed for {Path.GetFileName(project)}; graph cannot safely be narrowed.");
        return JsonNode.Parse(result.Output) ?? throw new InvalidOperationException("Empty MSBuild response.");
    }
    public static async Task<bool> IsTest(string root, string project) => (await Evaluate(root, project))["Properties"]?["IsTestProject"]?.GetValue<string>().Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    public static async Task<Affected> Affected(string root, string[] changed)
    {
        root = Path.GetFullPath(root);
        var projects = Discover(root);
        if (changed.Length == 0) return new([], "No changed files.");
        var broad = changed.Any(x => x.EndsWith(".props", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".targets", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(x) is "global.json" or "NuGet.Config" or "nuget.config" || x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        if (broad) return new(projects, "Shared build, solution or project metadata changed; conservative full graph.");
        var selected = new HashSet<string>(StringComparer.Ordinal); var references = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var paths = changed.Select(x => Path.GetFullPath(x, root)).ToHashSet(StringComparer.Ordinal);
        foreach (var project in projects)
        {
            var evaluation = await Evaluate(root, project);
            if (!string.IsNullOrEmpty(evaluation["Properties"]?["TargetFrameworks"]?.GetValue<string>())) return new(projects, "Multi-targeted graph; conservative full graph (conditional inner builds may differ).");
            var items = evaluation["Items"];
            var compiles = items?["Compile"]?.AsArray().Select(x => x?["FullPath"]?.GetValue<string>()).OfType<string>().ToArray() ?? [];
            references[project] = items?["ProjectReference"]?.AsArray().Select(x => x?["FullPath"]?.GetValue<string>()).OfType<string>().ToArray() ?? [];
            if (compiles.Any(paths.Contains) || paths.Any(p => p.StartsWith(Path.GetDirectoryName(project)! + Path.DirectorySeparatorChar, StringComparison.Ordinal))) selected.Add(project);
        }
        // Removed linked files and custom build inputs cannot always be inferred from evaluated Compile items.
        if (paths.Any(p => !projects.Any(project => p.StartsWith(Path.GetDirectoryName(project)! + Path.DirectorySeparatorChar, StringComparison.Ordinal))))
            return new(projects, "Change outside project directories; conservative full graph for custom or removed linked inputs.");
        bool added;
        do { added = false; foreach (var p in projects) if (references[p].Any(selected.Contains)) added |= selected.Add(p); } while (added);
        return new(selected.Order(StringComparer.Ordinal).ToArray(), "Evaluated Compile/ProjectReference graph including transitive dependents.");
    }
    public static bool HasApiChecks(string path)
    {
        if (!path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) return false;
        var doc = XDocument.Load(path);
        return doc.Descendants().Any(x => x.Name.LocalName == "PackageReference" && x.Attribute("Include")?.Value == "Microsoft.CodeAnalysis.PublicApiAnalyzers") || doc.Descendants().Any(x => x.Name.LocalName == "PackageValidationBaselineVersion" && !string.IsNullOrWhiteSpace(x.Value));
    }
    public static async Task<List<object>> Health(string root, HealthSettings policy)
    {
        var findings = new List<object>(); var projects = Discover(root);
        if (projects.Length == 0) findings.Add(new { rule = "projects", message = "No project files found; file-based apps are not project-scanned." });
        foreach (var project in projects)
        {
            var props = (await Evaluate(root, project))["Properties"]!;
            foreach (var (name, expected, enabled) in new[] { ("Nullable", "enable", policy.RequireNullable), ("ManagePackageVersionsCentrally", "true", policy.RequireCentralPackages), ("Deterministic", "true", policy.RequireDeterministic), ("EnableNETAnalyzers", "true", policy.RequireAnalyzers), ("RestorePackagesWithLockFile", "true", policy.RequireLockFiles) })
                if (enabled && !string.Equals(props[name]?.GetValue<string>(), expected, StringComparison.OrdinalIgnoreCase)) findings.Add(new { project, rule = name, expected, actual = props[name]?.GetValue<string>() });
            var frameworks = (props["TargetFrameworks"]?.GetValue<string>() is { Length: > 0 } multi ? multi : props["TargetFramework"]?.GetValue<string>() ?? "").Split(';');
            foreach (var tfm in frameworks) if (policy.AllowedFrameworks.Length > 0 && !policy.AllowedFrameworks.Contains(tfm)) findings.Add(new { project, rule = "TargetFramework", actual = tfm });
        }
        if (!File.Exists(Path.Combine(root, "global.json"))) findings.Add(new { rule = "sdk", message = "Missing global.json SDK policy." });
        return findings;
    }
}
public record Affected(string[] Projects, string Reason);

public static class SafeFiles
{
    public static IEnumerable<string> Enumerate(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root)) if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0) yield return file;
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            if (new[] { ".git", ".agent-tool", ".agent-results", "bin", "obj", "node_modules", "artifacts", "TestResults" }.Contains(Path.GetFileName(dir)) || (File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) continue;
            foreach (var file in Enumerate(dir)) yield return file;
        }
    }
    public static void NoLinks(string path)
    {
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if (new FileInfo(current).LinkTarget is not null || new DirectoryInfo(current).LinkTarget is not null) throw new IOException("Refusing to write through a symlink in a managed state path.");
    }
    public static void Atomic(string path, string content)
    {
        NoLinks(path); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, content, new UTF8Encoding(false));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public static class Output
{
    public static string[] Compact(string text, OutputSettings limits) => text.Split('\n').Select(x => Secrets.Redact(x.TrimEnd('\r'))).Where(x => x.Length > 0).Distinct(StringComparer.Ordinal).Take(limits.MaxLines).Select(x => x.Length <= limits.MaxLineLength ? x : x[..limits.MaxLineLength] + "…").ToArray();
    public static object SummarizeFile(string path, OutputSettings limits)
    {
        var interesting = new List<string>(); var tail = new Queue<string>(); int lines = 0, errors = 0, warnings = 0;
        foreach (var line in File.ReadLines(path))
        {
            lines++;
            var zeroCount = Regex.IsMatch(line, @"^\s*0\s+(errors?|warnings?)\b", RegexOptions.IgnoreCase);
            var error = !zeroCount && Regex.IsMatch(line, @"\b(error|failed|failure|fatal)\b", RegexOptions.IgnoreCase);
            var warning = !zeroCount && Regex.IsMatch(line, @"\bwarning\b", RegexOptions.IgnoreCase);
            if (error) errors++; if (warning) warnings++;
            if ((error || warning) && interesting.Count < limits.MaxLines) interesting.Add(line);
            tail.Enqueue(line); if (tail.Count > limits.MaxLines) tail.Dequeue();
        }
        return new { lines, errorLines = errors, warningLines = warnings, evidence = Compact(string.Join('\n', interesting.Count > 0 ? interesting.AsEnumerable() : tail), limits), artifact = Path.GetFullPath(path), note = "Text counts are matching lines, not a build success verdict." };
    }
    public static object Sarif(string path, OutputSettings limits)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var rows = new List<object>(); int count = 0;
        foreach (var run in doc.RootElement.GetProperty("runs").EnumerateArray())
            if (run.TryGetProperty("results", out var results)) foreach (var result in results.EnumerateArray())
                {
                    count++;
                    if (rows.Count < limits.MaxItems) rows.Add(new { rule = result.TryGetProperty("ruleId", out var rule) ? rule.GetString() : null, level = result.TryGetProperty("level", out var level) ? level.GetString() : "warning", message = Compact(result.GetProperty("message").TryGetProperty("text", out var text) ? text.GetString() ?? "" : result.GetProperty("message").ToString(), limits), locations = result.TryGetProperty("locations", out var locations) ? locations.EnumerateArray().Take(1).Select(x => x.Clone()).ToArray() : [] });
                }
        return new { count, results = rows, truncated = count > rows.Count, artifact = Path.GetFullPath(path) };
    }
}
public static class Audit
{
    public static int Count(JsonNode? node) => node switch
    {
        JsonObject obj => obj.Sum(kv => kv.Key == "vulnerabilities" && kv.Value is JsonArray a ? a.Count : Count(kv.Value)),
        JsonArray arr => arr.Sum(Count),
        _ => 0
    };
}
public static class Secrets
{
    const string Pattern = @"(?i)(?:Bearer\s+[A-Za-z0-9._~+/=-]+|(?:api[_-]?key|password|secret|token)\s*[=:]\s*[^\s,;]+|-----BEGIN[^\r\n]*PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9]{20,}|sk-[A-Za-z0-9_-]{16,})";
    public static string Redact(string value)
    {
        value = JevCredentials.Redact(value);
        return Regex.Replace(value, Pattern, "[REDACTED]");
    }
    public static bool LooksSensitive(string value) => !string.Equals(value, Redact(value), StringComparison.Ordinal);
}

public static class JevCredentials
{
    public const string EnvironmentVariable = "TYPESAFE_API_KEY";
    public static string Status(Func<string, string?>? environment = null)
    {
        environment ??= Environment.GetEnvironmentVariable;
        return string.IsNullOrEmpty(environment(EnvironmentVariable)) ? "JEV credentials: unavailable" : "JEV credentials: configured";
    }
    internal static string? Read() => Environment.GetEnvironmentVariable(EnvironmentVariable);
    internal static bool IsConfigured(Func<string?> source) => !string.IsNullOrEmpty(source());
    internal static string Redact(string value)
    {
        var key = Read();
        return string.IsNullOrEmpty(key) ? value : value.Replace(key, "[REDACTED]", StringComparison.Ordinal);
    }
    internal static bool Contains(string value)
    {
        var key = Read();
        return !string.IsNullOrEmpty(key) && value.Contains(key, StringComparison.Ordinal);
    }
    internal static bool Authorize(HttpRequestMessage request, Func<string?> source)
    {
        var key = source();
        if (string.IsNullOrEmpty(key)) return false;
        try { request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key); return true; }
        catch (FormatException) { return false; }
    }
}

public record ToolkitSettings { public string[] OptionalTools { get; init; } = ["dotnet-trace", "dotnet-dump", "dotnet-counters", "dotnet-gcdump", "dotnet-monitor"]; }
public record OutputSettings { public int MaxLines { get; init; } = 12; public int MaxLineLength { get; init; } = 240; public int MaxItems { get; init; } = 30; public int MaxOutputChars { get; init; } = 16000; }
public record HealthSettings
{
    public bool RequireNullable { get; init; } = true;
    public bool RequireCentralPackages { get; init; } = true;
    public bool RequireDeterministic { get; init; } = true;
    public bool RequireAnalyzers { get; init; } = true;
    public bool RequireLockFiles { get; init; }
    public string[] AllowedFrameworks { get; init; } = ["net10.0"];
}
public record JevSettings
{
    public string Mode { get; init; } = "auto";
    public string ApiUrl { get; init; } = "https://api.typesafe.ai/v1/systemone";
    public string Model { get; init; } = "jev-latest";
    public int TimeoutSeconds { get; init; } = 15;
    public int MaxInputBytes { get; init; } = 16384;
    public int MaxCandidates { get; init; } = 25;
    public double IncludeThreshold { get; init; } = .70;
    public double ExcludeThreshold { get; init; } = .10;
    public double MinConfidence { get; init; } = .80;
    public int CacheHours { get; init; } = 24;
    public void Validate()
    {
        if (Mode is not ("off" or "auto" or "required") || TimeoutSeconds is < 1 or > 120 || MaxInputBytes is < 1 or > 65536 || MaxCandidates is < 1 or > 100 || CacheHours is < 0 or > 720 || !double.IsFinite(IncludeThreshold) || !double.IsFinite(ExcludeThreshold) || !double.IsFinite(MinConfidence) || ExcludeThreshold < 0 || IncludeThreshold > 1 || ExcludeThreshold >= IncludeThreshold || MinConfidence is < 0 or > 1) throw new ArgumentException("Invalid JEV configuration.");
        if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out var url) || url.Scheme != "https" || url.UserInfo.Length != 0 || url.Query.Length != 0 || url.Fragment.Length != 0) throw new ArgumentException("JEV endpoint must use HTTPS without credentials, query or fragment.");
    }
}
public record Settings(JevSettings Jev, OutputSettings Output, HealthSettings Health, ToolkitSettings Toolkit)
{
    public static Settings Load(string toolkit, Func<string, string?>? env = null)
    {
        env ??= Environment.GetEnvironmentVariable;
        T Read<T>(string name) where T : new() => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(toolkit, "config", name + ".json")), AgentTool.Json) ?? throw new ArgumentException("Empty configuration.");
        var jev = Read<JevSettings>("jev");
        jev = jev with { Mode = env("JEV_MODE") ?? jev.Mode, ApiUrl = env("TYPESAFE_API_URL") ?? jev.ApiUrl, Model = env("JEV_MODEL") ?? jev.Model, TimeoutSeconds = env("JEV_TIMEOUT_SECONDS") is { } timeout ? int.TryParse(timeout, out var seconds) ? seconds : throw new ArgumentException("Invalid JEV_TIMEOUT_SECONDS.") : jev.TimeoutSeconds };
        jev.Validate();
        var output = Read<OutputSettings>("output-limits");
        if (output.MaxLines is < 1 or > 100 || output.MaxLineLength is < 20 or > 2000 || output.MaxItems is < 1 or > 200 || output.MaxOutputChars is < 1024 or > 131072) throw new ArgumentException("Invalid output limits.");
        return new(jev, output, Read<HealthSettings>("repo-health"), Read<ToolkitSettings>("toolkit"));
    }
}

public sealed class JevClient
{
    readonly HttpClient http;
    readonly JevSettings settings;
    readonly string cacheDirectory;
    readonly Func<string?> credentialSource;
    public JevClient(HttpClient http, JevSettings settings, string cacheDirectory) : this(http, settings, cacheDirectory, JevCredentials.Read) { }
    internal JevClient(HttpClient http, JevSettings settings, string cacheDirectory, Func<string?> credentialSource)
    {
        this.http = http; this.settings = settings; this.cacheDirectory = cacheDirectory; this.credentialSource = credentialSource;
    }
    public static JsonObject Request(string kind, string state, string instructions, JsonNode? criteria, string model)
    {
        if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(instructions)) throw new ArgumentException("state and instructions are required.");
        if (kind is not ("noul" or "choice" or "score")) throw new ArgumentException("Unsupported judgment type.");
        if (kind == "choice" && (criteria is not JsonObject choice || choice.Count is < 2 or > 255)) throw new ArgumentException("Choice requires a criteria object with 2–255 named choices.");
        if (kind == "score" && (criteria is not JsonArray score || score.Count is < 2 or > 10)) throw new ArgumentException("Score requires 2–10 ordered criteria.");
        var question = new JsonObject { ["type"] = kind, ["instructions"] = instructions };
        if (criteria is not null) question["criteria"] = criteria.DeepClone();
        return new JsonObject { ["model"] = model, ["state"] = state, ["questions"] = new JsonObject { ["judgment"] = question } };
    }
    public static string Hash(JsonNode request, string endpoint)
    {
        static JsonNode? Canonical(JsonNode? node) => node switch
        {
            JsonObject o => new JsonObject(o.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new KeyValuePair<string, JsonNode?>(x.Key, Canonical(x.Value)))),
            JsonArray a => new JsonArray(a.Select(Canonical).ToArray()),
            _ => node?.DeepClone()
        };
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("jev-v1\n" + endpoint + "\n" + Canonical(request)!.ToJsonString())));
    }
    public static string Route(double probability, JevSettings settings) => !double.IsFinite(probability) || probability is < 0 or > 1 ? "REVIEW" : probability >= settings.IncludeThreshold ? "INCLUDE" : probability <= settings.ExcludeThreshold ? "EXCLUDE" : "REVIEW";
    public async Task<Result> Judge(JsonObject request)
    {
        settings.Validate();
        if (settings.Mode == "off") return Result.Review("JEV disabled.");
        if (!JevCredentials.IsConfigured(credentialSource)) return Fallback("JEV credentials unavailable.");
        var body = request.ToJsonString();
        if (Encoding.UTF8.GetByteCount(body) > settings.MaxInputBytes || Secrets.LooksSensitive(body)) return Fallback("Input too large or potentially sensitive.");
        var hash = Hash(request, settings.ApiUrl); var cache = Path.Combine(cacheDirectory, hash + ".json");
        try
        {
            SafeFiles.NoLinks(cache);
            if (settings.CacheHours > 0 && File.Exists(cache) && DateTime.UtcNow - File.GetLastWriteTimeUtc(cache) < TimeSpan.FromHours(settings.CacheHours))
            {
                try { return Parse(JsonNode.Parse(await File.ReadAllTextAsync(cache))!, request, true); }
                catch (Exception e) when (e is JsonException or InvalidOperationException or ArgumentException or KeyNotFoundException) { /* Invalid cache is ignored; no guessed decisions. */ }
            }
            using var message = new HttpRequestMessage(HttpMethod.Post, settings.ApiUrl);
            if (!JevCredentials.Authorize(message, credentialSource)) return Fallback("JEV credentials unavailable.");
            message.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(settings.TimeoutSeconds));
            using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode) return Fallback($"HTTP {(int)response.StatusCode}; response body withheld.");
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var memory = new MemoryStream(); var block = new byte[4096]; int count;
            while ((count = await stream.ReadAsync(block, timeout.Token)) > 0)
            { if (memory.Length + count > 65536) return Fallback("Response too large."); await memory.WriteAsync(block.AsMemory(0, count), timeout.Token); }
            var json = JsonNode.Parse(memory.ToArray()) ?? throw new JsonException();
            var parsed = Parse(json, request, false);
            if (settings.CacheHours > 0) { try { SafeFiles.Atomic(cache, CacheResponse(json, request).ToJsonString()); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* Cache is an optimization only. */ } }
            return parsed;
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or ArgumentException or KeyNotFoundException or IOException or UnauthorizedAccessException)
        { return Fallback("JEV unavailable or invalid response; no candidate discarded."); }
    }
    Result Fallback(string reason) => settings.Mode == "required" ? new("REVIEW", new { reason, fallback = "Codex", requiredFailed = true }, 3) : Result.Review(reason);
    static JsonObject CacheResponse(JsonNode response, JsonObject request)
    {
        var answer = response["answers"]!["judgment"]!; var kind = request["questions"]!["judgment"]!["type"]!.GetValue<string>();
        var cached = new JsonObject { ["type"] = kind };
        if (kind == "noul") cached["noul"] = answer["noul"]!.DeepClone();
        else
        {
            cached["confidence"] = answer["confidence"]!.DeepClone();
            cached["probabilities"] = answer["probabilities"]!.DeepClone();
            cached[kind] = answer[kind]!.DeepClone();
        }
        return new JsonObject { ["answers"] = new JsonObject { ["judgment"] = cached } };
    }
    public Result Parse(JsonNode response, JsonObject request, bool cached)
    {
        var q = request["questions"]!["judgment"]!; var kind = q["type"]!.GetValue<string>();
        var a = response["answers"]?["judgment"] ?? throw new JsonException("Missing answer.");
        if (a["type"]?.GetValue<string>() != kind) throw new JsonException("Wrong answer type.");
        double Number(string name, double max = 1)
        {
            var n = a[name]?.GetValue<double>() ?? throw new JsonException("Missing numeric answer.");
            if (!double.IsFinite(n) || n < 0 || n > max) throw new JsonException("Invalid numeric range."); return n;
        }
        if (kind == "noul") { var n = Number("noul"); return new(Route(n, settings), new { probability = n, cached }); }
        var confidence = Number("confidence"); var probabilities = a["probabilities"]?.AsObject() ?? throw new JsonException("Missing probability distribution.");
        var expected = kind == "choice" ? q["criteria"]!.AsObject().Select(x => x.Key).ToArray() : Enumerable.Range(0, q["criteria"]!.AsArray().Count).Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray();
        if (!expected.Order(StringComparer.Ordinal).SequenceEqual(probabilities.Select(x => x.Key).Order(StringComparer.Ordinal))) throw new JsonException("Wrong probability labels.");
        var values = probabilities.Select(x => x.Value!.GetValue<double>()).ToArray();
        if (values.Any(x => !double.IsFinite(x) || x is < 0 or > 1) || Math.Abs(values.Sum() - 1) > .01) throw new JsonException("Invalid probability distribution.");
        object value;
        if (kind == "choice")
        {
            var choice = a["choice"]?.GetValue<string>() ?? throw new JsonException("Missing choice.");
            if (!expected.Contains(choice) || probabilities[choice]!.GetValue<double>() + .001 < values.Max()) throw new JsonException("Invalid choice.");
            value = choice;
        }
        else
        {
            var score = Number("score", expected.Length - 1);
            var weighted = probabilities.Sum(x => int.Parse(x.Key, CultureInfo.InvariantCulture) * x.Value!.GetValue<double>());
            if (Math.Abs(score - weighted) > .02) throw new JsonException("Score and distribution disagree.");
            value = score;
        }
        return new(confidence >= settings.MinConfidence ? "ACCEPT" : "REVIEW", new { value, confidence, cached });
    }
}

public record InstallEntry(string Destination, string Source, bool Directory);
public record InstallManifest(string Toolkit, string Home, string CodexHome, List<InstallEntry> Entries);
public static class Installer
{
    static string ManifestPath(string codex) => Path.Combine(codex, "codex-toolkit-install.json");
    static bool Exists(string path) => File.Exists(path) || Directory.Exists(path) || new FileInfo(path).LinkTarget is not null || new DirectoryInfo(path).LinkTarget is not null;
    static bool Matches(InstallEntry e)
    {
        var target = e.Directory ? new DirectoryInfo(e.Destination).LinkTarget : new FileInfo(e.Destination).LinkTarget;
        return target is not null && string.Equals(Path.GetFullPath(target, Path.GetDirectoryName(e.Destination)!), e.Source, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
    static List<InstallEntry> Plan(string toolkit, string home, string codex, bool bin)
    {
        var entries = new List<InstallEntry> { new(Path.Combine(codex, "AGENTS.md"), Path.Combine(toolkit, "global/AGENTS.md"), false) };
        entries.AddRange(Directory.GetFiles(Path.Combine(toolkit, "agents"), "*.toml").Select(s => new InstallEntry(Path.Combine(codex, "agents", Path.GetFileName(s)), s, false)));
        entries.AddRange(Directory.GetDirectories(Path.Combine(toolkit, "plugins/codex-toolkit/skills")).Select(s => new InstallEntry(Path.Combine(home, ".agents/skills", Path.GetFileName(s)), s, true)));
        if (bin) entries.Add(new(Path.Combine(home, ".local/bin/codex-agent-tool"), Path.Combine(toolkit, "tools/AgentTool.cs"), false));
        return entries;
    }
    static InstallManifest? Read(string codex)
    {
        var path = ManifestPath(codex); SafeFiles.NoLinks(path);
        return File.Exists(path) ? JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(path), AgentTool.Json) ?? throw new IOException("Invalid installation manifest.") : null;
    }
    public static object Inspect(string codex)
    {
        var manifest = Read(codex);
        return manifest is null ? new { installed = false } : new { installed = true, entries = manifest.Entries.Select(e => new { e.Destination, healthy = Matches(e) && (File.Exists(e.Source) || Directory.Exists(e.Source)) }) };
    }
    public static Result Run(string toolkit, string home, string? codexHome, string command, bool dryRun, bool bin)
    {
        toolkit = Path.GetFullPath(toolkit); home = Path.GetFullPath(home); var codex = Path.GetFullPath(codexHome ?? Path.Combine(home, ".codex"));
        SafeFiles.NoLinks(codex);
        var manifest = Read(codex);
        if (manifest is not null && (manifest.Toolkit != toolkit || manifest.Home != home || manifest.CodexHome != codex)) throw new IOException("Installation belongs to a different checkout/home; use that checkout to uninstall first.");
        var allowed = Plan(toolkit, home, codex, true);
        if (manifest is not null && manifest.Entries.Any(e => !allowed.Contains(e))) throw new IOException("Ownership manifest contains unexpected paths; no changes made.");
        var plan = command == "uninstall" ? manifest?.Entries.ToList() ?? [] : Plan(toolkit, home, codex, bin || manifest?.Entries.Any(x => x.Destination == Path.Combine(home, ".local/bin/codex-agent-tool")) == true);
        var conflicts = plan.Where(e => Exists(e.Destination) && (manifest?.Entries.Contains(e) != true || !Matches(e))).Select(e => e.Destination).ToArray();
        if (command != "uninstall" && conflicts.Length > 0) return new("conflict", new { conflicts, changed = false }, 1);
        foreach (var entry in plan) SafeFiles.NoLinks(Path.GetDirectoryName(entry.Destination)!);
        if (dryRun) return Result.Ok(new { dryRun, command, plan, preserved = conflicts });
        if (plan.Count == 0) return Result.Ok(new { command, changed = 0 });
        Directory.CreateDirectory(codex);
        var lockPath = Path.Combine(codex, "codex-toolkit-install.lock"); SafeFiles.NoLinks(lockPath);
        using var installLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
        // Re-read under the lock so stale concurrent plans cannot overwrite ownership records.
        var current = Read(codex);
        if (JsonSerializer.Serialize(current, AgentTool.Json) != JsonSerializer.Serialize(manifest, AgentTool.Json)) throw new IOException("Installation changed concurrently; rerun command.");
        manifest ??= new(toolkit, home, codex, []);
        var changed = new List<string>(); var preserved = new List<string>();
        foreach (var entry in plan)
        {
            if (command == "uninstall")
            {
                if (Matches(entry)) { if (entry.Directory) Directory.Delete(entry.Destination); else File.Delete(entry.Destination); changed.Add(entry.Destination); }
                else if (Exists(entry.Destination)) preserved.Add(entry.Destination);
                manifest.Entries.Remove(entry);
                SafeFiles.Atomic(ManifestPath(codex), JsonSerializer.Serialize(manifest, AgentTool.Json));
            }
            else
            {
                if (manifest.Entries.Contains(entry) && Matches(entry)) continue;
                if (Exists(entry.Destination)) throw new IOException("Destination appeared during installation; rerun to inspect conflicts.");
                Directory.CreateDirectory(Path.GetDirectoryName(entry.Destination)!);
                if (entry.Directory) Directory.CreateSymbolicLink(entry.Destination, entry.Source); else File.CreateSymbolicLink(entry.Destination, entry.Source);
                manifest.Entries.Remove(entry);
                manifest.Entries.Add(entry);
                try { SafeFiles.Atomic(ManifestPath(codex), JsonSerializer.Serialize(manifest, AgentTool.Json)); }
                catch { if (Matches(entry)) { if (entry.Directory) Directory.Delete(entry.Destination); else File.Delete(entry.Destination); } throw; }
                changed.Add(entry.Destination);
            }
        }
        if (command == "uninstall") File.Delete(ManifestPath(codex));
        return Result.Ok(new { command, changed, preserved, note = "Only owned links changed; user replacements are preserved. Empty parent directories remain." });
    }
}

public static class Validation
{
    public static Result Run(string root)
    {
        var errors = new List<string>(); int parsed = 0;
        foreach (var file in SafeFiles.Enumerate(root))
        {
            if (new FileInfo(file).Length == 0) errors.Add($"Empty file: {Path.GetRelativePath(root, file)}");
            if (Path.GetExtension(file) == ".json")
                try { JsonNode.Parse(File.ReadAllText(file)); parsed++; } catch (JsonException) { errors.Add($"Invalid JSON: {file}"); }
        }
        foreach (var skill in Directory.GetDirectories(Path.Combine(root, "plugins/codex-toolkit/skills")))
        {
            var path = Path.Combine(skill, "SKILL.md");
            if (!File.Exists(path)) { errors.Add($"Missing skill: {skill}"); continue; }
            var text = File.ReadAllText(path);
            if (!text.StartsWith("---\n", StringComparison.Ordinal) || !text.Contains("\nname: " + Path.GetFileName(skill) + "\n", StringComparison.Ordinal) || !text.Contains("\ndescription: ", StringComparison.Ordinal)) errors.Add($"Invalid skill frontmatter: {skill}");
            if (!File.Exists(Path.Combine(root, "evals", Path.GetFileName(skill), "eval.yaml"))) errors.Add($"Missing evaluation: {skill}");
        }
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "plugins/codex-toolkit/.codex-plugin/plugin.json")))!;
        var mirror = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "plugins/codex-toolkit/plugin.json")))!;
        if (!JsonNode.DeepEquals(manifest, mirror) || manifest["name"]?.GetValue<string>() != "codex-toolkit" || manifest["skills"]?.GetValue<string>() != "./skills/") errors.Add("Plugin identity, mirror or skill path mismatch.");
        var marketplace = JsonNode.Parse(File.ReadAllText(Path.Combine(root, ".agents/plugins/marketplace.json")))!;
        if (marketplace["plugins"]?[0]?["source"]?["path"]?.GetValue<string>() != "./plugins/codex-toolkit") errors.Add("Marketplace source path mismatch.");
        try { Settings.Load(root, _ => null); } catch (ArgumentException e) { errors.Add(e.Message); }
        return new(errors.Count == 0 ? "ok" : "failed", new { jsonFiles = parsed, errors, note = "Fast structural validation. Automated tests additionally parse TOML/YAML and validate full JSON Schemas." }, errors.Count == 0 ? 0 : 1);
    }
}

public static class Evaluation
{
    public static Result Run(string toolkit, string? skill, string? resultsPath)
    {
        // Evaluation documents use JSON syntax, a strict YAML 1.2 subset, to stay dependency-free.
        var cases = Directory.GetFiles(Path.Combine(toolkit, "evals"), "eval.yaml", SearchOption.AllDirectories).Where(p => skill is null || Path.GetFileName(Path.GetDirectoryName(p)) == skill).ToArray();
        if (cases.Length == 0) throw new ArgumentException("No matching evaluation.");
        var runs = resultsPath is null ? null : JsonNode.Parse(File.ReadAllText(resultsPath))?.AsArray();
        var outcomes = new List<object>(); bool failed = false;
        foreach (var path in cases)
        {
            var specification = JsonNode.Parse(File.ReadAllText(path))!;
            var name = specification["skill"]!.GetValue<string>();
            if (runs is null)
            {
                var fixturePath = Path.GetFullPath(specification["fixture"]!.GetValue<string>(), Path.GetDirectoryName(path)!);
                var fixture = JsonNode.Parse(File.ReadAllText(fixturePath));
                var ok = fixture is JsonObject && specification["scenario"]?.GetValue<string>().Length > 10 && specification["expected"]?.GetValue<string>().Length > 10 && specification["safety"]?.GetValue<string>().Length > 10;
                failed |= !ok;
                outcomes.Add(new { skill = name, passed = ok, kind = "scenario/fixture integrity", agentBehaviorMeasured = false });
                continue;
            }
            var run = runs.SingleOrDefault(r => r?["skill"]?.GetValue<string>() == name);
            var failures = new List<string>();
            if (run is null) failures.Add("Missing run.");
            else
            {
                if (run["success"]?.GetValue<bool>() != true) failures.Add("Correctness failed.");
                foreach (var budget in specification["budgets"]!.AsObject())
                    if (run[budget.Key] is null || run[budget.Key]!.GetValue<double>() < 0 || run[budget.Key]!.GetValue<double>() > budget.Value!.GetValue<double>()) failures.Add($"Missing, invalid or exceeded {budget.Key}.");
                if (run["baseline"] is not JsonObject baseline || baseline["success"]?.GetValue<bool>() != true) failures.Add("Successful baseline required for comparison.");
                else if (baseline["tokens"] is null || baseline["tokens"]!.GetValue<double>() <= 0) failures.Add("Valid baseline token measurement required.");
                else if (run["tokens"] is not null && run["tokens"]!.GetValue<double>() > baseline["tokens"]!.GetValue<double>() * 1.10) failures.Add("Token use regressed more than 10% against baseline.");
            }
            failed |= failures.Count > 0;
            outcomes.Add(new { skill = name, passed = failures.Count == 0, failures });
        }
        return new(failed ? "failed" : "ok", new { outcomes, note = runs is null ? "Offline smoke only; does not claim token savings or agent success. Supply --results for measured regression gates." : "Measured runs checked against correctness and regression budgets." }, failed ? 1 : 0);
    }
}
