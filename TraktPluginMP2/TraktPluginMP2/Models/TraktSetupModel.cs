using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MediaPortal.Common.General;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using TraktNet.Objects.Authentication;
using TraktNet.Objects.Get.Syncs.Activities;
using TraktNet.Objects.Get.Users;
using TraktNet.Services;
using TraktPluginMP2.Exceptions;
using TraktPluginMP2.Services;

namespace TraktPluginMP2.Models
{
  public class TraktSetupModel : IWorkflowModel
  {
    const string ApplicationId = "aea41e88de3cd0f8c8b2404d84d2e5d7317789e67fad223eba107aea2ef59068";
    const string SecretId = "adafedb5cd065e6abeb9521b8b64bc66adb010a7c08128811bf32c989f35b77a";

    private static readonly Guid TRAKT_SETUP_MODEL_ID = new Guid("0A24888F-63C0-442A-9DF6-431869BDE803");

    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktClient _traktClient;
    private readonly IFileOperations _fileOperations;
    private readonly ILibrarySynchronization _librarySynchronization;

    private readonly AbstractProperty _isScrobbleEnabledProperty = new WProperty(typeof(bool), false);
    private readonly AbstractProperty _isUserAuthorizedProperty = new WProperty(typeof(bool), false);
    private readonly AbstractProperty _testStatusProperty = new WProperty(typeof(string), string.Empty);
    private readonly AbstractProperty _pinCodeProperty = new WProperty(typeof(string), null);
    private readonly AbstractProperty _isSynchronizingProperty = new WProperty(typeof(bool), false);

    public TraktSetupModel()
    {
      _mediaPortalServices = new MediaPortalServices();
      _traktClient = new TraktClientProxy(ApplicationId, SecretId, _mediaPortalServices.GetLogger());
      _fileOperations = new FileOperations();
      ITraktCache traktCache = new TraktCache(_mediaPortalServices, _traktClient, _fileOperations);
      _librarySynchronization = new LibrarySynchronization(_mediaPortalServices, _traktClient, traktCache,_fileOperations);
    }

    public TraktSetupModel(IMediaPortalServices mediaPortalServices, ITraktClient traktClient, ILibrarySynchronization librarySynchronization, IFileOperations fileOperations)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktClient = traktClient;
      _fileOperations = fileOperations;
      _librarySynchronization = librarySynchronization;
    }

    #region Public properties - Bindable Data

    public AbstractProperty IsScrobbleEnabledProperty
    {
      get { return _isScrobbleEnabledProperty; }
    }

    public bool IsScrobbleEnabled
    {
      get { return (bool)_isScrobbleEnabledProperty.GetValue(); }
      set { _isScrobbleEnabledProperty.SetValue(value); }
    }

    public AbstractProperty IsUserAuthorizedProperty
    {
      get { return _isUserAuthorizedProperty; }
    }

    public bool IsUserAuthorized
    {
      get { return (bool)_isUserAuthorizedProperty.GetValue(); }
      set { _isUserAuthorizedProperty.SetValue(value); }
    }

    public AbstractProperty TestStatusProperty
    {
      get { return _testStatusProperty; }
    }

    public string TestStatus
    {
      get { return (string)_testStatusProperty.GetValue(); }
      set { _testStatusProperty.SetValue(value); }
    }

    public AbstractProperty PinCodeProperty
    {
      get { return _pinCodeProperty; }
    }

    public string PinCode
    {
      get { return (string)_pinCodeProperty.GetValue(); }
      set { _pinCodeProperty.SetValue(value); }
    }

    public AbstractProperty IsSynchronizingProperty
    {
      get { return _isSynchronizingProperty; }
    }

    public bool IsSynchronizing
    {
      get { return (bool)_isSynchronizingProperty.GetValue(); }
      set { _isSynchronizingProperty.SetValue(value); }
    }

    #endregion

    #region Public methods - Commands

    public void AuthorizeUser()
    {
      try
      {
        ITraktAuthorization authorization = _traktClient.GetAuthorization(PinCode);
        ITraktUserSettings traktUserSettings = _traktClient.GetTraktUserSettings();
        ITraktSyncLastActivities traktSyncLastActivities = _traktClient.GetLastActivities();

        string traktUserHomePath = _mediaPortalServices.GetTraktUserHomePath();
        if (!_fileOperations.DirectoryExists(traktUserHomePath))
        {
          _fileOperations.CreateDirectory(traktUserHomePath);
        }

        SaveTraktAuthorization(authorization, traktUserHomePath);
        SaveTraktUserSettings(traktUserSettings, traktUserHomePath);
        SaveLastSyncActivities(traktSyncLastActivities, traktUserHomePath);

        TestStatus = "[Trakt.AuthorizationSucceed]";
        IsUserAuthorized = true;
      }
      catch (Exception ex)
      {
        TestStatus = "[Trakt.AuthorizationFailed]";
        _mediaPortalServices.GetLogger().Error(ex);
        IsUserAuthorized = false;
      }
    }

