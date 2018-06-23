namespace TraktPluginMP2.Notifications
{
  public class TraktScrobbleStartedNotification : TraktScrobbleNotificationBase
  {
    private const string SUPER_LAYER_SCREEN = "TraktScrobbleStartedNotification";

    public TraktScrobbleStartedNotification(string message, bool isSuccess, int? progress, string actionType) : base(message, isSuccess, progress, actionType)
    {
    }

    public override string SuperLayerScreenName
    {
      get { return SUPER_LAYER_SCREEN; }
    }
  }
}