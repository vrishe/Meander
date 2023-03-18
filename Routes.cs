namespace Meander;

internal static class Routes
{
    public const string EditSignalTrackRoute = "main/edit-signal";
    public const string EditSignalTrackUrl = "///" + EditSignalTrackRoute;

    public static class EditSignalTrackQueryParams
    {
        public const string TrackId = "trackId";
    }

    public static string EditSignalTrackUrlFormat(Guid trackId) => $"{EditSignalTrackUrl}?{EditSignalTrackQueryParams.TrackId}={trackId}";

    public const string MainPageRoute = "main";
    public const string MainPageUrl = "//" + MainPageRoute;

    public const string ProjectSetupRoute = "new-project-setup";
    public const string ProjectSetupUrl = ProjectSetupRoute;

    public const string StartupPageRoute = "startup";
    public const string StartupPageUrl = "//" + StartupPageRoute;
}
