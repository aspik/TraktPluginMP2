using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;
using TraktNet;
using TraktNet.Exceptions;
using TraktNet.Objects.Authentication;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Episodes;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Shows;
using TraktNet.Objects.Get.Syncs.Activities;
using TraktNet.Objects.Get.Users;
using TraktNet.Objects.Get.Watched;
using TraktNet.Objects.Post.Scrobbles.Responses;
using TraktNet.Objects.Post.Syncs.Collection;
using TraktNet.Objects.Post.Syncs.Collection.Responses;
using TraktNet.Objects.Post.Syncs.History;
using TraktNet.Objects.Post.Syncs.History.Responses;
using TraktNet.Responses;
using TraktNet.Responses.Interfaces;

namespace TraktPluginMP2.Services
{
  public class TraktClientProxy : TraktClient, ITraktClient
  {
    private readonly ILogger _logger;

    public TraktClientProxy(string clientId, string clientSecret, ILogger logger) : base(clientId, clientSecret)
    {
      _logger = logger;
    }

    public ITraktAuthorization TraktAuthorization
    {
      get { return base.Authorization; }
    }

    public ITraktAuthorization GetAuthorization(string code)
    {
      ITraktResponse<ITraktAuthorization> response = new TraktResponse<ITraktAuthorization>();
      try
      {
        response = Task.Run(() => base.Authentication.GetAuthorizationAsync(code)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }
      return response.Value;
    }

    public ITraktAuthorization RefreshAuthorization(string refreshToken)
    {
      ITraktResponse<ITraktAuthorization> response = new TraktResponse<ITraktAuthorization>();
      try
      {
        response = Task.Run(() => base.Authentication.RefreshAuthorizationAsync(refreshToken)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public ITraktSyncHistoryPostResponse AddWatchedHistoryItems(ITraktSyncHistoryPost historyPost)
    {
      ITraktResponse<ITraktSyncHistoryPostResponse> response = new TraktResponse<ITraktSyncHistoryPostResponse>();
      try
      {
        response = Task.Run(() => base.Sync.AddWatchedHistoryItemsAsync(historyPost)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public ITraktSyncCollectionPostResponse AddCollectionItems(ITraktSyncCollectionPost collectionPost)
    {
      ITraktResponse<ITraktSyncCollectionPostResponse> response = new TraktResponse<ITraktSyncCollectionPostResponse>();
      try
      {
        response = Task.Run(() => base.Sync.AddCollectionItemsAsync(collectionPost)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public ITraktSyncLastActivities GetLastActivities()
    {
      ITraktResponse<ITraktSyncLastActivities> response = new TraktResponse<ITraktSyncLastActivities>();
      try
      {
        response = Task.Run(() => base.Sync.GetLastActivitiesAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public IEnumerable<ITraktWatchedMovie> GetWatchedMovies()
    {
      ITraktListResponse<ITraktWatchedMovie> response = new TraktListResponse<ITraktWatchedMovie>();
      try
      {
        response = Task.Run(() => base.Sync.GetWatchedMoviesAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public IEnumerable<ITraktCollectionMovie> GetCollectedMovies()
    {
      ITraktListResponse<ITraktCollectionMovie> response = new TraktListResponse<ITraktCollectionMovie>();
      try
      {
        response = Task.Run(() => base.Sync.GetCollectionMoviesAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public IEnumerable<ITraktWatchedShow> GetWatchedShows()
    {
      ITraktListResponse<ITraktWatchedShow> response = new TraktListResponse<ITraktWatchedShow>();
      try
      {
        response = Task.Run(() => base.Sync.GetWatchedShowsAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public IEnumerable<ITraktCollectionShow> GetCollectedShows()
    {
      ITraktListResponse<ITraktCollectionShow> response = new TraktListResponse<ITraktCollectionShow>();
      try
      {
        response = Task.Run(() => base.Sync.GetCollectionShowsAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public ITraktMovieScrobblePostResponse StartScrobbleMovie(ITraktMovie movie, float progress, string appVersion = null,
      DateTime? appBuildDate = null)
    {
      ITraktResponse<ITraktMovieScrobblePostResponse> response = new TraktResponse<ITraktMovieScrobblePostResponse>();
      try
      {
        response = Task.Run(() => base.Scrobble.StartMovieAsync(movie, progress, appVersion, appBuildDate)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public ITraktMovieScrobblePostResponse StopScrobbleMovie(ITraktMovie movie, float progress, string appVersion = null,
      DateTime? appBuildDate = null)
    {
      ITraktResponse<ITraktMovieScrobblePostResponse> response = new TraktResponse<ITraktMovieScrobblePostResponse>();
      try
      {
        response = Task.Run(() => base.Scrobble.StopMovieAsync(movie, progress, appVersion, appBuildDate)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public ITraktEpisodeScrobblePostResponse StartScrobbleEpisode(ITraktEpisode episode, ITraktShow traktShow, float progress, string appVersion = null,
      DateTime? appBuildDate = null)
    {
      ITraktResponse<ITraktEpisodeScrobblePostResponse> response = new TraktResponse<ITraktEpisodeScrobblePostResponse>();
      try
      {
        response = Task.Run(() => base.Scrobble.StartEpisodeWithShowAsync(episode, traktShow, progress, appVersion, appBuildDate)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public ITraktEpisodeScrobblePostResponse StopScrobbleEpisode(ITraktEpisode episode, ITraktShow traktShow, float progress, string appVersion = null,
      DateTime? appBuildDate = null)
    {
      ITraktResponse<ITraktEpisodeScrobblePostResponse> response = new TraktResponse<ITraktEpisodeScrobblePostResponse>();
      try
      {
        response = Task.Run(() => base.Scrobble.StopEpisodeWithShowAsync(episode, traktShow, progress, appVersion, appBuildDate)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public ITraktUserSettings GetTraktUserSettings()
    {
      ITraktResponse<ITraktUserSettings> response = new TraktResponse<ITraktUserSettings>();
      try
      {
        response = Task.Run(() => base.Users.GetSettingsAsync()).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }

      return response.Value;
    }

    public ITraktSyncCollectionRemovePostResponse RemoveCollectionItems(ITraktSyncCollectionPost collectionRemovePost)
    {
      ITraktResponse<ITraktSyncCollectionRemovePostResponse> response = new TraktResponse<ITraktSyncCollectionRemovePostResponse>();
      try
      {
        response = Task.Run(() => base.Sync.RemoveCollectionItemsAsync(collectionRemovePost)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }
      return response.Value;
    }

    public ITraktSyncHistoryRemovePostResponse RemoveWatchedHistoryItems(ITraktSyncHistoryRemovePost historyRemovePost)
    {
      ITraktResponse<ITraktSyncHistoryRemovePostResponse> response = new TraktResponse<ITraktSyncHistoryRemovePostResponse>();
      try
      {
        response = Task.Run(() => base.Sync.RemoveWatchedHistoryItemsAsync(historyRemovePost)).Result;
      }
      catch (AggregateException aggregateException)
      {
        UnwrapAggregateException(aggregateException);
      }
      return response.Value;
    }


    private void UnwrapAggregateException(AggregateException aggregateException)
    {
      aggregateException.Handle((x) =>
      {
        TraktException ex = x as TraktException;
        if (ex != null)
        {
          _logger.Error("TraktApiSharp exception occurred: RequestBody: {0}, RequestUrl: {1}, Response: {2}, ServerReasonPhrase: {3}, StatusCode: {4} ",
            ex.RequestBody, ex.RequestUrl, ex.Response, ex.ServerReasonPhrase, ex.StatusCode);
          throw ex;
        }
        throw new TraktException("Unknown error in TraktApiSharp.");
      });
    }
  }
}