using System.Collections.Generic;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Watched;
using TraktPluginMP2.Structures;

namespace TraktPluginMP2.Services
{
  public interface ITraktCache
  {
    TraktMovies RefreshMoviesCache();

    TraktEpisodes RefreshSeriesCache(); 

    void ClearLastActivity(string filename);
  }
}