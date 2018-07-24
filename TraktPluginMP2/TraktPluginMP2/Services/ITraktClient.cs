using System;
using System.Collections.Generic;
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

namespace TraktPluginMP2.Services
{
  public interface ITraktClient
  {
    ITraktAuthorization TraktAuthorization { get; }

    ITraktAuthorization GetAuthorization(string code);

    ITraktUserSettings GetTraktUserSettings();

    ITraktAuthorization RefreshAuthorization(string refreshToken);

    ITraktSyncHistoryPostResponse AddWatchedHistoryItems(ITraktSyncHistoryPost historyPost);

    ITraktSyncCollectionPostResponse AddCollectionItems(ITraktSyncCollectionPost collectionPost);

    ITraktSyncLastActivities GetLastActivities();

    IEnumerable<ITraktWatchedMovie> GetWatchedMovies();

    IEnumerable<ITraktCollectionMovie> GetCollectedMovies();

    IEnumerable<ITraktWatchedShow> GetWatchedShows();

    IEnumerable<ITraktCollectionShow> GetCollectedShows();

    ITraktMovieScrobblePostResponse StartScrobbleMovie(ITraktMovie movie, float progress, string appVersion = null, DateTime? appBuildDate = null);

    ITraktMovieScrobblePostResponse StopScrobbleMovie(ITraktMovie movie, float progress, string appVersion = null, DateTime? appBuildDate = null);

    ITraktEpisodeScrobblePostResponse StartScrobbleEpisode(ITraktEpisode episode, ITraktShow traktShow, float progress, string appVersion = null, DateTime? appBuildDate = null);

    ITraktEpisodeScrobblePostResponse StopScrobbleEpisode(ITraktEpisode episode, ITraktShow traktShow, float progress, string appVersion = null, DateTime? appBuildDate = null);

    ITraktSyncCollectionRemovePostResponse RemoveCollectionItems(ITraktSyncCollectionPost collectionRemovePost);

    ITraktSyncHistoryRemovePostResponse RemoveWatchedHistoryItems(ITraktSyncHistoryRemovePost historyRemovePost);
  }
}