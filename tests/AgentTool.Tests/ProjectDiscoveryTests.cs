namespace CodexToolkit.Tests;

public class ProjectDiscoveryTests
{
    public const string Project = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";
    [Fact] public void FallsBackToWorkingDirectoryWhenSourceLinkPathIsUnavailable() { var root = AgentTool.FindToolkit(source: "/_/tools/AgentTool.cs"); Assert.True(File.Exists(Path.Combine(root, "config", "toolkit.json"))); }
    [Fact] public void IgnoresGeneratedAndSymlinkDirectories() { using var repo = new TemporaryGitRepository(); repo.Write("src/A.csproj", Project); repo.Write("obj/Generated.csproj", Project); Directory.CreateSymbolicLink(Path.Combine(repo.Root, "loop"), repo.Root); Assert.Single(Projects.Discover(repo.Root)); }
    [Fact] public async Task ReverseDependentsAreSelected() { using var repo = new TemporaryGitRepository(); repo.Write("A/A.csproj", Project); repo.Write("A/A.cs", "class A {}"); repo.Write("B/B.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup><ProjectReference Include=\"../A/A.csproj\" /></ItemGroup></Project>"); repo.Write("C/C.csproj", Project); var result = await Projects.Affected(repo.Root, ["A/A.cs"]); Assert.Equal(2, result.Projects.Length); Assert.DoesNotContain(result.Projects, p => p.EndsWith("C.csproj", StringComparison.Ordinal)); }
    [Fact] public async Task SharedPropsWidenScope() { using var repo = new TemporaryGitRepository(); repo.Write("A/A.csproj", Project); repo.Write("B/B.csproj", Project); Assert.Equal(2, (await Projects.Affected(repo.Root, ["Directory.Build.props"])).Projects.Length); }
    [Fact] public async Task MultiTargetedGraphWidensConservatively() { using var repo = new TemporaryGitRepository(); repo.Write("A/A.csproj", Project.Replace("<TargetFramework>net10.0</TargetFramework>", "<TargetFrameworks>net9.0;net10.0</TargetFrameworks>", StringComparison.Ordinal)); repo.Write("A/a.cs", "class A {}"); repo.Write("B/B.csproj", Project); Assert.Equal(2, (await Projects.Affected(repo.Root, ["A/a.cs"])).Projects.Length); }
    [Fact] public async Task NoChangesMeansNoBuilds() { using var repo = new TemporaryGitRepository(); repo.Write("A.csproj", Project); Assert.Empty((await Projects.Affected(repo.Root, [])).Projects); }
}
