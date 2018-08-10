using System.Collections;
using System.Collections.Generic;
using NSubstitute;
using Tests.TestData.Setup;
using TraktNet.Enums;
using TraktNet.Objects.Authentication;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Post.Scrobbles.Responses;
using TraktPluginMP2.Notifications;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;

namespace Tests.TestData.Handlers
{
  public class StartScrobbleMovieTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      const string title = "Movie1 (2012)";
      yield return new object[]
      {
        new TraktPluginSettings
        {
          IsScrobbleEnabled = true,
          ShowScrobbleStartedNotifications = true
        },
        new MockedDatabaseMovie("tt12345", "67890", title, 2012, 0).Movie,
        GetMockedTraktClientWithValidAuthorization(),
        new TraktScrobbleStartedNotification(title, true, 10, "Start")
      };
    }

    private ITraktClient GetMockedTraktClientWithValidAuthorization()
    {
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.TraktAuthorization.Returns(new TraktAuthorization
      {
        RefreshToken = "ValidToken",
        AccessToken = "ValidToken",
        
      });

      traktClient.RefreshAuthorization(Arg.Any<string>()).Returns(new TraktAuthorization
      {
        RefreshToken = "ValidToken"
      });
      traktClient.StartScrobbleMovie(Arg.Any<ITraktMovie>(), Arg.Any<float>()).Returns(
        new TraktMovieScrobblePostResponse
        {
          Movie = new TraktMovie
          {
            Ids = new TraktMovieIds { Imdb = "tt1431045", Tmdb = 67890 },
            Title = "Movie1",
            Year = 2012
          },
          Action = TraktScrobbleActionType.Start,
          Progress = 10
        });

      return traktClient;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}