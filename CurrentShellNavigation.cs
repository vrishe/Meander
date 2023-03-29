using Microsoft.Extensions.Logging;

namespace Meander;

internal sealed class CurrentShellNavigation : IShellNavigation
{
    private readonly ILogger _logger;

    public CurrentShellNavigation(ILogger<App> logger)
    {
        _logger = logger;
    }

    public Task GoToAsync(string location)
    {
        EnsureMainThread();

        var shell = Shell.Current;
        if (shell is not null)
            return shell.GoToAsync(location);

        return MissingShellTask(location);
    }

    public Task GoToAsync(string location, IDictionary<string, object> parameters)
    {
        EnsureMainThread();

        var shell = Shell.Current;
        if (shell is not null)
            return shell.GoToAsync(location, parameters);

        return MissingShellTask(location);
    }

    private static void EnsureMainThread()
    {
        if (!MainThread.IsMainThread)
            throw new InvalidOperationException($"{nameof(CurrentShellNavigation)} is supported on the main thread ONLY.");
    }

    private Task MissingShellTask(string location)
    {
        _logger.LogWarning("Cannot go to {}. There is no current {} available.", location, nameof(Shell));
        var tcs = new TaskCompletionSource<object>();
        tcs.SetCanceled();
        return tcs.Task;
    }
}
