using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;

namespace TraktPluginMP2.Settings.Configuration
{
  public class LibrarySyncSetting : SingleSelectionList
  {
    public LibrarySyncSetting()
    {
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.SyncSeriesAndMovies]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.SyncOnlySeries]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Plugins.Trakt.SyncOnlyMovies]"));
    }

    public override void Load()
    {
      TraktPluginSettings settings = SettingsManager.Load<TraktPluginSettings>();
      if (settings.SyncSeriesAndMovies && !settings.SyncOnlySeries && !settings.SyncOnlyMovies)
      {
        Selected = 0;
      }
      else if (!settings.SyncSeriesAndMovies && !settings.SyncOnlyMovies)
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
        settings.SyncSeriesAndMovies = true;
        settings.SyncOnlyMovies = false;
        settings.SyncOnlySeries = false;
      }
      else if (Selected == 1)
      {
        settings.SyncOnlySeries = true;
        settings.SyncSeriesAndMovies = false;
        settings.SyncOnlyMovies = false;
      }
      else
      {
        settings.SyncOnlyMovies = true;
        settings.SyncSeriesAndMovies = false;
        settings.SyncOnlySeries = false;
      }
      SettingsManager.Save(settings);
    }
  }
}