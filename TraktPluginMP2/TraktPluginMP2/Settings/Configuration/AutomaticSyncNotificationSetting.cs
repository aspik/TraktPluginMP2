using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;

namespace TraktPluginMP2.Settings.Configuration
{
  public class AutomaticSyncNotificationSetting : SingleSelectionList
  {
    public AutomaticSyncNotificationSetting()
    {
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.AlwaysShowNotification]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.ShowNotificationOnFailure]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.DisableNotifications]"));
    }

    public override void Load()
    {
      TraktPluginSettings settings = SettingsManager.Load<TraktPluginSettings>();
      if (settings.ShowAutomaticSyncNotifications && settings.ShowAutomaticSyncNotificationsOnFailure)
      {
        Selected = 0;
      }
      else if (!settings.ShowAutomaticSyncNotifications && settings.ShowAutomaticSyncNotificationsOnFailure)
      {
        Selected = 1;
      }
      else
      {
        Selected = 2;
      }
    }

    public override void Save()
    {
      base.Save();
      TraktPluginSettings settings = SettingsManager.Load<TraktPluginSettings>();

      if (Selected == 0)
      {
        settings.ShowAutomaticSyncNotifications = true;
        settings.ShowAutomaticSyncNotificationsOnFailure = true;
      }
      else if (Selected == 1)
      {
        settings.ShowAutomaticSyncNotificationsOnFailure = true;
        settings.ShowAutomaticSyncNotifications = false;
      }
      else
      {
        settings.ShowAutomaticSyncNotificationsOnFailure = false;
        settings.ShowAutomaticSyncNotifications = false;
      }
      SettingsManager.Save(settings);
    }
  }
}