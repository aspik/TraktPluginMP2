using System;

namespace TraktPluginMP2.Structures
{
  public class Episode
  {
    public uint? ShowId { get; set; }

    public uint? ShowTvdbId { get; set; }

    public string ShowImdbId { get; set; }

    public string ShowTitle { get; set; }

    public int? ShowYear { get; set; }

    public int? Season { get; set; }

    public int? Number { get; set; }
  }
}