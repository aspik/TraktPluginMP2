using System;
using System.Collections;
using System.Collections.Generic;
using TraktNet.Objects.Get.Shows;
using TraktNet.Objects.Get.Syncs.Activities;
using TraktNet.Objects.Get.Watched;

namespace Tests.TestData.Cache
{
  public class WatchedEpisodesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        GetOnlineWatchedEpisodes_1(),
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {WatchedAt = new DateTime(2018,04,20,20,00,00)}},
        4
      };
      yield return new object[]
      {
        GetOnlineWatchedEpisodes_2(),
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {WatchedAt = new DateTime(2018,04,26,20,00,00)}},
        5
      };
    }

    private List<ITraktWatchedShow> GetOnlineWatchedEpisodes_1()
    {
      return new List<ITraktWatchedShow>
      {
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 248682, Imdb = "tt1826940"}
          },
          WatchedSeasons = new List<ITraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason()
            {
              Number = 9,
              Episodes = new List<ITraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode() {Number = 1},
                new TraktWatchedShowEpisode() {Number = 2}
              }
            }
          }
        },
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 275278, Imdb = "tt3597606"}
          },
          WatchedSeasons = new List<ITraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 1,
              Episodes = new List<ITraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1}
              }
            }
          }
        },
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 317653, Imdb = "tt6682754"}
          },
          WatchedSeasons = new List<ITraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 1,
              Episodes = new List<ITraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1}
              }
            }
          }
        }
      };
    }

    private List<ITraktWatchedShow> GetOnlineWatchedEpisodes_2()
    {
      return new List<ITraktWatchedShow>
      {
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 80379, Imdb = "tt0898266"}
          },
          WatchedSeasons = new List<ITraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 9,
              Episodes = new List<ITraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1},
                new TraktWatchedShowEpisode {Number = 2}
              }
            }
          }
        },
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 298901, Imdb = "tt4635276"}
          },
          WatchedSeasons = new List<ITraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 1,
              Episodes = new List<ITraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1}
              }
            }
          }
        },
        new TraktWatchedShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 248682, Imdb = "tt1826940"}
          },
          WatchedSeasons = new List<ITraktWatchedShowSeason>
          {
            new TraktWatchedShowSeason
            {
              Number = 1,
              Episodes = new List<ITraktWatchedShowEpisode>
              {
                new TraktWatchedShowEpisode {Number = 1},
                new TraktWatchedShowEpisode {Number = 2}
              }
            }
          }
        }
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}