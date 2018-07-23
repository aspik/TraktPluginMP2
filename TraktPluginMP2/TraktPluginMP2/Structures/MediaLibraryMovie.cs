using System;
using Newtonsoft.Json;

namespace TraktPluginMP2.Structures
{
  public class MediaLibraryMovie
  {
    [JsonProperty(PropertyName = "added_to_db")]
    public String AddedToDb { get; set; }

    [JsonProperty(PropertyName = "last_played")]
    public String LastPlayed { get; set; }

    [JsonProperty(PropertyName = "imdb")]
    public string Imdb { get; set; }

    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; }

    [JsonProperty(PropertyName = "year")]
    public int Year { get; set; }

    [JsonProperty(PropertyName = "play_count")]
    public int PlayCount { get; set; }
  }
}