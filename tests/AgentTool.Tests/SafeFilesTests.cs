namespace CodexToolkit.Tests;

public class SafeFilesTests
{
    [Fact]
    public void AtomicRefusesReportSymlinkAndReplacesOrdinaryReport()
    {
        using var repo = new TemporaryGitRepository();
        var sentinel = Path.Combine(repo.Root, "sentinel"); File.WriteAllText(sentinel, "user-owned sentinel");
        var report = Path.Combine(repo.Root, ".agent-tool", "upstream-drift.json"); Directory.CreateDirectory(Path.GetDirectoryName(report)!); File.CreateSymbolicLink(report, sentinel);
        Assert.Throws<IOException>(() => SafeFiles.Atomic(report, "[]"));
        Assert.Equal("user-owned sentinel", File.ReadAllText(sentinel));
        File.Delete(report); SafeFiles.Atomic(report, "[]"); Assert.Equal("[]", File.ReadAllText(report));
    }
}
