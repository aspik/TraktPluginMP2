using System.Collections.Generic;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Watched;

namespace TraktPluginMP2.Structures
{
  public class TraktMovies
  {
    public IList<ITraktMovie> UnWatched { get; set; }

    public IList<ITraktWatchedMovie> Watched { get; set; }

    public IList<ITraktCollectionMovie> Collected { get; set; }
  }
}