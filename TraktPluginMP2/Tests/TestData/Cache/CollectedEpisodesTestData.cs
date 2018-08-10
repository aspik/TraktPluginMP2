using System;
using System.Collections;
using System.Collections.Generic;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Shows;
using TraktNet.Objects.Get.Syncs.Activities;

namespace Tests.TestData.Cache
{
  public class CollectedEpisodesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        GetOnlineCollectedEpisodes_1(),
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {CollectedAt = new DateTime(2018,04,20,17,00,00)}},
        3
      };
      yield return new object[]
      {
        GetOnlineCollectedEpisodes_2(),
        new TraktSyncLastActivities {Episodes = new TraktSyncEpisodesLastActivities {CollectedAt = new DateTime(2018,04,21,17,00,00)}},
        6
      };
    }

    private List<ITraktCollectionShow> GetOnlineCollectedEpisodes_1()
    {
      return new List<ITraktCollectionShow>
      {
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 317653, Imdb = "tt6682754"}
          },
          CollectionSeasons = new List<ITraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason()
            {
              Number = 1,
              Episodes = new List<ITraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode() {Number = 1}
              }
            }
          }
        },
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 248682, Imdb = "tt1826940"}
          },
          CollectionSeasons = new List<ITraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 7,
              Episodes = new List<ITraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 3}
              }
            }
          }
        },
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 275278, Imdb = "tt3597606"}
          },
          CollectionSeasons = new List<ITraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 1,
              Episodes = new List<ITraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 1}
              }
            }
          }
        }
      };
    }

    private List<ITraktCollectionShow> GetOnlineCollectedEpisodes_2()
    {
      return new List<ITraktCollectionShow>
      {
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 317653, Imdb = "tt6682754"}
          },
          CollectionSeasons = new List<ITraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 9,
              Episodes = new List<ITraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 1},
                new TraktCollectionShowEpisode {Number = 2}
              }
            }
          }
        },
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 248682, Imdb = "tt1826940"}
          },
          CollectionSeasons = new List<ITraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 1,
              Episodes = new List<ITraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 3}
              }
            }
          }
        },
        new TraktCollectionShow
        {
          Show = new TraktShow
          {
            Ids = new TraktShowIds {Tvdb = 275278, Imdb = "tt3597606"}
          },
          CollectionSeasons = new List<ITraktCollectionShowSeason>
          {
            new TraktCollectionShowSeason
            {
              Number = 1,
              Episodes = new List<ITraktCollectionShowEpisode>
              {
                new TraktCollectionShowEpisode {Number = 1},
                new TraktCollectionShowEpisode {Number = 2},
                new TraktCollectionShowEpisode {Number = 3}
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