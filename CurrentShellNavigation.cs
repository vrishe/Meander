﻿using Microsoft.Extensions.Logging;

namespace Meander;

internal sealed class CurrentShellNavigation : IShellNavigation
{
    private readonly ILogger _logger;

    public CurrentShellNavigation(ILoggerProvider loggerProvider)
    {
        _logger = loggerProvider.CreateLogger(nameof(CurrentShellNavigation));
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
        _logger.LogWarning($"Cannot go to {location}. There is no current {nameof(Shell)} available.");
        var tcs = new TaskCompletionSource<object>();
        tcs.SetCanceled();
        return tcs.Task;
    }
}
