using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UserManagement;
using Newtonsoft.Json;
using TraktNet.Objects.Authentication;
using TraktNet.Objects.Basic;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Shows;
using TraktNet.Objects.Get.Syncs.Activities;
using TraktNet.Objects.Get.Watched;
using TraktNet.Objects.Post;
using TraktNet.Objects.Post.Syncs.Collection;
using TraktNet.Objects.Post.Syncs.Collection.Responses;
using TraktNet.Objects.Post.Syncs.History;
using TraktNet.Objects.Post.Syncs.History.Responses;
using TraktNet.Services;
using TraktPluginMP2.Exceptions;
using TraktPluginMP2.Structures;
using TraktPluginMP2.Utilities;

namespace TraktPluginMP2.Services
{
  public class LibrarySynchronization : ILibrarySynchronization
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktClient _traktClient;
    private readonly ITraktCache _traktCache;
    private readonly IFileOperations _fileOperations;

    public LibrarySynchronization(IMediaPortalServices mediaPortalServices, ITraktClient traktClient, ITraktCache traktCache, IFileOperations fileOperations)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktClient = traktClient;
      _traktCache = traktCache;
      _fileOperations = fileOperations;
    }

    public TraktSyncMoviesResult SyncMovies()
    {
      _mediaPortalServices.GetLogger().Info("Trakt: start sync movies");

      ValidateAuthorization();

      _traktCache.RefreshMoviesCache();

      TraktSyncMoviesResult syncMoviesResult = new TraktSyncMoviesResult();
      IList<ITraktMovie> traktUnWatchedMovies = _traktCache.UnWatchedMovies.ToList();
      IList<ITraktWatchedMovie> traktWatchedMovies = _traktCache.WatchedMovies.ToList();
      IList<ITraktCollectionMovie> traktCollectedMovies = _traktCache.CollectedMovies.ToList();

      Guid[] types =
      {
        MediaAspect.ASPECT_ID, MovieAspect.ASPECT_ID, VideoAspect.ASPECT_ID, ImporterAspect.ASPECT_ID,
        ExternalIdentifierAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID, VideoStreamAspect.ASPECT_ID,
        VideoAudioStreamAspect.ASPECT_ID
      };

      IContentDirectory contentDirectory = _mediaPortalServices.GetServerConnectionManager().ContentDirectory;
      if (contentDirectory == null)
      {
        throw new MediaLibraryNotConnectedException("ML not connected");
      }

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = _mediaPortalServices.GetUserManagement();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }

      #region Get local database info

      IList<MediaItem> collectedMovies = contentDirectory.SearchAsync(new MediaItemQuery(types, null, null), true, userProfile, false).Result;

      if (collectedMovies.Any())
      {
        syncMoviesResult.CollectedInLibrary = collectedMovies.Count;
        _mediaPortalServices.GetLogger().Info("Trakt: found {0} collected movies available to sync in media library", collectedMovies.Count);
      }

      List<MediaItem> watchedMovies = collectedMovies.Where(MediaItemAspectsUtl.IsWatched).ToList();

      if (watchedMovies.Any())
      {
        syncMoviesResult.WatchedInLibrary = watchedMovies.Count;
        _mediaPortalServices.GetLogger().Info("Trakt: found {0} watched movies available to sync in media library", watchedMovies.Count);
      }

      #endregion

      #region Mark movies as unwatched in local database

      _mediaPortalServices.GetLogger().Info("Trakt: start marking movies as unwatched in media library");
      if (traktUnWatchedMovies.Any())
      {
        foreach (var movie in traktUnWatchedMovies)
        {
          var localMovie = watchedMovies.FirstOrDefault(m => MovieMatch(m, movie));
          if (localMovie == null)
          {
            continue;
          }

          _mediaPortalServices.GetLogger().Info(
            "Marking movie as unwatched in library, movie is not watched on trakt. Title = '{0}', Year = '{1}', IMDb ID = '{2}', TMDb ID = '{3}'",
            movie.Title, movie.Year.HasValue ? movie.Year.ToString() : "<empty>", movie.Ids.Imdb ?? "<empty>",
            movie.Ids.Tmdb.HasValue ? movie.Ids.Tmdb.ToString() : "<empty>");

          if (_mediaPortalServices.MarkAsUnWatched(localMovie).Result)
          {
            syncMoviesResult.MarkedAsUnWatchedInLibrary++;
          }
        }

        // update watched set
        watchedMovies = collectedMovies.Where(MediaItemAspectsUtl.IsWatched).ToList();
      }

      #endregion

      #region Mark movies as watched in local database

      _mediaPortalServices.GetLogger().Info("Trakt: start marking movies as watched in media library");
      if (traktWatchedMovies.Any())
      {
        foreach (var twm in traktWatchedMovies)
        {
          var localMovie = collectedMovies.FirstOrDefault(m => MovieMatch(m, twm.Movie));
          if (localMovie == null)
          {
            continue;
          }

          _mediaPortalServices.GetLogger().Info(
            "Marking movie as watched in library, movie is watched on trakt. Plays = '{0}', Title = '{1}', Year = '{2}', IMDb ID = '{3}', TMDb ID = '{4}'",
            twm.Plays, twm.Movie.Title, twm.Movie.Year.HasValue ? twm.Movie.Year.ToString() : "<empty>",
            twm.Movie.Ids.Imdb ?? "<empty>", twm.Movie.Ids.Tmdb.HasValue ? twm.Movie.Ids.Tmdb.ToString() : "<empty>");

          if (_mediaPortalServices.MarkAsWatched(localMovie).Result)
          {
            syncMoviesResult.MarkedAsWatchedInLibrary++;
          }
        }
      }

      #endregion

      #region Add movies to watched history at trakt.tv

      _mediaPortalServices.GetLogger().Info("Trakt: finding movies to add to watched history");

      List<TraktSyncHistoryPostMovie> syncWatchedMovies = (from movie in watchedMovies
                                                           where !traktWatchedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
                                                           select new TraktSyncHistoryPostMovie
                                                           {
                                                             Ids = new TraktMovieIds
                                                             {
                                                               Imdb = MediaItemAspectsUtl.GetMovieImdbId(movie),
                                                               Tmdb = MediaItemAspectsUtl.GetMovieTmdbId(movie)
                                                             },
                                                             Title = MediaItemAspectsUtl.GetMovieTitle(movie),
                                                             Year = MediaItemAspectsUtl.GetMovieYear(movie),
                                                             WatchedAt = MediaItemAspectsUtl.GetLastPlayedDate(movie),
                                                           }).ToList();

      if (syncWatchedMovies.Any())
      {
        _mediaPortalServices.GetLogger().Info("Trakt: trying to add {0} watched movies to trakt watched history", syncWatchedMovies.Count);

        ITraktSyncHistoryPostResponse watchedResponse = _traktClient.AddWatchedHistoryItems(new TraktSyncHistoryPost { Movies = syncWatchedMovies });
        syncMoviesResult.AddedToTraktWatchedHistory = watchedResponse.Added?.Movies;

        if (watchedResponse.Added?.Movies != null)
        {
          _mediaPortalServices.GetLogger().Info("Trakt: successfully added {0} watched movies to trakt watched history", watchedResponse.Added.Movies.Value);
        }
      }

      #endregion

      #region Add movies to collection at trakt.tv

      _mediaPortalServices.GetLogger().Info("Trakt: finding movies to add to collection");

      List<TraktSyncCollectionPostMovie> syncCollectedMovies = (from movie in collectedMovies
                                                                where !traktCollectedMovies.ToList().Exists(c => MovieMatch(movie, c.Movie))
                                                                select new TraktSyncCollectionPostMovie
                                                                {
                                         
                                                                    MediaType = MediaItemAspectsUtl.GetVideoMediaType(movie),
                                                                    MediaResolution = MediaItemAspectsUtl.GetVideoResolution(movie),
                                                                    Audio = MediaItemAspectsUtl.GetVideoAudioCodec(movie),
                                                                    AudioChannels = MediaItemAspectsUtl.GetVideoAudioChannel(movie),
                                                                    ThreeDimensional = false,
                                
                                                                  Ids = new TraktMovieIds
                                                                  {
                                                                    Imdb = MediaItemAspectsUtl.GetMovieImdbId(movie),
                                                                    Tmdb = MediaItemAspectsUtl.GetMovieTmdbId(movie)
                                                                  },
                                                                  Title = MediaItemAspectsUtl.GetMovieTitle(movie),
                                                                  Year = MediaItemAspectsUtl.GetMovieYear(movie),
                                                                  CollectedAt = MediaItemAspectsUtl.GetDateAddedToDb(movie)
                                                                }).ToList();

      if (syncCollectedMovies.Any())
      {
        _mediaPortalServices.GetLogger().Info("Trakt: trying to add {0} collected movies to trakt collection", syncCollectedMovies.Count);

        foreach (var traktSyncCollectionPostMovie in syncCollectedMovies)
        {
          string audio = traktSyncCollectionPostMovie.Audio?.DisplayName;
          string channel = traktSyncCollectionPostMovie.AudioChannels?.DisplayName;
          string res = traktSyncCollectionPostMovie.MediaResolution?.DisplayName;
          string mediatype = traktSyncCollectionPostMovie.MediaType?.DisplayName;
          string name = traktSyncCollectionPostMovie.Title;
          _mediaPortalServices.GetLogger().Info("Trakt: {0}, {1}, {2}, {3}, {4}", audio, channel, res, mediatype, name);
        }
        ITraktSyncCollectionPostResponse collectionResponse = _traktClient.AddCollectionItems(new TraktSyncCollectionPost { Movies = syncCollectedMovies });
        syncMoviesResult.AddedToTraktCollection = collectionResponse.Added?.Movies;

        if (collectionResponse.Added?.Movies != null)
        {
          _mediaPortalServices.GetLogger().Info("Trakt: successfully added {0} collected movies to trakt collection", collectionResponse.Added.Movies.Value);
        }
      }
      #endregion

      return syncMoviesResult;
    }

    public TraktSyncEpisodesResult SyncSeries()
    {
      _mediaPortalServices.GetLogger().Info("Trakt: start sync series");

      ValidateAuthorization();

      _traktCache.RefreshSeriesCache();

      TraktSyncEpisodesResult syncEpisodesResult = new TraktSyncEpisodesResult();
      IList<Episode> traktUnWatchedEpisodes = _traktCache.UnWatchedEpisodes.ToList();
      IList<EpisodeWatched> traktWatchedEpisodes = _traktCache.WatchedEpisodes.ToList();
      IList<EpisodeCollected> traktCollectedEpisodes = _traktCache.CollectedEpisodes.ToList();

      Guid[] types =
      {
        MediaAspect.ASPECT_ID, EpisodeAspect.ASPECT_ID, VideoAspect.ASPECT_ID, ImporterAspect.ASPECT_ID,
        ProviderResourceAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID
      };
      var contentDirectory = _mediaPortalServices.GetServerConnectionManager().ContentDirectory;
      if (contentDirectory == null)
      {
        throw new MediaLibraryNotConnectedException("ML not connected");
      }

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = _mediaPortalServices.GetUserManagement();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }

      #region Get data from local database

      IList<MediaItem> localEpisodes = contentDirectory.SearchAsync(new MediaItemQuery(types, null, null), true, userProfile, false).Result;

      if (localEpisodes.Any())
      {
        syncEpisodesResult.CollectedInLibrary = localEpisodes.Count;
        _mediaPortalServices.GetLogger().Info("Trakt: found {0} total episodes in library", localEpisodes.Count);
      }

      List<MediaItem> localWatchedEpisodes = localEpisodes.Where(MediaItemAspectsUtl.IsWatched).ToList();

      if (localWatchedEpisodes.Any())
      {
        syncEpisodesResult.WatchedInLibrary = localWatchedEpisodes.Count;
        _mediaPortalServices.GetLogger().Info("Trakt: found {0} episodes watched in library", localWatchedEpisodes.Count);
      }

      #endregion

      #region Mark episodes as unwatched in local database

      _mediaPortalServices.GetLogger().Info("Trakt: start marking series episodes as unwatched in media library");
      if (traktUnWatchedEpisodes.Any())
      {
        // create a unique key to lookup and search for faster
        ILookup<string, MediaItem> localLookupEpisodes = localWatchedEpisodes.ToLookup(twe => CreateLookupKey(twe), twe => twe);

        foreach (var episode in traktUnWatchedEpisodes)
        {
          string tvdbKey = CreateLookupKey(episode);

          var watchedEpisode = localLookupEpisodes[tvdbKey].FirstOrDefault();
          if (watchedEpisode != null)
          {
            _mediaPortalServices.GetLogger().Info(
              "Marking episode as unwatched in library, episode is not watched on trakt. Title = '{0}', Year = '{1}', Season = '{2}', Episode = '{3}', Show TVDb ID = '{4}', Show IMDb ID = '{5}'",
              episode.ShowTitle, episode.ShowYear.HasValue ? episode.ShowYear.ToString() : "<empty>", episode.Season,
              episode.Number, episode.ShowTvdbId.HasValue ? episode.ShowTvdbId.ToString() : "<empty>",
              episode.ShowImdbId ?? "<empty>");

            if (_mediaPortalServices.MarkAsUnWatched(watchedEpisode).Result)
            {
              syncEpisodesResult.MarkedAsUnWatchedInLibrary++;
            }

            // update watched episodes
            localWatchedEpisodes.Remove(watchedEpisode);
          }
        }
      }

      #endregion

      #region Mark episodes as watched in local database

      _mediaPortalServices.GetLogger().Info("Trakt: start marking series episodes as watched in media library");
      if (traktWatchedEpisodes.Any())
      {
        // create a unique key to lookup and search for faster
        ILookup<string, EpisodeWatched> onlineEpisodes = traktWatchedEpisodes.ToLookup(twe => CreateLookupKey(twe), twe => twe);
        List<MediaItem> localUnWatchedEpisodes = localEpisodes.Except(localWatchedEpisodes).ToList();
        foreach (var episode in localUnWatchedEpisodes)
        {
          string tvdbKey = CreateLookupKey(episode);

          var traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();
          if (traktEpisode != null)
          {
            _mediaPortalServices.GetLogger().Info(
              "Marking episode as watched in library, episode is watched on trakt. Plays = '{0}', Title = '{1}', Year = '{2}', Season = '{3}', Episode = '{4}', Show TVDb ID = '{5}', Show IMDb ID = '{6}', Last Watched = '{7}'",
              traktEpisode.Plays, traktEpisode.ShowTitle,
              traktEpisode.ShowYear.HasValue ? traktEpisode.ShowYear.ToString() : "<empty>", traktEpisode.Season,
              traktEpisode.Number, traktEpisode.ShowTvdbId.HasValue ? traktEpisode.ShowTvdbId.ToString() : "<empty>",
              traktEpisode.ShowImdbId ?? "<empty>", traktEpisode.WatchedAt);

            if (_mediaPortalServices.MarkAsWatched(episode).Result)
            {
              syncEpisodesResult.MarkedAsWatchedInLibrary++;
            }
          }
        }
      }

      #endregion

      #region Add episodes to watched history at trakt.tv

      ITraktSyncHistoryPost syncHistoryPost = GetWatchedShowsForSync(localWatchedEpisodes, traktWatchedEpisodes);
      if (syncHistoryPost.Shows != null && syncHistoryPost.Shows.Any())
      {
        _mediaPortalServices.GetLogger().Info("Trakt: trying to add {0} watched episodes to trakt watched history", syncHistoryPost.Shows.Count());
        ITraktSyncHistoryPostResponse response = _traktClient.AddWatchedHistoryItems(syncHistoryPost);
        syncEpisodesResult.AddedToTraktWatchedHistory = response.Added?.Episodes;

        if (response.Added?.Episodes != null)
        {
          _mediaPortalServices.GetLogger().Info("Trakt: successfully added {0} watched episodes to trakt watched history", response.Added.Episodes.Value);
        }
      }

      #endregion

      #region Add episodes to collection at trakt.tv

      ITraktSyncCollectionPost syncCollectionPost = GetCollectedShowsForSync(localEpisodes, traktCollectedEpisodes);
      if (syncCollectionPost.Shows != null && syncCollectionPost.Shows.Any())
      {
        _mediaPortalServices.GetLogger().Info("Trakt: trying to add {0} collected episodes to trakt collection", syncCollectionPost.Shows.Count());
        ITraktSyncCollectionPostResponse response = _traktClient.AddCollectionItems(syncCollectionPost);
        syncEpisodesResult.AddedToTraktCollection = response.Added?.Episodes;

        if (response.Added?.Episodes != null)
        {
          _mediaPortalServices.GetLogger().Info("Trakt: successfully added {0} collected episodes to trakt collection", response.Added.Episodes.Value);
        }
      }

      #endregion

      return syncEpisodesResult;
    }

    public void BackupMovies()
    {
      Guid[] types =
      {
        MediaAspect.ASPECT_ID, MovieAspect.ASPECT_ID, VideoAspect.ASPECT_ID, ImporterAspect.ASPECT_ID,
        ExternalIdentifierAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID, VideoStreamAspect.ASPECT_ID,
        VideoAudioStreamAspect.ASPECT_ID
      };

      IContentDirectory contentDirectory = _mediaPortalServices.GetServerConnectionManager().ContentDirectory;
      if (contentDirectory == null)
      {
        throw new MediaLibraryNotConnectedException("ML not connected");
      }

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = _mediaPortalServices.GetUserManagement();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }

      IList<MediaItem> collectedMovies = contentDirectory.SearchAsync(new MediaItemQuery(types, null, null), true, userProfile, false).Result;
      IList<MediaLibraryMovie> libraryMovies = new List<MediaLibraryMovie>();

      foreach (MediaItem collectedMovie in collectedMovies)
      {
        libraryMovies.Add(new MediaLibraryMovie
        {
          Title = MediaItemAspectsUtl.GetMovieTitle(collectedMovie),
          AddedToDb = MediaItemAspectsUtl.GetDateAddedToDb(collectedMovie).ToString("O"),
          LastPlayed = MediaItemAspectsUtl.GetLastPlayedDate(collectedMovie).ToString("O"),
          PlayCount = MediaItemAspectsUtl.GetPlayCount(collectedMovie),
          Imdb = MediaItemAspectsUtl.GetMovieImdbId(collectedMovie),
          Year = MediaItemAspectsUtl.GetMovieYear(collectedMovie)
        });
      }
      SaveLibraryMovies(libraryMovies);
    }

    private void SaveLibraryMovies(IEnumerable<MediaLibraryMovie> libraryMovies)
    {
      string libraryMoviesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.MediaLibraryMovies.Value);
      string libraryMoviesJson = JsonConvert.SerializeObject(libraryMovies);
      _fileOperations.FileWriteAllText(libraryMoviesPath, libraryMoviesJson, Encoding.UTF8);
    }

    public void BackupSeries()
    {
      
    }

    private void ValidateAuthorization()
    {
      if (!_traktClient.TraktAuthorization.IsValid)
      {
        string authFilePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.Authorization.Value);
        string savedAuthorization = _fileOperations.FileReadAllText(authFilePath);
        ITraktAuthorization savedAuth = TraktSerializationService.DeserializeAsync<ITraktAuthorization>(savedAuthorization).Result;

        if (!savedAuth.IsRefreshPossible)
        {
          throw new Exception("Saved authorization is not valid.");
        }

        ITraktAuthorization refreshedAuth = _traktClient.RefreshAuthorization(savedAuth.RefreshToken);
        string serializedAuth = TraktSerializationService.SerializeAsync(refreshedAuth).Result;
        _fileOperations.FileWriteAllText(authFilePath, serializedAuth, Encoding.UTF8);
      }
    }

    /// <summary>
    /// Checks if a local movie is the same as an online movie
    /// </summary>
    private bool MovieMatch(MediaItem localMovie, ITraktMovie traktMovie)
    {
      bool result = false;
      // IMDb comparison
      if (!string.IsNullOrEmpty(traktMovie.Ids.Imdb) && !string.IsNullOrEmpty(MediaItemAspectsUtl.GetMovieImdbId(localMovie)))
      {
        result = String.Compare(MediaItemAspectsUtl.GetMovieImdbId(localMovie), traktMovie.Ids.Imdb, StringComparison.OrdinalIgnoreCase) == 0;
      }

      // TMDb comparison
      else if ((MediaItemAspectsUtl.GetMovieTmdbId(localMovie) != 0) && traktMovie.Ids.Tmdb.HasValue)
      {
        result= MediaItemAspectsUtl.GetMovieTmdbId(localMovie) == traktMovie.Ids.Tmdb.Value;
      }

      // Title & Year comparison
      else if (String.Compare(MediaItemAspectsUtl.GetMovieTitle(localMovie), traktMovie.Title,
                 StringComparison.OrdinalIgnoreCase) == 0 &&
               (MediaItemAspectsUtl.GetMovieYear(localMovie) == traktMovie.Year))
      {
        result = true;
      }

      return result;
    }

    private string CreateLookupKey(MediaItem episode)
    {
      var tvdid = MediaItemAspectsUtl.GetTvdbId(episode);
      var seasonIndex = MediaItemAspectsUtl.GetSeasonIndex(episode);
      var episodeIndex = MediaItemAspectsUtl.GetEpisodeIndex(episode);

      return string.Format("{0}_{1}_{2}", tvdid, seasonIndex, episodeIndex);
    }

    private string CreateLookupKey(Episode episode)
    {
      string show;

      if (episode.ShowTvdbId != null)
      {
        show = episode.ShowTvdbId.Value.ToString();
      }
      else if (episode.ShowImdbId != null)
      {
        show = episode.ShowImdbId;
      }
      else
      {
        if (episode.ShowTitle == null)
          return episode.GetHashCode().ToString();

        show = episode.ShowTitle + "_" + episode.ShowYear ?? string.Empty;
      }

      return string.Format("{0}_{1}_{2}", show, episode.Season, episode.Number);
    }

    private ITraktSyncHistoryPost GetWatchedShowsForSync(IList<MediaItem> localWatchedEpisodes, IEnumerable<EpisodeWatched> traktEpisodesWatched)
    {
      _mediaPortalServices.GetLogger().Info("Trakt: finding local shows to add to trakt watched history");
      TraktSyncHistoryPostBuilder builder = new TraktSyncHistoryPostBuilder();
      ILookup<string, EpisodeWatched> onlineEpisodes = traktEpisodesWatched.ToLookup(twe => CreateLookupKey(twe), twe => twe);

      foreach (var episode in localWatchedEpisodes)
      {
        string tvdbKey = CreateLookupKey(episode);
        EpisodeWatched traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();

        if (traktEpisode == null)
        {
          TraktShow show = new TraktShow
          {
            Title = MediaItemAspectsUtl.GetSeriesTitle(episode),
            Ids = new TraktShowIds
            {
              Tvdb = MediaItemAspectsUtl.GetTvdbId(episode),
              Imdb = MediaItemAspectsUtl.GetSeriesImdbId(episode)
            }
          };

          DateTime watchedAt = MediaItemAspectsUtl.GetLastPlayedDate(episode);

          builder.AddShow(show, new PostHistorySeasons
          {
            {
              MediaItemAspectsUtl.GetSeasonIndex(episode),
              new PostHistoryEpisodes
              {
                {MediaItemAspectsUtl.GetEpisodeIndex(episode), watchedAt} 
              }
            }
          });
        }
      }
      return builder.Build();
    }

    private ITraktSyncCollectionPost GetCollectedShowsForSync(IList<MediaItem> localCollectedEpisodes, IEnumerable<EpisodeCollected> traktEpisodesCollected)
    {
      _mediaPortalServices.GetLogger().Info("Trakt: finding local episodes to add to trakt collection");
      TraktSyncCollectionPostBuilder builder = new TraktSyncCollectionPostBuilder();
      ILookup<string, EpisodeCollected> onlineEpisodes = traktEpisodesCollected.ToLookup(tce => CreateLookupKey(tce), tce => tce);

      foreach (var episode in localCollectedEpisodes)
      {
        string tvdbKey = CreateLookupKey(episode);
        EpisodeCollected traktEpisode = onlineEpisodes[tvdbKey].FirstOrDefault();

        if (traktEpisode == null)
        {
          TraktShow show = new TraktShow
          {
            Title = MediaItemAspectsUtl.GetSeriesTitle(episode),
            Ids = new TraktShowIds
            {
              Tvdb = MediaItemAspectsUtl.GetTvdbId(episode),
              Imdb = MediaItemAspectsUtl.GetSeriesImdbId(episode)
            }
          };

          TraktMetadata metadata = new TraktMetadata
          {
            Audio = MediaItemAspectsUtl.GetVideoAudioCodec(episode),
            AudioChannels = MediaItemAspectsUtl.GetVideoAudioChannel(episode),
            MediaResolution = MediaItemAspectsUtl.GetVideoResolution(episode),
            MediaType = MediaItemAspectsUtl.GetVideoMediaType(episode),
            ThreeDimensional = false
          };

          DateTime collectedAt = MediaItemAspectsUtl.GetDateAddedToDb(episode);

          builder.AddShow(show,
            new PostSeasons
            {
              {
                MediaItemAspectsUtl.GetSeasonIndex(episode),
                new PostEpisodes
                {
                  {MediaItemAspectsUtl.GetEpisodeIndex(episode), metadata, collectedAt}
                }
              }
            });
        }
      }
      return builder.Build();
    }
  }
}