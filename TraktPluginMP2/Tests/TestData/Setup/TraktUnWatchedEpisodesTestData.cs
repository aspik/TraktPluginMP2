﻿using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using TraktPluginMP2.Structures;

namespace Tests.TestData.Setup
{
  public class TraktUnWatchedEpisodesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("318493", 1, new List<int>{2}, 3).Episode,
          new MockedDatabaseEpisode("998201", 4, new List<int>{1}, 1).Episode
        },
        new List<Episode>
        {
          new Episode {ShowTvdbId = 234593, Season = 4, Number = 6},
          new Episode {ShowTvdbId = 092101, Season = 3, Number = 8}
        },
        0
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("289590", 2, new List<int>{6}, 1).Episode,
          new MockedDatabaseEpisode("318493", 1, new List<int>{2}, 3).Episode,
        },
        new List<Episode>
        {
          new Episode {ShowTvdbId = 318493, Season = 1, Number = 2}
        },
        1
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("289590", 2, new List<int>{6}, 1).Episode,
          new MockedDatabaseEpisode("318493", 1, new List<int>{2}, 3).Episode,
          new MockedDatabaseEpisode("998201", 4, new List<int>{1}, 1).Episode
        },
        new List<Episode>
        {
          new Episode {ShowTvdbId = 289590, Season = 2, Number = 6},
          new Episode {ShowTvdbId = 318493, Season = 1, Number = 2},
          new Episode {ShowTvdbId = 998201, Season = 4, Number = 1}
        },
        3
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}