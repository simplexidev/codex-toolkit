using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Tomlyn;
using YamlDotNet.Serialization;

namespace CodexToolkit.Tests;

public class MetadataTests
{
    static string Root => AgentTool.FindToolkit();
    [Fact]
    public void AllJsonTomlAndYamlParse()
    {
        var yaml = new DeserializerBuilder().Build();
        foreach (var file in SafeFiles.Enumerate(Root))
        {
            if (Path.GetExtension(file) == ".json") { using var doc = JsonDocument.Parse(File.ReadAllText(file)); Assert.NotEqual(JsonValueKind.Undefined, doc.RootElement.ValueKind); }
            if (Path.GetExtension(file) == ".toml" || file.EndsWith(".toml.example", StringComparison.Ordinal)) Assert.False(Toml.Parse(File.ReadAllText(file)).HasErrors, file);
            if (Path.GetExtension(file) is ".yml" or ".yaml") Assert.NotNull(yaml.Deserialize<object>(File.ReadAllText(file)));
        }
    }
    [Fact]
    public void ConfigsConformToSchemas()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "config"), "*.json").Concat(Directory.GetFiles(Path.Combine(Root, "upstream"), "*.json")))
        {
            var instance = JsonNode.Parse(File.ReadAllText(file))!;
            var schemaPath = Path.GetFullPath(instance["$schema"]!.GetValue<string>(), Path.GetDirectoryName(file)!);
            var schema = JsonSchema.FromFile(schemaPath);
            var result = schema.Evaluate(instance, new() { OutputFormat = OutputFormat.List });
            Assert.True(result.IsValid, file + ": " + JsonSerializer.Serialize(result));
        }
    }
    [Fact]
    public void ReleaseIdentityIsOnePointZero()
    {
        var config = JsonNode.Parse(File.ReadAllText(Path.Combine(Root, "config/toolkit.json")))!;
        Assert.Equal("1.0.0", config["version"]!.GetValue<string>());
        Assert.Equal(0, Validation.Run(Root).ExitCode);
    }
    [Fact]
    public async Task ReleaseArchiveContainsUserFacingMetadataAndExcludesDevelopmentState()
    {
        using var repo = new TemporaryGitRepository(); var archive = Path.Combine(repo.Root, "codex-toolkit.zip");
        var result = await AgentTool.Execute(Cli.Parse(["release", "--output", archive]), Root, Root, Settings.Load(Root));
        Assert.Equal(0, result.ExitCode);
        using var zip = System.IO.Compression.ZipFile.OpenRead(archive); var entries = zip.Entries.Select(x => x.FullName).ToArray();
        Assert.Contains("CHANGELOG.md", entries); Assert.Contains("plugins/codex-toolkit/.codex-plugin/plugin.json", entries);
        Assert.DoesNotContain(entries, x => x.StartsWith("tests/", StringComparison.Ordinal) || x.StartsWith(".agent-results/", StringComparison.Ordinal) || x.StartsWith("artifacts/", StringComparison.Ordinal));
    }
    [Fact]
    public void SkillMetadataAndReferencesExist()
    {
        var yaml = new DeserializerBuilder().Build();
        foreach (var dir in Directory.GetDirectories(Path.Combine(Root, "plugins/codex-toolkit/skills")))
        {
            var text = File.ReadAllText(Path.Combine(dir, "SKILL.md")); var front = text.Split("---", 3)[1];
            var metadata = yaml.Deserialize<Dictionary<string, object>>(front);
            Assert.Equal(Path.GetFileName(dir), metadata["name"]); Assert.InRange(metadata["description"].ToString()!.Length, 20, 250);
            Assert.True(File.Exists(Path.Combine(dir, "agents/openai.yaml")));
            Assert.True(File.Exists(Path.Combine(Root, "evals", Path.GetFileName(dir), "eval.yaml")));
        }
        Assert.Equal(0, Validation.Run(Root).ExitCode);
    }
    [Fact]
    public void NativeAgentsHaveRequiredFields()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "agents"), "*.toml"))
        {
            var agent = Toml.ToModel(File.ReadAllText(file));
            Assert.True(agent.ContainsKey("name")); Assert.True(agent.ContainsKey("description")); Assert.True(agent.ContainsKey("developer_instructions")); Assert.Equal("read-only", agent["sandbox_mode"]);
        }
    }
    [Fact]
    public void SkillActivationMetadataStaysNarrowAndCredentialFree()
    {
        var yaml = new DeserializerBuilder().Build();
        Assert.False(Directory.Exists(Path.Combine(Root, "plugins/codex-toolkit/skills/repo-locate")));
        Assert.False(Directory.Exists(Path.Combine(Root, "evals/repo-locate")));
        Assert.Equal(new[] { "reviewer.toml" }, Directory.GetFiles(Path.Combine(Root, "agents"), "*.toml").Select(Path.GetFileName).Order());
        foreach (var skill in Directory.GetDirectories(Path.Combine(Root, "plugins/codex-toolkit/skills")))
        {
            var name = Path.GetFileName(skill);
            var metadata = yaml.Deserialize<Dictionary<string, object>>(File.ReadAllText(Path.Combine(skill, "agents/openai.yaml")));
            var ui = (Dictionary<object, object>)metadata["interface"];
            Assert.Equal($"Use ${name}.", ui["default_prompt"]);
            Assert.DoesNotContain("...", ui["short_description"].ToString());
            Assert.DoesNotContain("TYPESAFE_API_KEY", File.ReadAllText(Path.Combine(skill, "SKILL.md")));
            Assert.DoesNotContain("Commands above use", File.ReadAllText(Path.Combine(skill, "SKILL.md")));
        }
        Assert.Contains("allow_implicit_invocation: false", File.ReadAllText(Path.Combine(Root, "plugins/codex-toolkit/skills/jev-judgment/agents/openai.yaml")));
        Assert.Contains("allow_implicit_invocation: false", File.ReadAllText(Path.Combine(Root, "plugins/codex-toolkit/skills/package-audit/agents/openai.yaml")));
    }

    [Fact]
    public void LiveJevWorkflowIsManuallyOrLowFrequencyTriggeredAndSecretIsStepScoped()
    {
        var workflow = File.ReadAllText(Path.Combine(Root, ".github/workflows/jev-integration.yml"));
        Assert.Contains("workflow_dispatch:", workflow);
        Assert.Contains("cron: '23 10 * * 3'", workflow);
        Assert.DoesNotContain("pull_request:", workflow);
        Assert.Contains("environment: jev-integration", workflow);
        Assert.Contains("TYPESAFE_API_KEY: ${{ secrets.TYPESAFE_API_KEY }}", workflow);
        Assert.Contains("JEV_MODE: required", workflow);
        Assert.DoesNotContain("permissions: write-all", workflow);
        Assert.DoesNotContain("echo $TYPESAFE_API_KEY", workflow);
        Assert.DoesNotContain("printf '%s\\n' \"$TYPESAFE_API_KEY\"", workflow);
    }

    [Fact]
    public void ReleaseChecksumsUseDownloadableAssetNames()
    {
        var workflow = File.ReadAllText(Path.Combine(Root, ".github/workflows/release.yml"));
        Assert.Contains("(cd artifacts && sha256sum codex-toolkit.zip > SHA256SUMS)", workflow);
        Assert.DoesNotContain("sha256sum artifacts/codex-toolkit.zip > artifacts/SHA256SUMS", workflow);
    }
}

