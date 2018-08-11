using System.Collections.Generic;

namespace TraktPluginMP2.Structures
{
  public class TraktEpisodes
  {
    public IList<Episode> UnWatched { get; set; }

    public IList<EpisodeWatched> Watched { get; set; }

    public IList<EpisodeCollected> Collected { get; set; }
  }
}