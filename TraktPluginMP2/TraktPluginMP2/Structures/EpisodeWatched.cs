using System;

namespace TraktPluginMP2.Structures
{
  public class EpisodeWatched : Episode
  {
    public int? Plays { get; set; }

    public DateTime? WatchedAt { get; set; }
  }
}