namespace TraktPluginMP2.Notifications
{
  public class TraktSyncLibrarySuccessNotification : ITraktNotification
  {
    private const string SUPER_LAYER_SCREEN = "TraktSyncLibrarySuccessNotification";

    public string SuperLayerScreenName
    {
      get { return SUPER_LAYER_SCREEN; }
    }
  }
}