    public void SyncMediaToTrakt()
    {
      if (!IsSynchronizing)
      {
        try
        {
          IsSynchronizing = true;
          IThreadPool threadPool = _mediaPortalServices.GetThreadPool();
          threadPool.Add(() =>
          {
            if (_mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.SyncOnlyMovies || _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.SyncSeriesAndMovies)
            {
              TestStatus = "[Trakt.SyncMovies]";
              _librarySynchronization.SyncMovies();
            }

            if (_mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.SyncOnlySeries || _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.SyncSeriesAndMovies)
            {
              TestStatus = "[Trakt.SyncSeries]";
              _librarySynchronization.SyncSeries();
            }
            IsSynchronizing = false;
            TestStatus = "[Trakt.SyncFinished]";
          }, ThreadPriority.BelowNormal);
        }
        catch (MediaLibraryNotConnectedException ex)
        {
          TestStatus = "[Trakt.MediaLibraryNotConnected]";
          _mediaPortalServices.GetLogger().Error(ex.Message);
        }
        catch (Exception ex)
        {
          TestStatus = "[Trakt.SyncingFailed]";
          _mediaPortalServices.GetLogger().Error(ex.Message);
        }
      }
    }

    public void BackupLibrary()
    {
      try
      {
        IThreadPool threadPool = _mediaPortalServices.GetThreadPool();
        threadPool.Add(() =>
        {
          TestStatus = "[Trakt.BackupMovies]";
          _librarySynchronization.BackupMovies();
          TestStatus = "[Trakt.BackupSeries]";
          _librarySynchronization.BackupSeries();
          IsSynchronizing = false;
          TestStatus = "[Trakt.BackupFinished]";
        }, ThreadPriority.BelowNormal);
      }
      catch (Exception ex)
      {
        TestStatus = "[Trakt.BackupFailed]";
        _mediaPortalServices.GetLogger().Error(ex.Message);
      }
    }

    #endregion

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // clear the PIN code text box, necessary when entering the plugin
      PinCode = string.Empty;

      string authFilePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.Authorization.Value);
      bool savedAuthFileExists = _fileOperations.FileExists(authFilePath);
      if (!savedAuthFileExists)
      {
        TestStatus = "[Trakt.NotAuthorized]";
        IsUserAuthorized = false;
      }
      else
      {
        string savedAuthorization = _fileOperations.FileReadAllText(authFilePath);
        ITraktAuthorization savedAuthFile = TraktSerializationService.DeserializeAsync<ITraktAuthorization>(savedAuthorization).Result;
        if (savedAuthFile.IsRefreshPossible)
        {
          TestStatus = "[Trakt.AlreadyAuthorized]";
          IsUserAuthorized = true;
        }
        else
        {
          TestStatus = "[Trakt.SavedAuthIsNotValid]";
          IsUserAuthorized = false;
        }
      }
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    public Guid ModelId
    {
      get { return TRAKT_SETUP_MODEL_ID; }
    }

    private void SaveTraktAuthorization(ITraktAuthorization authorization, string path)
    {
      string serializedAuthorization = TraktSerializationService.SerializeAsync(authorization).Result;
      string authorizationFilePath = Path.Combine(path, FileName.Authorization.Value);
      _fileOperations.FileWriteAllText(authorizationFilePath, serializedAuthorization, Encoding.UTF8);
    }

    private void SaveTraktUserSettings(ITraktUserSettings traktUserSettings, string path)
    {
      string serializedSettings = TraktSerializationService.SerializeAsync(traktUserSettings).Result;
      string settingsFilePath = Path.Combine(path, FileName.UserSettings.Value);
      _fileOperations.FileWriteAllText(settingsFilePath, serializedSettings, Encoding.UTF8);
    }

    private void SaveLastSyncActivities(ITraktSyncLastActivities traktSyncLastActivities, string path)
    {
      string serializedSyncActivities = TraktSerializationService.SerializeAsync(traktSyncLastActivities).Result;
      string syncActivitiesFilePath = Path.Combine(path, FileName.LastActivity.Value);
      _fileOperations.FileWriteAllText(syncActivitiesFilePath, serializedSyncActivities, Encoding.UTF8);
    }
  }
}
