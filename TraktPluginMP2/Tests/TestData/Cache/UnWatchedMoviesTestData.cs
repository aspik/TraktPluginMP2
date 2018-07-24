using System;
using System.Collections;
using System.Collections.Generic;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Syncs.Activities;
using TraktNet.Objects.Get.Watched;

namespace Tests.TestData.Cache
{
  public class UnWatchedMoviesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        GetOnlineWatchedMovies_1(),
        new TraktSyncLastActivities {Movies = new TraktSyncMoviesLastActivities {WatchedAt = new DateTime(2018,05,20,20,00,00)}},
        0
      };
      yield return new object[]
      {
        GetOnlineWatchedMovies_2(),
        new TraktSyncLastActivities {Movies = new TraktSyncMoviesLastActivities {WatchedAt = new DateTime(2018,04,20,20,00,00)}},
        2
      };
    }

    private List<ITraktWatchedMovie> GetOnlineWatchedMovies_1()
    {
      return new List<ITraktWatchedMovie>
      {
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt3416828"}}},
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt2179136"}}},
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt0050083"}}}
      };
    }

    private List<ITraktWatchedMovie> GetOnlineWatchedMovies_2()
    {
      return new List<ITraktWatchedMovie>
      {
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt2080374"}}},
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt1291570"}}},
        new TraktWatchedMovie {Movie = new TraktMovie {Ids = new TraktMovieIds {Imdb = "tt0050083"}}}
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}