using System.Collections.Generic;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Watched;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public interface ITraktCache
  {
    void RefreshMoviesCache();
    void RefreshSeriesCache();
    IEnumerable<ITraktMovie> UnWatchedMovies { get; } 
    IEnumerable<ITraktWatchedMovie> WatchedMovies { get; } 
    IEnumerable<ITraktCollectionMovie> CollectedMovies { get; }
    IEnumerable<Episode> UnWatchedEpisodes { get; } 
    IEnumerable<EpisodeWatched> WatchedEpisodes { get; } 
    IEnumerable<EpisodeCollected> CollectedEpisodes { get; }
  }
}