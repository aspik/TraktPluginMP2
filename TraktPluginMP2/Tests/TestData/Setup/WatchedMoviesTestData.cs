using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using TraktPluginMP2.Structures;

namespace Tests.TestData.Setup
{
  public class WatchedMoviesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 100).Movie,
          new MockedDatabaseMovie("", "16729", "Movie_2", 2016, 100).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2011, 100).Movie
        },
        new List<MovieWatched>
        {
          new MovieWatched {Imdb = "tt12345", Tmdb = 67890, Title = "Movie_1", Year = 2012},
          new MovieWatched {Imdb = "tt67804", Tmdb = 16729, Title = "Movie_2", Year = 2016},
          new MovieWatched {Imdb = "tt03412", Tmdb = 34251, Title = "Movie_3", Year = 2011}
        },
        null
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 100).Movie,
          new MockedDatabaseMovie("", "16729", "Movie_2", 2016, 100).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2011, 100).Movie
        },
        new List<MovieWatched>
        {
          new MovieWatched {Imdb = "tt12345", Tmdb = 67890, Title = "Movie_1", Year = 2012},
          new MovieWatched {Imdb = "tt67804", Tmdb = 16729, Title = "Movie_2", Year = 2016}
        },
        1
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 100).Movie,
          new MockedDatabaseMovie("", "67890", "Movie_2", 2016, 100).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2010, 100).Movie
        },
        new List<MovieWatched>(),
        3
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}