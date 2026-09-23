using System.Text.Json;

namespace CodexToolkit.Tests;

public class DeterministicPrimitivesTests
{
    const string Project = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";

    [Fact]
    public async Task RepositorySummaryIsBoundedAndClassified()
    {
        using var repo = new TemporaryGitRepository(); repo.Write("src/A.cs", "class A {}"); repo.Write("docs/note.md", "note");
        var node = JsonSerializer.SerializeToNode(await Repository.Summary(repo.Root, null, new() { MaxItems = 1 }), AgentTool.Json)!;
        Assert.Equal("repository-summary", node["kind"]!.GetValue<string>());
        Assert.Equal(2, node["changes"]!["count"]!.GetValue<int>());
        Assert.True(node["changes"]!["truncated"]!.GetValue<bool>());
        Assert.Equal(1, node["changes"]!["byExtension"]![".cs"]!.GetValue<int>());
    }

    [Fact]
    public async Task OwnershipUsesCompileGraphAndIncludesTransitiveImpact()
    {
        using var repo = new TemporaryGitRepository();
        repo.Write("A/A.csproj", Project); repo.Write("A/A.cs", "class A {}");
        repo.Write("B/B.csproj", Project.Replace("</Project>", "<ItemGroup><ProjectReference Include=\"../A/A.csproj\" /></ItemGroup></Project>", StringComparison.Ordinal));
        repo.Write("C/C.csproj", Project.Replace("</Project>", "<ItemGroup><ProjectReference Include=\"../B/B.csproj\" /></ItemGroup></Project>", StringComparison.Ordinal));
        var node = JsonSerializer.SerializeToNode(await Projects.Ownership(repo.Root, "A/A.cs"), AgentTool.Json)!;
        Assert.Equal("evaluated-compile-item", node["basis"]!.GetValue<string>());
        Assert.Equal(3, node["impactedProjects"]!.AsArray().Count);
    }

    [Fact]
    public async Task InspectionNormalizesProjectGraphToRepositoryPaths()
    {
        using var repo = new TemporaryGitRepository(); repo.Write("src/A.csproj", Project);
        var node = JsonSerializer.SerializeToNode(await DotnetFacts.Inspect(repo.Root, null), AgentTool.Json)!;
        Assert.Equal("src/A.csproj", node["projects"]![0]!["path"]!.GetValue<string>());
        Assert.Equal("net10.0", node["projects"]![0]!["targetFrameworks"]![0]!.GetValue<string>());
        Assert.NotNull(node["environment"]!["sdk"]);
    }

    [Fact]
    public async Task SolutionInspectionExcludesProjectsOutsideTheSolution()
    {
        using var repo = new TemporaryGitRepository(); repo.Write("src/A.csproj", Project); repo.Write("other/B.csproj", Project); repo.Write("App.slnx", "<Solution><Project Path=\"src/A.csproj\" /></Solution>");
        var node = JsonSerializer.SerializeToNode(await DotnetFacts.Inspect(repo.Root, "App.slnx"), AgentTool.Json)!;
        Assert.Single(node["projects"]!.AsArray()); Assert.Equal("src/A.csproj", node["projects"]![0]!["path"]!.GetValue<string>());
    }

    [Fact]
    public async Task PlansAreStructuredAndDoNotExecuteBuilds()
    {
        using var repo = new TemporaryGitRepository();
        repo.Write("src/A.csproj", Project); repo.Write("tests/A.Tests.csproj", Project.Replace("</PropertyGroup>", "<IsTestProject>true</IsTestProject></PropertyGroup>").Replace("</Project>", "<ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"18.0.1\" /><ProjectReference Include=\"../src/A.csproj\" /></ItemGroup></Project>", StringComparison.Ordinal));
        var build = JsonSerializer.SerializeToNode(await DotnetFacts.BuildPlan(repo.Root, "src/A.csproj", null, "Release", true), AgentTool.Json)!;
        Assert.Equal("restore", build["commands"]![0]!["arguments"]![0]!.GetValue<string>());
        Assert.Contains("-bl:.agent-tool/binlogs/", build["commands"]![1]!["arguments"]!.AsArray().Last()!.GetValue<string>(), StringComparison.Ordinal);
        var test = JsonSerializer.SerializeToNode(await DotnetFacts.TestPlan(repo.Root, "src/A.csproj", null, "Release", "Category=Fast"), AgentTool.Json)!;
        Assert.Equal("vstest", test["tests"]![0]!["platform"]!.GetValue<string>());
        Assert.Contains("Category=Fast", test["tests"]![0]!["command"]!["arguments"]!.AsArray().Select(value => value!.GetValue<string>()));
        Assert.False(Directory.Exists(Path.Combine(repo.Root, ".agent-tool")));
    }

    [Fact]
    public void ParsesTrxAndJUnitResults()
    {
        var root = AgentTool.FindToolkit();
        var trx = JsonSerializer.SerializeToNode(DotnetArtifacts.TestResults(Path.Combine(root, "tests/fixtures/test-results/sample.trx"), new()), AgentTool.Json)!;
        var junit = JsonSerializer.SerializeToNode(DotnetArtifacts.TestResults(Path.Combine(root, "tests/fixtures/test-results/sample.junit.xml"), new()), AgentTool.Json)!;
        Assert.Equal(3, trx["total"]!.GetValue<int>()); Assert.Equal(500, trx["durationMilliseconds"]!.GetValue<double>());
        Assert.Equal(1, junit["outcomes"]!["Failed"]!.GetValue<int>()); Assert.Equal(750, junit["durationMilliseconds"]!.GetValue<double>());
    }

    [Theory]
    [InlineData("cobertura.xml", "cobertura")]
    [InlineData("opencover.xml", "opencover")]
    public void ParsesCoverageFormats(string file, string format)
    {
        var path = Path.Combine(AgentTool.FindToolkit(), "tests/fixtures/coverage", file);
        var node = JsonSerializer.SerializeToNode(DotnetArtifacts.Coverage(path, new()), AgentTool.Json)!;
        Assert.Equal(format, node["format"]!.GetValue<string>()); Assert.Equal(75, node["branches"]!["percent"]!.GetValue<double>());
    }

    [Fact]
    public void DiagnosticsPlanRejectsInvalidPidAndReportsTools()
    {
        Assert.Throws<ArgumentException>(() => DotnetFacts.DiagnosticsPlan("0"));
        var node = JsonSerializer.SerializeToNode(DotnetFacts.DiagnosticsPlan("42"), AgentTool.Json)!;
        Assert.Equal(5, node["tools"]!.AsArray().Count); Assert.Equal(4, node["plans"]!.AsArray().Count);
        Assert.Equal("dotnet-counters", node["plans"]![0]!["executable"]!.GetValue<string>());
        Assert.Equal("monitor", node["plans"]![0]!["arguments"]![0]!.GetValue<string>());
    }

    [Fact]
    public void DependencyInventorySeparatesDirectAndTransitivePackages()
    {
        var root = AgentTool.FindToolkit(); var path = Path.Combine(root, "tests/fixtures/nuget/dependency-report.json");
        var node = JsonSerializer.SerializeToNode(DotnetArtifacts.Dependencies(path, root, new()), AgentTool.Json)!;
        Assert.Equal(1, node["direct"]!.GetValue<int>()); Assert.Equal(1, node["transitive"]!.GetValue<int>());
        Assert.Equal("1.0.1", node["packages"]![0]!["resolvedVersion"]!.GetValue<string>());
    }
}
