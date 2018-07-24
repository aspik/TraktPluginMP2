using System.Collections;
using System.Collections.Generic;
using NSubstitute;
using Tests.TestData.Setup;
using TraktNet.Enums;
using TraktNet.Objects.Authentication;
using TraktNet.Objects.Get.Episodes;
using TraktNet.Objects.Get.Shows;
using TraktNet.Objects.Post.Scrobbles.Responses;
using TraktPluginMP2.Notifications;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;

namespace Tests.TestData.Handlers
{
  public class StopScrobbleSeriesTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      const string title = "Title_1";
      yield return new object[]
      {
        new TraktPluginSettings
        {
          IsScrobbleEnabled = true,
          ShowScrobbleStoppedNotifications = true
        },
        new MockedDatabaseEpisode("289590", 2, new List<int> {6}, 1).Episode,
        GetMockedTraktClientWithValidAuthorization(),
        new TraktScrobbleStoppedNotification(title, true, 100, "Stop")
      };
    }

    private ITraktClient GetMockedTraktClientWithValidAuthorization()
    {
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.TraktAuthorization.Returns(new TraktAuthorization
      {
        RefreshToken = "ValidToken",
        AccessToken = "ValidToken"
      });

      traktClient.RefreshAuthorization(Arg.Any<string>()).Returns(new TraktAuthorization
      {
        RefreshToken = "ValidToken"
      });
      traktClient.StartScrobbleEpisode(Arg.Any<ITraktEpisode>(), Arg.Any<ITraktShow>(), Arg.Any<float>()).Returns(
        new TraktEpisodeScrobblePostResponse
        {
          Episode = new TraktEpisode
          {
            Ids = new TraktEpisodeIds { Imdb = "tt12345", Tvdb = 289590 },
            Number = 2,
            Title = "Title_1",
            SeasonNumber = 2
          },
          Action = TraktScrobbleActionType.Start,
          Progress = 10
        });

      traktClient.StopScrobbleEpisode(Arg.Any<ITraktEpisode>(), Arg.Any<ITraktShow>(), Arg.Any<float>()).Returns(
        new TraktEpisodeScrobblePostResponse
        {
          Episode = new TraktEpisode
          {
            Ids = new TraktEpisodeIds { Imdb = "tt12345", Tvdb = 289590 },
            Number = 2,
            Title = "Title_1",
            SeasonNumber = 2
          },
          Progress = 100,
          Action = TraktScrobbleActionType.Stop
        });

      return traktClient;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}