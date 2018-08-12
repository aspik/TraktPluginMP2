using System;

namespace TraktPluginMP2.Structures
{
  public class MovieWatched : Movie
  {
      public int? Plays { get; set; }

      public DateTime? WatchedAt { get; set; }
  }
}