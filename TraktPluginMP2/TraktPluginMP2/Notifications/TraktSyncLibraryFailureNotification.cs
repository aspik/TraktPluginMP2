namespace TraktPluginMP2.Notifications
{
  public class TraktSyncLibraryFailureNotification : ITraktNotification
  {
    private const string SUPER_LAYER_SCREEN = "TraktSyncLibraryFailureNotification";

    private string _errorMessage;

    public TraktSyncLibraryFailureNotification(string errorMessage)
    {
      _errorMessage = errorMessage;
    }

    public string ErrorMessage
    {
      get { return _errorMessage; }
    }

    public string SuperLayerScreenName
    {
      get { return SUPER_LAYER_SCREEN; }
    }
  }
}