namespace CodexToolkit.Tests;

public static class ProcessRunner
{
    public static async Task<ProcessResult> Git(string root, params string[] args) => await Processes.Run("git", args, root);
}
