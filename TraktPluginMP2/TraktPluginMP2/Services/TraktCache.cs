using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Syncs.Activities;
using TraktNet.Objects.Get.Watched;
using TraktNet.Services;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public class TraktCache : ITraktCache
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktClient _traktClient;
    private readonly IFileOperations _fileOperations;

    private ITraktSyncLastActivities _onlineSyncLastActivities;
    private ITraktSyncLastActivities _savedSyncLastActivities;

    public TraktCache(IMediaPortalServices mediaPortalServices, ITraktClient traktClient, IFileOperations fileOperations)
    {
      _mediaPortalServices = mediaPortalServices;
      _traktClient = traktClient;
      _fileOperations = fileOperations;
    }

    public IEnumerable<ITraktMovie> UnWatchedMovies { get; private set; } = new List<TraktMovie>();
    public IEnumerable<ITraktWatchedMovie> WatchedMovies { get; private set; } = new List<TraktWatchedMovie>();
    public IEnumerable<ITraktCollectionMovie> CollectedMovies { get; private set; } = new List<TraktCollectionMovie>();

    public IEnumerable<Episode> UnWatchedEpisodes { get; private set; } = new List<Episode>();
    public IEnumerable<EpisodeWatched> WatchedEpisodes { get; private set; } = new List<EpisodeWatched>();
    public IEnumerable<EpisodeCollected> CollectedEpisodes { get; private set; } = new List<EpisodeCollected>();

    public void RefreshMoviesCache()
    {
      _onlineSyncLastActivities = _traktClient.GetLastActivities();
      _savedSyncLastActivities = SavedLastSyncActivities();

      RefreshUnWatchedMovies();
      RefreshWatchedMovies();
      RefreshCollectedMovies();

      SaveLastSyncActivities(_onlineSyncLastActivities);
    }

    public void RefreshSeriesCache()
    {
      _onlineSyncLastActivities = _traktClient.GetLastActivities();
      _savedSyncLastActivities = SavedLastSyncActivities();

      RefreshUnWatchedEpisodes();
      RefreshWatchedEpisodes();
      RefreshCollectedEpisodes();

      SaveLastSyncActivities(_onlineSyncLastActivities);
    }

    private void RefreshUnWatchedMovies()
    {
      IEnumerable<ITraktWatchedMovie> previouslyWatched = CachedWatchedMovies();
      IEnumerable<ITraktWatchedMovie> currentWatched = _traktClient.GetWatchedMovies();

      // anything not in the current watched that is previously watched
      // must be unwatched now.
      if (previouslyWatched != null)
      {
        UnWatchedMovies = from pw in previouslyWatched
          where !currentWatched.Any(m => (m.Movie.Ids.Trakt == pw.Movie.Ids.Trakt || m.Movie.Ids.Imdb == pw.Movie.Ids.Imdb))
          select new TraktMovie
          {
            Ids = pw.Movie.Ids,
            Title = pw.Movie.Title,
            Year = pw.Movie.Year
          };
      }
    }

    private void RefreshWatchedMovies()
    {
      DateTime? onlineWatchedMoviesDate = _onlineSyncLastActivities.Movies.WatchedAt;
      DateTime? savedWatchedMoviesDate = _savedSyncLastActivities.Movies.WatchedAt;
      WatchedMovies = IsCacheUpToDate(onlineWatchedMoviesDate, savedWatchedMoviesDate) ? CachedWatchedMovies() : OnlineWatchedMovies();
    }

    private bool IsCacheUpToDate(DateTime? dateTime, DateTime? dateTime1)
    {
      return dateTime == dateTime1;
    }

    private void RefreshCollectedMovies()
    {
      DateTime? onlineCollectedMoviesDate = _onlineSyncLastActivities.Movies.CollectedAt;
      DateTime? savedCollectedMoviesDate = _savedSyncLastActivities.Movies.CollectedAt;
      CollectedMovies = IsCacheUpToDate(onlineCollectedMoviesDate, savedCollectedMoviesDate) ? CachedCollectedMovies() : OnlineCollectedMovies();
    }

    private IEnumerable<ITraktCollectionMovie> OnlineCollectedMovies()
    {
      IEnumerable<ITraktCollectionMovie> collectedMovies = _traktClient.GetCollectedMovies();
      SaveCollectedMovies(collectedMovies);

      return collectedMovies;
    }

    private IEnumerable<ITraktWatchedMovie> OnlineWatchedMovies()
    {
      IEnumerable<ITraktWatchedMovie> watchedMovies = _traktClient.GetWatchedMovies();
      SaveWatchedMovies(watchedMovies);
      return watchedMovies;
    }

    private void RefreshUnWatchedEpisodes()
    {
      IEnumerable<Episode> previouslyWatched = CachedWatchedEpisodes();
      IEnumerable<ITraktWatchedShow> currentWatchedShows = _traktClient.GetWatchedShows();
      IList<EpisodeWatched> currentEpisodesWatched = new List<EpisodeWatched>();
      // convert to internal data structure
      foreach (var show in currentWatchedShows)
      {
        foreach (var season in show.WatchedSeasons)
        {
          foreach (var episode in season.Episodes)
          {
            currentEpisodesWatched.Add(new EpisodeWatched
            {
              ShowId = show.Show.Ids.Trakt,
              ShowTvdbId = show.Show.Ids.Tvdb,
              ShowImdbId = show.Show.Ids.Imdb,
              ShowTitle = show.Show.Title,
              ShowYear = show.Show.Year,
              Season = season.Number,
              Number = episode.Number,
              Plays = episode.Plays,
              WatchedAt = episode.LastWatchedAt
            });
          }
        }
      }
      // anything not in the current watched that is previously watched
      // must be unwatched now.
      // Note: we can add to internal cache from external events, so we can't always rely on trakt id for comparisons
      ILookup<string, EpisodeWatched> dictCurrWatched = currentEpisodesWatched.ToLookup(cwe => cwe.ShowTvdbId + "_" + cwe.Season + "_" + cwe.Number);

      UnWatchedEpisodes = from pwe in previouslyWatched
                                               where !dictCurrWatched[pwe.ShowTvdbId + "_" + pwe.Season + "_" + pwe.Number].Any()
                                               select new Episode
                                               {
                                                 ShowId = pwe.ShowId,
                                                 ShowTvdbId = pwe.ShowTvdbId,
                                                 ShowImdbId = pwe.ShowImdbId,
                                                 ShowTitle = pwe.ShowTitle,
                                                 ShowYear = pwe.ShowYear,
                                                 Season = pwe.Season,
                                                 Number = pwe.Number
                                               };
    }

    private IList<EpisodeWatched> OnlineWatchedEpisodes()
    {
      IList<EpisodeWatched> episodesWatched = new List<EpisodeWatched>();

      IEnumerable<ITraktWatchedShow> watchedShows = _traktClient.GetWatchedShows();

      // convert to internal data structure
      foreach (var show in watchedShows)
      {
        foreach (var season in show.WatchedSeasons)
        {
          foreach (var episode in season.Episodes)
          {
            episodesWatched.Add(new EpisodeWatched
            {
              ShowId = show.Show.Ids.Trakt,
              ShowTvdbId = show.Show.Ids.Tvdb,
              ShowImdbId = show.Show.Ids.Imdb,
              ShowTitle = show.Show.Title,
              ShowYear = show.Show.Year,
              Number = episode.Number,
              Season = season.Number,
              Plays = episode.Plays,
              WatchedAt = episode.LastWatchedAt
            });
          }
        }
      }
      SaveWatchedEpisodes(episodesWatched);
      return episodesWatched;
    }

    private void RefreshWatchedEpisodes()
    {
      DateTime? onlineWatchedEpisodesDate = _onlineSyncLastActivities.Episodes.WatchedAt;
      DateTime? savedWatchedEpisodesDate = _savedSyncLastActivities.Episodes.WatchedAt;
      WatchedEpisodes = IsCacheUpToDate(onlineWatchedEpisodesDate, savedWatchedEpisodesDate) ? CachedWatchedEpisodes() : OnlineWatchedEpisodes();
    }

    private void RefreshCollectedEpisodes()
    {
      DateTime? onlineCollectedEpisodesDate = _onlineSyncLastActivities.Episodes.CollectedAt;
      DateTime? savedCollectedEpisodesDate = _savedSyncLastActivities.Episodes.CollectedAt;
      CollectedEpisodes = IsCacheUpToDate(onlineCollectedEpisodesDate, savedCollectedEpisodesDate) ? CachedCollectedEpisodes() : OnlineCollectedEpisodes();
    }

    private IEnumerable<EpisodeCollected> OnlineCollectedEpisodes()
    {
      IList<EpisodeCollected> episodesCollected = new List<EpisodeCollected>();

      IEnumerable<ITraktCollectionShow> collectedShows = _traktClient.GetCollectedShows();

      // convert to internal data structure
      foreach (var show in collectedShows)
      {
        foreach (var season in show.CollectionSeasons)
        {
          foreach (var episode in season.Episodes)
          {
            episodesCollected.Add(new EpisodeCollected
            {
              ShowId = show.Show.Ids.Trakt,
              ShowTvdbId = show.Show.Ids.Tvdb,
              ShowImdbId = show.Show.Ids.Imdb,
              ShowTitle = show.Show.Title,
              ShowYear = show.Show.Year,
              Number = episode.Number,
              Season = season.Number,
              CollectedAt = episode.CollectedAt
            });
          }
        }
      }
      SaveCollectedEpisodes(episodesCollected);

      return episodesCollected;
    }

    private ITraktSyncLastActivities SavedLastSyncActivities()
    {
      string traktUserHomePath = _mediaPortalServices.GetTraktUserHomePath();
      string savedSyncActivitiesFilePath = Path.Combine(traktUserHomePath, FileName.LastActivity.Value);
      if (!_fileOperations.FileExists(savedSyncActivitiesFilePath))
      {
        throw new Exception("Last sync activities file could not be found in: " + traktUserHomePath);
      }
      string savedSyncActivitiesJson = _fileOperations.FileReadAllText(savedSyncActivitiesFilePath);
      return TraktSerializationService.DeserializeAsync<ITraktSyncLastActivities>(savedSyncActivitiesJson).Result;
    }

    private IEnumerable<ITraktWatchedMovie> CachedWatchedMovies()
    {
      IEnumerable<ITraktWatchedMovie> watchedMovies = new List<ITraktWatchedMovie>();

      string watchedMoviesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedMovies.Value);
      if (_fileOperations.FileExists(watchedMoviesPath))
      {
        string watchedMoviesJson = _fileOperations.FileReadAllText(watchedMoviesPath);
        watchedMovies = TraktSerializationService.DeserializeCollectionAsync<ITraktWatchedMovie>(watchedMoviesJson).Result;
      }
      return watchedMovies;
    }

    private IEnumerable<ITraktCollectionMovie> CachedCollectedMovies()
    {
      IEnumerable<ITraktCollectionMovie> collectedMovies = new List<ITraktCollectionMovie>();

      string collectedMoviesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedMovies.Value);
      if (_fileOperations.FileExists(collectedMoviesPath))
      {
        string collectedMoviesJson = _fileOperations.FileReadAllText(collectedMoviesPath);
        collectedMovies = TraktSerializationService.DeserializeCollectionAsync<ITraktCollectionMovie>(collectedMoviesJson).Result;
      }
      return collectedMovies;
    }

    private IEnumerable<EpisodeWatched> CachedWatchedEpisodes()
    {
      IEnumerable<EpisodeWatched> watchedEpisodes = new List<EpisodeWatched>();

      string watchedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedEpisodes.Value);
      if (_fileOperations.FileExists(watchedEpisodesPath))
      {
        string watchedEpisodesJson = _fileOperations.FileReadAllText(watchedEpisodesPath);
        watchedEpisodes = JsonConvert.DeserializeObject<List<EpisodeWatched>>(watchedEpisodesJson);
      }
      return watchedEpisodes;
    }

    private IEnumerable<EpisodeCollected> CachedCollectedEpisodes()
    {
      IEnumerable<EpisodeCollected> collectedEpisodes = new List<EpisodeCollected>();

      string collectedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedEpisodes.Value);
      if (_fileOperations.FileExists(collectedEpisodesPath))
      {
        string collectedEpisodesJson = _fileOperations.FileReadAllText(collectedEpisodesPath);
        collectedEpisodes = JsonConvert.DeserializeObject<List<EpisodeCollected>>(collectedEpisodesJson);
      }
      return collectedEpisodes;
    }

    private void SaveLastSyncActivities(ITraktSyncLastActivities syncLastActivities)
    {
      string lastSyncActivitiesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.LastActivity.Value);
      string lastSyncActivitiesJson = TraktSerializationService.SerializeAsync(syncLastActivities).Result;
      _fileOperations.FileWriteAllText(lastSyncActivitiesPath, lastSyncActivitiesJson, Encoding.UTF8);
    }

    private void SaveWatchedMovies(IEnumerable<ITraktWatchedMovie> watchedMovies)
    {
      string watchedMoviesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedMovies.Value);
      string watchedMoviesJson = TraktSerializationService.SerializeCollectionAsync(watchedMovies).Result;
      _fileOperations.FileWriteAllText(watchedMoviesPath, watchedMoviesJson, Encoding.UTF8);
    }

    private void SaveCollectedMovies(IEnumerable<ITraktCollectionMovie> collectedMovies)
    {
      string collectedMoviesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedMovies.Value);
      string collectedMoviesJson = TraktSerializationService.SerializeCollectionAsync(collectedMovies).Result;
      _fileOperations.FileWriteAllText(collectedMoviesPath, collectedMoviesJson, Encoding.UTF8);
    }

    private void SaveWatchedEpisodes(IList<EpisodeWatched> episodesWatched)
    {
      string watchedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedEpisodes.Value);
      string watchedEpisodesJson = JsonConvert.SerializeObject(episodesWatched);
      _fileOperations.FileWriteAllText(watchedEpisodesPath, watchedEpisodesJson, Encoding.UTF8);
    }

    private void SaveCollectedEpisodes(IList<EpisodeCollected> episodesCollected)
    {
      string collectedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedEpisodes.Value);
      string collectedEpisodesJson = JsonConvert.SerializeObject(episodesCollected);
      _fileOperations.FileWriteAllText(collectedEpisodesPath, collectedEpisodesJson, Encoding.UTF8);
    }
  }
}