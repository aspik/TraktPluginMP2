using System;

namespace TraktPluginMP2.Structures
{
  public class EpisodeCollected : Episode
  {
    public DateTime? CollectedAt { get; set; }
  }
}