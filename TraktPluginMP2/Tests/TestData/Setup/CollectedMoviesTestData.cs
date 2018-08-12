using System;
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using TraktPluginMP2.Structures;

namespace Tests.TestData.Setup
{
  public class CollectedMoviesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 0).Movie,
          new MockedDatabaseMovie("", "67890", "Movie_2", 2016, 0).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2010, 100).Movie
        },
        new List<MovieCollected>(),
        3
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 0).Movie,
          new MockedDatabaseMovie("", "16729", "Movie_2", 2016, 100).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2010, 100).Movie
        },
        new List<MovieCollected>
        {
          new MovieCollected {Imdb = "tt12345", Tmdb = 67890, Title = "Movie_1", Year = 2012, CollectedAt = DateTime.Now}
        },
        2
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseMovie("tt12345", "67890", "Movie_1", 2012, 100).Movie,
          new MockedDatabaseMovie("", "16729", "Movie_2", 2008, 100).Movie,
          new MockedDatabaseMovie("", "0", "Movie_3", 2001, 100).Movie
        },
        new List<MovieCollected>
        {
          new MovieCollected {Imdb = "tt12345", Tmdb = 67890, Title = "Movie_1", Year = 2012, CollectedAt = DateTime.Now},
          new MovieCollected {Imdb = "tt42690", Tmdb = 16729, Title = "Movie_2", Year = 2008, CollectedAt = DateTime.Now},
          new MovieCollected {Imdb = "tt00754", Tmdb = 34251, Title = "Movie_3", Year = 2001, CollectedAt = DateTime.Now}
        },
        null
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}