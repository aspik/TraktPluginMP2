using System;
using System.IO;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TraktPluginMP2;
using TraktPluginMP2.Exceptions;
using TraktPluginMP2.Handlers;
using TraktPluginMP2.Notifications;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;
using Xunit;

namespace Tests
{
  public class TraktSyncHandlerTests
  {
    private const string DataPath = @"C:\FakeTraktUserHomePath\";

    [Fact]
    public void ShouldEnableTraktSyncHandlerWhenSettingsChange()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      TraktPluginSettings settings = new TraktPluginSettings
      {
        IsAutomaticLibrarySyncEnabled = false
      };
      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);

      ILibrarySynchronization librarySynchronization = Substitute.For<ILibrarySynchronization>();
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileExists(Path.Combine(DataPath, FileName.Authorization.Value)).Returns(true);

      TraktSyncHandlerManager traktSyncHandler = new TraktSyncHandlerManager(mediaPortalServices, librarySynchronization, fileOperations);

      // Act
      settings.IsAutomaticLibrarySyncEnabled = true;
      settingsChangeWatcher.TraktSettingsChanged += Raise.Event();

      // Assert
      Assert.True(traktSyncHandler.IsActive);
    }

    [Fact]
    public void ShouldEnableTraktSyncHandlerWhenUserChanged()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      TraktPluginSettings settings = new TraktPluginSettings
      {
        IsAutomaticLibrarySyncEnabled = false
      };
      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      IUserMessageHandler userMessageHandler = Substitute.For<IUserMessageHandler>();
      mediaPortalServices.GetUserMessageHandler().Returns(userMessageHandler);

      ILibrarySynchronization librarySynchronization = Substitute.For<ILibrarySynchronization>();
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileExists(Path.Combine(DataPath, FileName.Authorization.Value)).Returns(true);

      TraktSyncHandlerManager traktScrobbleHandler = new TraktSyncHandlerManager(mediaPortalServices, librarySynchronization, fileOperations);

      // Act
      settings.IsAutomaticLibrarySyncEnabled = true;
      userMessageHandler.UserChangedProxy += Raise.Event();

      // Assert
      Assert.True(traktScrobbleHandler.IsActive);
    }

    [Fact]
    public void ShouldSyncLibraryWhenShareImportCompletedIsWithSuccess()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      SetSettings(mediaPortalServices, new TraktPluginSettings {IsAutomaticLibrarySyncEnabled = true, ShowAutomaticSyncNotifications = true});

      IAsynchronousMessageQueue messageQueue = GetMockedMsgQueue(mediaPortalServices);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileExists(Path.Combine(DataPath, FileName.Authorization.Value)).Returns(true);

      ILibrarySynchronization librarySynchronization = Substitute.For<ILibrarySynchronization>();
      librarySynchronization.SyncMovies().Returns(new TraktSyncMoviesResult());
      librarySynchronization.SyncSeries().Returns(new TraktSyncEpisodesResult());

      TraktSyncHandlerManager traktScrobbleHandler = new TraktSyncHandlerManager(mediaPortalServices, librarySynchronization, fileOperations);

      // Act
      // send share import completed message
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "ContentDirectory" }),
        GetSystemMessageForMessageType(ContentDirectoryMessaging.MessageType.ShareImportCompleted));

      // Assert
      mediaPortalServices.GetTraktNotificationModel().Received().ShowNotification(Arg.Any<TraktSyncLibrarySuccessNotification>(), Arg.Any<TimeSpan>());
    }

    [Fact]
    public void ShouldFailSyncingLibraryWhenMediaLibraryNotConnected()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      SetSettings(mediaPortalServices, new TraktPluginSettings { IsAutomaticLibrarySyncEnabled = true, ShowAutomaticSyncNotifications = true });

      IAsynchronousMessageQueue messageQueue = GetMockedMsgQueue(mediaPortalServices);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.FileExists(Path.Combine(DataPath, FileName.Authorization.Value)).Returns(true);

      ILibrarySynchronization librarySynchronization = Substitute.For<ILibrarySynchronization>();
      librarySynchronization.SyncMovies().Throws(new MediaLibraryNotConnectedException("ML not connected"));

      TraktSyncHandlerManager traktScrobbleHandler = new TraktSyncHandlerManager(mediaPortalServices, librarySynchronization, fileOperations);

      // Act
      // send share import completed message
      messageQueue.MessageReceivedProxy += Raise.Event<MessageReceivedHandler>(new AsynchronousMessageQueue(new object(), new[] { "ContentDirectory" }),
        GetSystemMessageForMessageType(ContentDirectoryMessaging.MessageType.ShareImportCompleted));

      // Assert
      mediaPortalServices.GetTraktNotificationModel().Received().ShowNotification(Arg.Any<TraktSyncLibraryFailureNotification>(), Arg.Any<TimeSpan>());
    }

    private void SetSettings(IMediaPortalServices mediaPortalServices, TraktPluginSettings settings)
    {
      ITraktSettingsChangeWatcher settingsChangeWatcher = Substitute.For<ITraktSettingsChangeWatcher>();
      settingsChangeWatcher.TraktSettings.Returns(settings);
      mediaPortalServices.GetTraktSettingsWatcher().Returns(settingsChangeWatcher);

      ISettingsManager settingsManager = Substitute.For<ISettingsManager>();
      settingsManager.Load<TraktPluginSettings>().Returns(settings);
      mediaPortalServices.GetSettingsManager().Returns(settingsManager);
    }

    private IAsynchronousMessageQueue GetMockedMsgQueue(IMediaPortalServices mediaPortalServices)
    {
      IAsynchronousMessageQueue messageQueue = Substitute.For<IAsynchronousMessageQueue>();
      messageQueue.When(x => x.StartProxy()).Do(x => { /*nothing*/});
      mediaPortalServices.GetMessageQueue(Arg.Any<object>(), Arg.Any<string[]>()).Returns(messageQueue);
      return messageQueue;
    }

    private SystemMessage GetSystemMessageForMessageType(ContentDirectoryMessaging.MessageType msgType)
    {
      SystemMessage state = new SystemMessage(msgType)
      {
        ChannelName = "ContentDirectory"
      };
      return state;
    }
  }
}