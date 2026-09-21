namespace CodexToolkit.Tests;

public sealed class TemporaryGitRepository : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "codex-toolkit-tests-" + Guid.NewGuid().ToString("N"));
    public TemporaryGitRepository()
    {
        Directory.CreateDirectory(Root);
        Run("init", "-b", "main"); Run("config", "user.name", "Toolkit Tests"); Run("config", "user.email", "tests@example.invalid");
        Write(".gitignore", ".agent-tool/\n**/bin/\n**/obj/\n");
        Commit();
    }
    public void Write(string path, string contents) { var file = Path.Combine(Root, path); Directory.CreateDirectory(Path.GetDirectoryName(file)!); File.WriteAllText(file, contents); }
    public string Run(params string[] args) => Git.Require(Root, args).GetAwaiter().GetResult();
    public void Commit() { Run("add", "."); Run("commit", "-m", "fixture"); }
    public void Dispose() { try { Directory.Delete(Root, true); } catch (IOException) { } }
}
