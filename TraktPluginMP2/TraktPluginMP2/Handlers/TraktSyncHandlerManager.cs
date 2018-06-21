using System;
using System.IO;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using TraktPluginMP2.Notifications;
using TraktPluginMP2.Services;

namespace TraktPluginMP2.Handlers
{
  public class TraktSyncHandlerManager : IDisposable
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly IFileOperations _fileOperations;
    private IAsynchronousMessageQueue _messageQueue;
    private readonly ILibrarySynchronization _librarySynchronization;

    public TraktSyncHandlerManager(IMediaPortalServices mediaPortalServices, ILibrarySynchronization librarySynchronization, IFileOperations fileOperations)
    {
      _mediaPortalServices = mediaPortalServices;
      _fileOperations = fileOperations;
      _librarySynchronization = librarySynchronization;
      _mediaPortalServices.GetTraktSettingsWatcher().TraktSettingsChanged += ConfigureHandler;
      _mediaPortalServices.GetUserMessageHandler().UserChangedProxy += ConfigureHandler;
      ConfigureHandler();
    }

    private void ConfigureHandler(object sender, EventArgs e)
    {
      ConfigureHandler();
    }

    private void ConfigureHandler()
    {
      string authorizationFilePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.Authorization.Value);
      bool isUserAuthorized = _fileOperations.FileExists(authorizationFilePath);
      bool isAutomaticSyncEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.IsAutomaticLibrarySyncEnabled;

      if (isUserAuthorized && isAutomaticSyncEnabled)
      {
        SubscribeToMessages();
        _mediaPortalServices.GetLogger().Info("Trakt: enabled trakt sync handler.");
      }
      else
      {
        UnsubscribeFromMessages();
        _mediaPortalServices.GetLogger().Info("Trakt: disabled trakt sync handler.");
      }
    }

    private void SubscribeToMessages()
    {
      if (_messageQueue == null)
      {
        _messageQueue = _mediaPortalServices.GetMessageQueue(this, new string[]
        {
          ContentDirectoryMessaging.CHANNEL
        });
        _messageQueue.MessageReceivedProxy += OnMessageReceived;
        _messageQueue.StartProxy();
      }
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ContentDirectoryMessaging.CHANNEL)
      {
        ContentDirectoryMessaging.MessageType messageType = (ContentDirectoryMessaging.MessageType) message.MessageType;
        if (messageType == ContentDirectoryMessaging.MessageType.ShareImportCompleted)
        {
          try
          {
            SyncLibraryWithTrakt();

            bool syncNotificationsEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowAutomaticSyncNotifications;
            if (syncNotificationsEnabled)
            {
              ShowNotification(new TraktSyncLibraryFinishedNotification("Success message", true), TimeSpan.FromSeconds(5));
            }
          }
          catch (Exception ex)
          {
            _mediaPortalServices.GetLogger().Error(ex.Message);

            bool syncNotificationsEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowAutomaticSyncNotifications;
            bool syncNotificationsOnFailureEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowAutomaticSyncNotificationsOnFailure;
            if (syncNotificationsEnabled || syncNotificationsOnFailureEnabled)
            {
              ShowNotification(new TraktSyncLibraryFinishedNotification(ex.Message, false), TimeSpan.FromSeconds(5));
            }
          }
        }
      }
    }

    private void SyncLibraryWithTrakt()
    {
      TraktSyncMoviesResult syncMoviesResult = _librarySynchronization.SyncMovies();
      _mediaPortalServices.GetLogger().Info("Trakt: Finished automatic movies sync.");

      _mediaPortalServices.GetLogger().Info("There are '{0}' watched movies and '{1}' collected movies in library.", 
        syncMoviesResult.WatchedInLibrary, syncMoviesResult.CollectedInLibrary);

      _mediaPortalServices.GetLogger().Info("There were '{0}' movies added to watched history and '{1}' movies added to collection at trakt.",
        syncMoviesResult.AddedToTraktWatchedHistory.HasValue ? syncMoviesResult.AddedToTraktWatchedHistory.ToString() : "<empty>", 
        syncMoviesResult.AddedToTraktCollection.HasValue ? syncMoviesResult.AddedToTraktCollection.ToString() : "<empty>");

      _mediaPortalServices.GetLogger().Info("There were '{0}' movies marked as watched and '{1}' movies marked as unwatched in library.",
        syncMoviesResult.MarkedAsWatchedInLibrary, syncMoviesResult.MarkedAsUnWatchedInLibrary);

      TraktSyncEpisodesResult syncEpisodesResult = _librarySynchronization.SyncSeries();
      _mediaPortalServices.GetLogger().Info("Trakt: Finished automatic series sync.");

      _mediaPortalServices.GetLogger().Info("There are '{0}' watched episodes and '{1}' collected episodes in library.",
        syncEpisodesResult.WatchedInLibrary, syncEpisodesResult.CollectedInLibrary);

      _mediaPortalServices.GetLogger().Info("There were '{0}' episodes added to watched history and '{1}' episodes added to collection at trakt.",
        syncEpisodesResult.AddedToTraktWatchedHistory.HasValue ? syncEpisodesResult.AddedToTraktWatchedHistory.ToString() : "<empty>",
        syncEpisodesResult.AddedToTraktCollection.HasValue ? syncEpisodesResult.AddedToTraktCollection.ToString() : "<empty>");

      _mediaPortalServices.GetLogger().Info("There were '{0}' episodes marked as watched and '{1}' episodes marked as unwatched in library.",
        syncEpisodesResult.MarkedAsWatchedInLibrary, syncEpisodesResult.MarkedAsUnWatchedInLibrary);
    }

    private void ShowNotification(ITraktNotification notification, TimeSpan duration)
    {
      _mediaPortalServices.GetTraktNotificationModel().ShowNotification(notification, duration);
    }

    private void UnsubscribeFromMessages()
    {
      if (_messageQueue != null)
      {
        _messageQueue.ShutdownProxy();
        _messageQueue = null;
      }
    }

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }
  }
}