namespace Meander;

public interface IFilePersistencyService
{
    Task ExportProjectAsync();

    Task ImportProjectAsync();
}
