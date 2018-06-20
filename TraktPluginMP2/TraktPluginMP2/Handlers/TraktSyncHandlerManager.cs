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
            TraktSyncMoviesResult syncMoviesResult = _librarySynchronization.SyncMovies();
            TraktSyncEpisodesResult syncEpisodesResult = _librarySynchronization.SyncSeries();
          }
          catch (Exception ex)
          {
            _mediaPortalServices.GetLogger().Error(ex.Message);
          }
        }
      }
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