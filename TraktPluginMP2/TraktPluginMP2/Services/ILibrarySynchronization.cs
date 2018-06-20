namespace TraktPluginMP2.Services
{
  public interface ILibrarySynchronization
  {
    TraktSyncMoviesResult SyncMovies();

    TraktSyncEpisodesResult SyncSeries();
  }
}