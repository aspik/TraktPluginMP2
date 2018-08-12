namespace TraktPluginMP2.Structures
{
  public class Movie
  {
      public string Title { get; set; }

      public int? Year { get; set; }

      public uint TraktId { get; set; }

      public string Imdb { get; set; }

      public uint? Tmdb { get; set; }
    }
}