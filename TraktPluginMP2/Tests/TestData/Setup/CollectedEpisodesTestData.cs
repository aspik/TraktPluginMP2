﻿using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using TraktPluginMP2.Structures;

namespace Tests.TestData.Setup
{
  public class CollectedEpisodesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("289590", 2, new List<int> {6}, 100).Episode,
          new MockedDatabaseEpisode("318493", 1, new List<int> {2}, 0).Episode,
          new MockedDatabaseEpisode("998201", 4, new List<int> {1}, 100).Episode
        },
        new List<EpisodeCollected>
        {
          new EpisodeCollected {ShowTvdbId = 289590, Season = 2, Number = 6},
          new EpisodeCollected {ShowTvdbId = 318493, Season = 1, Number = 2},
          new EpisodeCollected {ShowTvdbId = 998201, Season = 4, Number = 1}
        },
        null
      };
      yield return new object[]
      {
        new List<MediaItem>
        {
          new MockedDatabaseEpisode("289590", 2, new List<int> {6}, 100).Episode,
          new MockedDatabaseEpisode("318493", 1, new List<int> {2}, 0).Episode,
          new MockedDatabaseEpisode("998201", 4, new List<int> {1}, 100).Episode
        },
        new List<EpisodeCollected>(),
        3
      };

    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}