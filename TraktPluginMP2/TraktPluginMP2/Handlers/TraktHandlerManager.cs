﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using MediaPortal.UI.ServerCommunication;
using Newtonsoft.Json;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Shows;
using TraktApiSharp.Objects.Get.Shows.Episodes;
using TraktApiSharp.Objects.Post.Scrobbles.Responses;
using TraktPluginMP2.Notifications;
using TraktPluginMP2.Services;
using TraktPluginMP2.Utilities;

namespace TraktPluginMP2.Handlers
{
  public class TraktHandlerManager : IDisposable
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktClient _traktClient;
    private readonly IFileOperations _fileOperations;
    private IAsynchronousMessageQueue _messageQueue;
    private TimeSpan _duration;
    private TimeSpan _resumePosition;
    private TraktMovie _traktMovie;
    private TraktEpisode _traktEpisode;
    private TraktShow _traktShow;

    public TraktHandlerManager(IMediaPortalServices mediaPortalServices, ITraktClient traktClient, IFileOperations fileOperations)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktClient = traktClient;
      _fileOperations = fileOperations;
      _mediaPortalServices.GetTraktSettingsWatcher().TraktSettingsChanged += ConfigureHandler;
      _mediaPortalServices.GetUserMessageHandler().UserChangedProxy += ConfigureHandler;
      ConfigureHandler();
    }

    public bool IsActive { get; private set; }

    private void ConfigureHandler(object sender, EventArgs e)
    {
      ConfigureHandler();
    }

    private void ConfigureHandler()
    {
      string authorizationFilePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.Authorization.Value);
      bool isUserAuthorized = _fileOperations.FileExists(authorizationFilePath);
      bool isScrobbleEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.IsScrobbleEnabled;

      if (isUserAuthorized && isScrobbleEnabled)
      {
        SubscribeToMessages();
        IsActive = true;
        _mediaPortalServices.GetLogger().Info("Trakt: enabled trakt handler.");
      }
      else
      {
        UnsubscribeFromMessages();
        IsActive = false;
        _mediaPortalServices.GetLogger().Info("Trakt: disabled trakt handler.");
      }
    }

    private void SubscribeToMessages()
    {
      if (_messageQueue == null)
      {
        _messageQueue = _mediaPortalServices.GetMessageQueue(this, new string[]
        {
          PlayerManagerMessaging.CHANNEL
        });
        _messageQueue.MessageReceivedProxy += OnMessageReceived;
        _messageQueue.StartProxy();
      }
    }


    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerStarted:
            StartScrobble(message);
            break;
          case PlayerManagerMessaging.MessageType.PlayerResumeState:
            SaveResumePosition(message);
            break;
          case PlayerManagerMessaging.MessageType.PlayerError:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
          case PlayerManagerMessaging.MessageType.PlayerStopped:
            StopScrobble();
            break;
        }
      }
    }

    private void StartScrobble(SystemMessage message)
    {
      try
      {
        IPlayerSlotController psc = (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
        IPlayerContext pc = _mediaPortalServices.GetPlayerContext(psc);
        if (pc?.CurrentMediaItem == null)
        {
          throw new ArgumentNullException(nameof(pc.CurrentMediaItem));
        }

        IMediaPlaybackControl pmc = pc.CurrentPlayer as IMediaPlaybackControl;
        if (pmc == null)
        {
          throw new ArgumentNullException(nameof(pmc));
        }

        if (IsSeries(pc.CurrentMediaItem))
        {
          HandleEpisodeScrobbleStart(pc, pmc);
        }
        else if (IsMovie(pc.CurrentMediaItem))
        {
          HandleMovieScrobbleStart(pc, pmc);
        }
      }
      catch (ArgumentNullException ex)
      {
        _mediaPortalServices.GetLogger().Error("Trakt: exception while starting scrobble: " + ex);
      }
      catch (Exception ex)
      {
        bool startNotificationsEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowScrobbleStartedNotifications;
        bool startNotificationsOnFailureEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowScrobbleStartedNotificationsOnFailure;
        if (startNotificationsEnabled || startNotificationsOnFailureEnabled)
        {
          ShowNotification(new TraktScrobbleStartedNotification(ex.Message, false), TimeSpan.FromSeconds(5));
        }
        _mediaPortalServices.GetLogger().Error("Trakt: exception while starting scrobble: " + ex);
        _traktEpisode = null;
        _traktMovie = null;
        _duration = TimeSpan.Zero;
      }
    }

    private bool IsSeries(MediaItem item)
    {
      return item.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID);
    }

    private void HandleEpisodeScrobbleStart(IPlayerContext pc, IMediaPlaybackControl pmc)
    {
      ValidateAuthorization();

      MediaItem episodeMediaItem = GetMediaItem(pc.CurrentMediaItem.MediaItemId, new Guid[] { MediaAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, EpisodeAspect.ASPECT_ID });
      _traktEpisode = ExtractTraktEpisode(episodeMediaItem);
      _traktShow = ExtractTraktShow(episodeMediaItem);
      float progress = GetCurrentProgress(pmc);
      TraktEpisodeScrobblePostResponse postEpisodeResponse = _traktClient.StartScrobbleEpisode(_traktEpisode, _traktShow, progress);
      string title = postEpisodeResponse.Episode.Title;

      bool startNotificationsEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowScrobbleStartedNotifications;
      if (startNotificationsEnabled)
      {
        ShowNotification(new TraktScrobbleStartedNotification(title, true), TimeSpan.FromSeconds(5));
      }

      _duration = pmc.Duration;
      _mediaPortalServices.GetLogger().Info("Trakt: started to scrobble: {0}", title);
    }

    private void ValidateAuthorization()
    {
      if (!_traktClient.TraktAuthorization.IsValid)
      {
        string authFilePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.Authorization.Value);
        string savedAuthorization = _fileOperations.FileReadAllText(authFilePath);
        TraktAuthorization savedAuth = JsonConvert.DeserializeObject<TraktAuthorization>(savedAuthorization);

        if (!savedAuth.IsRefreshPossible)
        {
          throw new Exception("Saved authorization is not valid.");
        }

        TraktAuthorization refreshedAuth = _traktClient.RefreshAuthorization(savedAuth.RefreshToken);
        string serializedAuth = JsonConvert.SerializeObject(refreshedAuth);
        _fileOperations.FileWriteAllText(authFilePath, serializedAuth, Encoding.UTF8);
      }
    }

    private void ShowNotification(ITraktNotification notification, TimeSpan duration)
    {
      _mediaPortalServices.GetTraktNotificationModel().ShowNotification(notification, duration);
    }

    private MediaItem GetMediaItem(Guid filter, Guid[] aspects)
    {
      IServerConnectionManager scm = _mediaPortalServices.GetServerConnectionManager();
      IContentDirectory cd = scm.ContentDirectory;

      return cd?.SearchAsync(new MediaItemQuery(aspects, new Guid[] { }, new MediaItemIdFilter(filter)), false, null, true).Result.First();
    }

    private TraktEpisode ExtractTraktEpisode(MediaItem episodeMediaItem)
    {
      TraktEpisode episode = new TraktEpisode
      {
        Number = MediaItemAspectsUtl.GetEpisodeIndex(episodeMediaItem),
        SeasonNumber = MediaItemAspectsUtl.GetSeasonIndex(episodeMediaItem)
      };
      return episode;
    }

    private TraktShow ExtractTraktShow(MediaItem episodeMediaItem)
    {
      TraktShow show = new TraktShow
      {
        Ids = new TraktShowIds
        {
          Imdb = MediaItemAspectsUtl.GetSeriesImdbId(episodeMediaItem),
          Tvdb = MediaItemAspectsUtl.GetTvdbId(episodeMediaItem)
        },
        Title = MediaItemAspectsUtl.GetSeriesTitle(episodeMediaItem),
      };
      return show;
    }

    private float GetCurrentProgress(IMediaPlaybackControl pmc)
    {
      return (float)(100 * pmc.CurrentTime.TotalMilliseconds / pmc.Duration.TotalMilliseconds);
    }

    private bool IsMovie(MediaItem item)
    {
      return item.Aspects.ContainsKey(MovieAspect.ASPECT_ID);
    }

    private void HandleMovieScrobbleStart(IPlayerContext pc, IMediaPlaybackControl pmc)
    {
      ValidateAuthorization();

      MediaItem movieMediaItem = GetMediaItem(pc.CurrentMediaItem.MediaItemId, new Guid[] { MediaAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MovieAspect.ASPECT_ID });
      _traktMovie = ConvertMediaItemToTraktMovie(movieMediaItem);
      float progress = GetCurrentProgress(pmc);
      TraktMovieScrobblePostResponse postMovieResponse = _traktClient.StartScrobbleMovie(_traktMovie, progress);
      string title = postMovieResponse.Movie.Title;

      bool startNotificationsEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowScrobbleStartedNotifications;
      if (startNotificationsEnabled)
      {
        ShowNotification(new TraktScrobbleStartedNotification(title, true), TimeSpan.FromSeconds(5));
      }

      _duration = pmc.Duration;
      _mediaPortalServices.GetLogger().Info("Trakt: started to scrobble: {0}", title);
    }

    private TraktMovie ConvertMediaItemToTraktMovie(MediaItem movieMediaItem)
    {
      TraktMovie movie = new TraktMovie
      {
        Ids = new TraktMovieIds
        {
          Imdb = MediaItemAspectsUtl.GetMovieImdbId(movieMediaItem),
          Tmdb = MediaItemAspectsUtl.GetMovieTmdbId(movieMediaItem)
        },
        Title = MediaItemAspectsUtl.GetMovieTitle(movieMediaItem),
        Year = MediaItemAspectsUtl.GetMovieYear(movieMediaItem)
      };
      return movie;
    }

    private void SaveResumePosition(SystemMessage message)
    {
      IResumeState resumeState = (IResumeState)message.MessageData[PlayerManagerMessaging.KEY_RESUME_STATE];
      PositionResumeState positionResume = resumeState as PositionResumeState;
      if (positionResume != null)
      {
        _resumePosition = positionResume.ResumePosition;
      }
    }

    private void StopScrobble()
    {
      try
      {
        if (_traktEpisode != null && _traktShow != null)
        {
          ValidateAuthorization();

          float progress = GetSavedProgress();
          TraktEpisodeScrobblePostResponse postEpisodeResponse = _traktClient.StopScrobbleEpisode(_traktEpisode, _traktShow, progress);
          string title = postEpisodeResponse.Episode.Title;

          bool stopNotificationsEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowScrobbleStoppedNotifications;
          if (stopNotificationsEnabled)
          {
            ShowNotification(new TraktScrobbleStoppedNotification(title, true), TimeSpan.FromSeconds(5));
          }

          _traktEpisode = null;
          _traktShow = null;
          _mediaPortalServices.GetLogger().Info("Trakt: stopped to scrobble: {0}", title);
        }
        else if (_traktMovie != null)
        {
          ValidateAuthorization();

          float progress = GetSavedProgress();
          TraktMovieScrobblePostResponse postMovieResponse = _traktClient.StopScrobbleMovie(_traktMovie, progress);
          string title = postMovieResponse.Movie.Title;

          bool stopNotificationsEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowScrobbleStoppedNotifications;
          if (stopNotificationsEnabled)
          {
            ShowNotification(new TraktScrobbleStoppedNotification(title, true), TimeSpan.FromSeconds(5));
          }
          
          _traktMovie = null;
          _mediaPortalServices.GetLogger().Info("Trakt: stopped scrobble: {0}", title);
        }
      }
      catch (Exception ex)
      {
        bool stopNotificationsEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowScrobbleStoppedNotifications;
        bool stopNotificationsOnFailureEnabled = _mediaPortalServices.GetTraktSettingsWatcher().TraktSettings.ShowScrobbleStoppedNotificationsOnFailure;
        if (stopNotificationsEnabled || stopNotificationsOnFailureEnabled)
        {
          ShowNotification(new TraktScrobbleStoppedNotification(ex.Message, false), TimeSpan.FromSeconds(4));
        }
        _mediaPortalServices.GetLogger().Error("Trakt: exception while stopping scrobble: " + ex);
      }
    }

    private float GetSavedProgress()
    {
      return Math.Min((float)(_resumePosition.TotalSeconds* 100 / _duration.TotalSeconds), 100);
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