using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

    public TraktMovies RefreshMoviesCache()
    {
      _onlineSyncLastActivities = _traktClient.GetLastActivities();
      _savedSyncLastActivities = SavedLastSyncActivities();

      TraktMovies traktMovies = new TraktMovies
      {
        UnWatched = RefreshUnWatchedMovies(),
        Watched = RefreshWatchedMovies(),
        Collected = RefreshCollectedMovies()
      };

      SaveLastSyncActivities(_savedSyncLastActivities);

      return traktMovies;
    }

    public TraktEpisodes RefreshSeriesCache()
    {
       _onlineSyncLastActivities = _traktClient.GetLastActivities();
      _savedSyncLastActivities = SavedLastSyncActivities();

      TraktEpisodes traktEpisodes = new TraktEpisodes
      {
        UnWatched = RefreshUnWatchedEpisodes(),
        Watched = RefreshWatchedEpisodes(),
        Collected = RefreshCollectedEpisodes()
      };

      SaveLastSyncActivities(_savedSyncLastActivities);

      return traktEpisodes;
    }

    public void ClearLastActivity(string filename)
    {
      ITraktSyncLastActivities lastActivities = SavedLastSyncActivities();
      if (filename.Equals(FileName.CollectedEpisodes.Value))
      {
        lastActivities.Episodes.CollectedAt = null;
      }
      else if (filename.Equals(FileName.CollectedMovies.Value))
      {
        lastActivities.Movies.CollectedAt = null;
      }
      else if (filename.Equals(FileName.WatchedEpisodes.Value))
      {
        lastActivities.Episodes.WatchedAt = null;
      }
      else if (filename.Equals(FileName.WatchedMovies.Value))
      {
        lastActivities.Movies.WatchedAt = null;
      }

      _savedSyncLastActivities = lastActivities;
      SaveLastSyncActivities(_savedSyncLastActivities);
    }

    private IList<ITraktMovie> RefreshUnWatchedMovies()
    {
      IEnumerable<ITraktWatchedMovie> previouslyWatched = CachedWatchedMovies();
      IEnumerable<ITraktWatchedMovie> currentWatched = _traktClient.GetWatchedMovies();
      IEnumerable<ITraktMovie> unWatchedMovies = new List<TraktMovie>();

      // anything not in the current watched that is previously watched
      // must be unwatched now.
      if (previouslyWatched != null)
      {
        unWatchedMovies = from pw in previouslyWatched
          where !currentWatched.Any(m => (m.Movie.Ids.Trakt == pw.Movie.Ids.Trakt || m.Movie.Ids.Imdb == pw.Movie.Ids.Imdb))
          select new TraktMovie
          {
            Ids = pw.Movie.Ids,
            Title = pw.Movie.Title,
            Year = pw.Movie.Year
          };
      }

      return unWatchedMovies.ToList();
    }

    private IList<ITraktWatchedMovie> RefreshWatchedMovies()
    {
      IEnumerable<ITraktWatchedMovie> watchedMovies;
      if (IsCacheInitialized(FileName.WatchedMovies.Value) && _onlineSyncLastActivities.Movies.WatchedAt == _savedSyncLastActivities.Movies.WatchedAt)
      {
        watchedMovies = CachedWatchedMovies();
      }
      else
      {
        watchedMovies = _traktClient.GetWatchedMovies();
        SaveWatchedMovies(watchedMovies);
        _savedSyncLastActivities.Movies.WatchedAt = _onlineSyncLastActivities.Movies.WatchedAt;
      }
      return watchedMovies.ToList();
    }

    private bool IsCacheInitialized(string file)
    {
      string filePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), file);
      return _fileOperations.FileExists(filePath);
    }

    private IList<ITraktCollectionMovie> RefreshCollectedMovies()
    {
      IEnumerable<ITraktCollectionMovie> collectedMovies;
      if (IsCacheInitialized(FileName.CollectedMovies.Value) && _onlineSyncLastActivities.Movies.CollectedAt == _savedSyncLastActivities.Movies.CollectedAt)
      {
        collectedMovies = CachedCollectedMovies();
      }
      else
      {
        collectedMovies = _traktClient.GetCollectedMovies();
        SaveCollectedMovies(collectedMovies);
        _savedSyncLastActivities.Movies.CollectedAt = _onlineSyncLastActivities.Movies.CollectedAt;
      }
      return collectedMovies.ToList();
    }

    private IList<Episode> RefreshUnWatchedEpisodes()
    {
      IEnumerable<Episode> previouslyWatchedEpisodes = GetWatchedEpisodesFromCache();
      IEnumerable<ITraktWatchedShow> currentWatchedShows = _traktClient.GetWatchedShows();
      IEnumerable<ITraktWatchedShow> currentWatchedShowsList = currentWatchedShows.ToList(); 
      IList<EpisodeWatched> currentEpisodesWatched = ConvertWatchedShowsToWatchedEpisodes(currentWatchedShowsList);

      // anything not in the current watched that is previously watched
      // must be unwatched now.
      // Note: we can add to internal cache from external events, so we can't always rely on trakt id for comparisons
      ILookup<string, EpisodeWatched> dictCurrWatched = currentEpisodesWatched.ToLookup(cwe => cwe.ShowTvdbId + "_" + cwe.Season + "_" + cwe.Number);

      IEnumerable<Episode> unWatchedEpisodes = from pwe in previouslyWatchedEpisodes
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

      return unWatchedEpisodes.ToList();
    }

    private static IList<EpisodeWatched> ConvertWatchedShowsToWatchedEpisodes(IEnumerable<ITraktWatchedShow> traktWatchedShows)
    {
      IList<EpisodeWatched> episodesWatched = new List<EpisodeWatched>();
      foreach (ITraktWatchedShow show in traktWatchedShows)
      {
        foreach (ITraktWatchedShowSeason season in show.WatchedSeasons)
        {
          foreach (ITraktWatchedShowEpisode episode in season.Episodes)
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
      return episodesWatched;
    }

    private IList<EpisodeWatched> GetWatchedEpisodesFromOnlineAndSaveItToCache()
    {
      IEnumerable<ITraktWatchedShow> watchedShows = _traktClient.GetWatchedShows();
      string watchedEpisodesFilePath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedEpisodes.Value);
      IEnumerable<ITraktWatchedShow> traktWatchedShows = watchedShows.ToList();
      string watchedEpisodesJson = TraktSerializationService.SerializeCollectionAsync(traktWatchedShows).Result;
      _fileOperations.FileWriteAllText(watchedEpisodesFilePath, watchedEpisodesJson, Encoding.UTF8);

      return ConvertWatchedShowsToWatchedEpisodes(traktWatchedShows);
    }

    private IEnumerable<EpisodeCollected> GetCollectedEpisodesFromOnlineAndSaveItToCache()
    {
      IEnumerable<ITraktCollectionShow> collectedShows = _traktClient.GetCollectedShows();
      string collectedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedEpisodes.Value);
      IEnumerable<ITraktCollectionShow> traktCollectionShows = collectedShows.ToList();
      string collectedEpisodesJson = TraktSerializationService.SerializeCollectionAsync(traktCollectionShows).Result;
      _fileOperations.FileWriteAllText(collectedEpisodesPath, collectedEpisodesJson, Encoding.UTF8);

      return ConvertCollectionShowsToCollectedEpisodes(traktCollectionShows);
    }

    private static IEnumerable<EpisodeCollected> ConvertCollectionShowsToCollectedEpisodes(IEnumerable<ITraktCollectionShow> traktCollectionShows)
    {
      IList<EpisodeCollected> episodesCollected = new List<EpisodeCollected>();
      foreach (ITraktCollectionShow show in traktCollectionShows)
      {
        foreach (ITraktCollectionShowSeason season in show.CollectionSeasons)
        {
          foreach (ITraktCollectionShowEpisode episode in season.Episodes)
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
      return episodesCollected;
    }

    private IList<EpisodeWatched> RefreshWatchedEpisodes()
    {
      IEnumerable<EpisodeWatched> watchedEpisodes;
      if (IsCacheInitialized(FileName.WatchedEpisodes.Value) && _onlineSyncLastActivities.Episodes.WatchedAt == _savedSyncLastActivities.Episodes.WatchedAt)
      {
        watchedEpisodes = GetWatchedEpisodesFromCache();
      }
      else
      {
        watchedEpisodes = GetWatchedEpisodesFromOnlineAndSaveItToCache();
        _savedSyncLastActivities.Episodes.WatchedAt = _onlineSyncLastActivities.Episodes.WatchedAt;
      }
      return watchedEpisodes.ToList();
    }

    private IList<EpisodeCollected> RefreshCollectedEpisodes()
    {
      IEnumerable<EpisodeCollected> collectedEpisodes;
      if (IsCacheInitialized(FileName.CollectedEpisodes.Value) && _onlineSyncLastActivities.Episodes.CollectedAt == _savedSyncLastActivities.Episodes.CollectedAt)
      {
        collectedEpisodes = GetCollectedEpisodesFromCache();
      }
      else
      {
        collectedEpisodes = GetCollectedEpisodesFromOnlineAndSaveItToCache();
        _savedSyncLastActivities.Episodes.CollectedAt = _onlineSyncLastActivities.Episodes.CollectedAt;
      }
      return collectedEpisodes.ToList();
    }

    private IEnumerable<EpisodeCollected> GetCollectedEpisodesFromCache()
    {
      IEnumerable<ITraktCollectionShow> traktCollectionShows = new List<ITraktCollectionShow>();

      string collectedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.CollectedEpisodes.Value);
      if (_fileOperations.FileExists(collectedEpisodesPath))
      {
        string collectedEpisodesJson = _fileOperations.FileReadAllText(collectedEpisodesPath);
        traktCollectionShows = TraktSerializationService.DeserializeCollectionAsync<ITraktCollectionShow>(collectedEpisodesJson).Result;
      }

      return ConvertCollectionShowsToCollectedEpisodes(traktCollectionShows);
    }

    private IEnumerable<EpisodeWatched> GetWatchedEpisodesFromCache()
    {
      IEnumerable<ITraktWatchedShow> traktWatchedShows = new List<ITraktWatchedShow>();

      string watchedEpisodesPath = Path.Combine(_mediaPortalServices.GetTraktUserHomePath(), FileName.WatchedEpisodes.Value);
      if (_fileOperations.FileExists(watchedEpisodesPath))
      {
        string watchedEpisodesJson = _fileOperations.FileReadAllText(watchedEpisodesPath);
        traktWatchedShows = TraktSerializationService.DeserializeCollectionAsync<ITraktWatchedShow>(watchedEpisodesJson).Result;
      }

      return ConvertWatchedShowsToWatchedEpisodes(traktWatchedShows);
    }

    private ITraktSyncLastActivities SavedLastSyncActivities()
    {
      string traktUserHomePath = _mediaPortalServices.GetTraktUserHomePath();
      string savedSyncActivitiesFilePath = Path.Combine(traktUserHomePath, FileName.LastActivity.Value);
      if (!_fileOperations.FileExists(savedSyncActivitiesFilePath))
      {
        return new TraktSyncLastActivities
        {
          Movies = new TraktSyncMoviesLastActivities(),
          Shows = new TraktSyncShowsLastActivities(),
          Episodes = new TraktSyncEpisodesLastActivities()
        };
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
  }
}