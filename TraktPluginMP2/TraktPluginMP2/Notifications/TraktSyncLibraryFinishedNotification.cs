namespace TraktPluginMP2.Notifications
{
  public class TraktSyncLibraryFinishedNotification : ITraktNotification
  {
    const string SUPER_LAYER_SCREEN = "TraktSyncLibraryFinishedNotification";

    private string _message;
    private bool _isSuccess;

    public TraktSyncLibraryFinishedNotification(string message, bool isSuccess)
    {
      _message = message;
      _isSuccess = isSuccess;
    }

    public string Message
    {
      get { return _message; }
    }

    public bool IsSuccess
    {
      get { return _isSuccess; }
    }

    public string SuperLayerScreenName
    {
      get { return SUPER_LAYER_SCREEN; }
    }
  }
}