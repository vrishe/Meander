using System.IO.MemoryMappedFiles;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Storage;
using Meander.State;
using Meander.State.Actions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReduxSimple;

namespace Meander;

internal sealed class DefaultFilePersistencyService : IFilePersistencyService
{
    public const string WellKnownProjectExtension = ".mjp";

    private readonly ILogger _logger;
    private readonly JsonSerializer _serialzier;
    private readonly ReduxStore<GlobalState> _store;

    public DefaultFilePersistencyService(ILogger<App> logger, ReduxStore<GlobalState> store)
    {
        _logger = logger;
        _serialzier = JsonSerializer.CreateDefault();
        _store = store;
    }

    public async Task ExportProjectAsync()
    {
        var state = _store.State;
        var tempFile = Path.Join(FileSystem.Current.CacheDirectory, $"project_{Guid.NewGuid():N}");
        await Task.Run(() =>
        {
            using var w = new StreamWriter(tempFile);
            using var jtw = new JsonTextWriter(w);
            _serialzier.Serialize(jtw, state);
        }).ConfigureAwait(false);

        GetOutputPathAndFileName(state, tempFile, out string exportPath, out string fileName);

        try
        {
            using var mmf = MemoryMappedFile.CreateFromFile(tempFile, FileMode.Open);
            using var view = mmf.CreateViewStream();

            var result = await MainThread.InvokeOnMainThreadAsync(() => FileSaver.SaveAsync(exportPath, fileName, view, CancellationToken.None));
            if (!result.IsSuccessful)
                _logger.LogError(result.Exception, "Fail to export current project at: {}", exportPath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Fail to export current project at: {}", exportPath);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* ignore */ }
        }
    }

    public async Task ImportProjectAsync()
    {
        var result = await MainThread.InvokeOnMainThreadAsync(() => FilePicker.PickAsync(new()
        {
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.WinUI] = new[] { WellKnownProjectExtension, ".json", ".txt" }
            }),
        })).ConfigureAwait(false);

        if (result == null)
            throw new OperationCanceledException();

        using var r = new StreamReader(result.FullPath);
        using var jtr = new JsonTextReader(r);
        var state = _serialzier.Deserialize<GlobalState>(jtr) with
        {
            SourceFilePath = result.FullPath
        };

        await MainThread.InvokeOnMainThreadAsync(
            () => _store.Dispatch(new ReplaceStateAction { NewState = state }));
    }

    private static void GetOutputPathAndFileName(GlobalState state, string tempFile, out string exportPath, out string fileName)
    {
        fileName = null;
        if (!string.IsNullOrEmpty(state.ProjectName))
        {
            var re = new Regex($"[{new string(Path.GetInvalidFileNameChars())}]+");
            fileName = Path.ChangeExtension(re.Replace(state.ProjectName, string.Empty), WellKnownProjectExtension);
        }
        if (!string.IsNullOrEmpty(state.SourceFilePath))
        {
            exportPath = Path.GetDirectoryName(state.SourceFilePath);
            if (string.IsNullOrEmpty(fileName))
                fileName = Path.GetFileName(state.SourceFilePath);
        }
        else exportPath = FileSystem.Current.AppDataDirectory;

        if (string.IsNullOrEmpty(fileName))
            fileName = Path.ChangeExtension(Path.GetFileName(tempFile), WellKnownProjectExtension);
    }
}
