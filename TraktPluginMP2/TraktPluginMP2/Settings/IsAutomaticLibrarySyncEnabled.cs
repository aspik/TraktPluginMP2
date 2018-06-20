using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace TraktPluginMP2.Settings
{
  public class IsAutomaticLibrarySyncEnabled : YesNo
  {
    public override void Load()
    {
      _yes = SettingsManager.Load<TraktPluginSettings>().IsAutomaticLibrarySyncEnabled;
    }

    public override void Save()
    {
      base.Save();
      TraktPluginSettings settings = SettingsManager.Load<TraktPluginSettings>();
      settings.IsAutomaticLibrarySyncEnabled = _yes;
      SettingsManager.Save(settings);
    }
  }
}