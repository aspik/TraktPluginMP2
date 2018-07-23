using System.Collections.Generic;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Movies;
using TraktApiSharp.Objects.Get.Watched;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public interface ITraktCache
  {
    void RefreshMoviesCache();
    void RefreshSeriesCache();
    IEnumerable<TraktMovie> UnWatchedMovies { get; } 
    IEnumerable<TraktWatchedMovie> WatchedMovies { get; } 
    IEnumerable<TraktCollectionMovie> CollectedMovies { get; }
    IEnumerable<Episode> UnWatchedEpisodes { get; } 
    IEnumerable<EpisodeWatched> WatchedEpisodes { get; } 
    IEnumerable<EpisodeCollected> CollectedEpisodes { get; }
  }
}