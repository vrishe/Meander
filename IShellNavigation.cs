namespace Meander;

public interface IShellNavigation
{
    Task GoToAsync(string location);

    Task GoToAsync(string location, IDictionary<string, object> parameters);
}
