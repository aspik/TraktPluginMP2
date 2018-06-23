namespace TraktPluginMP2.Notifications
{
  public abstract class TraktScrobbleNotificationBase : ITraktNotification
  {
    protected string _message;
    protected bool _isSuccess;
    protected int? _progress;
    protected string _actionType;

    protected TraktScrobbleNotificationBase(string message, bool isSuccess, int? progress, string actionType)
    {
      _message = message;
      _isSuccess = isSuccess;
      _progress = progress;
      _actionType = actionType;
    }

    public string Message
    {
      get { return _message; }
    }

    public bool IsSuccess
    {
      get { return _isSuccess; }
    }

    public int? Progress
    {
      get { return _progress; }
    }

    public string ActionType
    {
      get { return _actionType; }
    }

    public abstract string SuperLayerScreenName { get; }
  }
}