public class RepositoryIntegrityTests
{
    [Fact]
    public void LocalMarkdownLinksResolve()
    {
        var root = AgentTool.FindToolkit();
        foreach (var file in SafeFiles.Enumerate(root).Where(p => p.EndsWith(".md", StringComparison.Ordinal)))
        {
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file), @"\]\(([^)]+)\)"))
            {
                var target = match.Groups[1].Value.Split('#')[0];
                if (target.Length == 0 || target.Contains("://", StringComparison.Ordinal)) continue;
                var resolved = Path.GetFullPath(target, Path.GetDirectoryName(file)!);
                Assert.True(File.Exists(resolved) || Directory.Exists(resolved), $"Broken link in {file}: {target}");
            }
        }
    }
    [Fact]
    public async Task GeneratedStateIsIgnored()
    {
        var root = AgentTool.FindToolkit();
        var result = await Processes.Run("git", new[] { "check-ignore", ".agent-tool/jev-cache/example.json", "artifacts/release.zip", "TestResults/result.trx" }, root);
        Assert.Equal(0, result.ExitCode); Assert.Equal(3, result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }
    [Fact]
    public void SchemaRejectsInvalidConfiguration()
    {
        var schema = JsonSchema.FromFile(Path.Combine(AgentTool.FindToolkit(), "schemas/jev-config.schema.json"));
        var config = JsonNode.Parse(File.ReadAllText(Path.Combine(AgentTool.FindToolkit(), "config/jev.json")))!;
        config["timeoutSeconds"] = -1;
        Assert.False(schema.Evaluate(config).IsValid);
    }
}
