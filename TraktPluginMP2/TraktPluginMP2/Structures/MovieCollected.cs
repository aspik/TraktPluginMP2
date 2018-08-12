using System;

namespace TraktPluginMP2.Structures
{
  public class MovieCollected : Movie
  {
      public DateTime? CollectedAt { get; set; }
  }
}