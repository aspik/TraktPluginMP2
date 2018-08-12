using System.Collections.Generic;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Watched;

namespace TraktPluginMP2.Structures
{
  public class TraktMovies
  {
    public IList<Movie> UnWatched { get; set; }

    public IList<MovieWatched> Watched { get; set; }

    public IList<MovieCollected> Collected { get; set; }
  }
}