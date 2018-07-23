using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using Newtonsoft.Json;
using NSubstitute;
using Tests.TestData.Setup;
using TraktApiSharp.Authentication;
using TraktApiSharp.Objects.Get.Collection;
using TraktApiSharp.Objects.Get.Watched;
using TraktApiSharp.Objects.Post.Syncs.Collection;
using TraktApiSharp.Objects.Post.Syncs.Collection.Responses;
using TraktApiSharp.Objects.Post.Syncs.History;
using TraktApiSharp.Objects.Post.Syncs.History.Responses;
using TraktPluginMP2;
using TraktPluginMP2.Services;
using TraktPluginMP2.Structures;
using Xunit;

namespace Tests
{
  public class TraktIntegrationTests
  {
    const string ApplicationId = "aea41e88de3cd0f8c8b2404d84d2e5d7317789e67fad223eba107aea2ef59068";
    const string SecretId = "adafedb5cd065e6abeb9521b8b64bc66adb010a7c08128811bf32c989f35b77a";

    const string HomeUserPath = "C:\\ProgramData\\Team MediaPortal\\MP2-Client\\Trakt\\dd68bd88-ea40-4ac4-b5fd-3c99a95551ce";

    [Fact]
    public void SyncCollectedMoviesToTrakt()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.MarkAsWatched(Arg.Any<MediaItem>()).Returns(true);
      mediaPortalServices.MarkAsUnWatched(Arg.Any<MediaItem>()).Returns(true);
      mediaPortalServices.GetTraktUserHomePath()
        .Returns(HomeUserPath);

      IList<MediaItem> collectedMovies = new List<MediaItem>();
      IList<MediaLibraryMovie> savedMovies = new List<MediaLibraryMovie>();
      string collectedMoviesPath = Path.Combine(mediaPortalServices.GetTraktUserHomePath(), FileName.MediaLibraryMovies.Value);
      if (File.Exists(collectedMoviesPath))
      {
        string collectedMoviesJson = File.ReadAllText(collectedMoviesPath);
        savedMovies = JsonConvert.DeserializeObject<List<MediaLibraryMovie>>(collectedMoviesJson);
      }

      foreach (MediaLibraryMovie movie in savedMovies)
      {
        collectedMovies.Add(new MockedDatabaseMovie(new MediaLibraryMovie
        {
          Title = movie.Title,
          Imdb = movie.Imdb,
          Year = movie.Year,
          AddedToDb = movie.AddedToDb,
          LastPlayed = movie.LastPlayed,
          PlayCount = movie.PlayCount
        }).Movie);
      }

      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), true, null, false).Returns(collectedMovies);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      ILogger logger = Substitute.For<ILogger>();
      ITraktClient traktClient = new TraktClientProxy(ApplicationId, SecretId, logger);
 

      ValidateAuthorization(traktClient, new FileOperations());

      IFileOperations fileOperations = new FileOperations();
      ITraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncMoviesResult result = librarySynchronization.SyncMovies();

      // Assert
      Assert.NotNull(result);
    }

    [Fact]
    public void SyncWatchedMoviesToTrakt()
    {
      // Arrange

      // Act

      // Assert
    }

    [Fact]
    public void SyncCollectedSeriesToTrakt()
    {
      // Arrange

      // Act

      // Assert
    }

    [Fact]
    public void SyncWatchedSeriesToTrakt()
    {
      // Arrange

      // Act

      // Assert
    }

    [Fact]
    public void CleanUpCollectedMoviesAtTrakt()
    {
      // Arrange
      ILogger logger = Substitute.For<ILogger>();
      ITraktClient traktClient = new TraktClientProxy(ApplicationId, SecretId, logger);
      ValidateAuthorization(traktClient, new FileOperations());
      IEnumerable<TraktCollectionMovie> collectedMovies = traktClient.GetCollectedMovies();
      IList<TraktSyncCollectionPostMovie> collectionPostMovies = new List<TraktSyncCollectionPostMovie>();

      foreach (TraktCollectionMovie traktCollectionMovie in collectedMovies)
      {
        collectionPostMovies.Add(new TraktSyncCollectionPostMovie
        {
          Metadata = traktCollectionMovie.Metadata,
          Ids = traktCollectionMovie.Movie.Ids,
          Year = traktCollectionMovie.Movie.Year,
          Title = traktCollectionMovie.Movie.Title
        });
      }

      // Act
      TraktSyncCollectionRemovePostResponse response = traktClient.RemoveCollectionItems(new TraktSyncCollectionPost{Movies = collectionPostMovies});

      // Assert
      Assert.Equal(collectedMovies.Count(), response.Deleted.Movies);
    }

    [Fact]
    public void CleanUpCollectedShowAttrakt()
    {
      // Arrange
      ILogger logger = Substitute.For<ILogger>();
      ITraktClient traktClient = new TraktClientProxy(ApplicationId, SecretId, logger);
      ValidateAuthorization(traktClient, new FileOperations());
      IEnumerable<TraktCollectionShow> collectionShows = traktClient.GetCollectedShows();
      IList<TraktSyncCollectionPostShow> collectionPostShows = new List<TraktSyncCollectionPostShow>();

      foreach (TraktCollectionShow traktSyncCollectionPostShow in collectionShows)
      {
        collectionPostShows.Add(new TraktSyncCollectionPostShow
        {
          Ids = traktSyncCollectionPostShow.Show.Ids
        });
      }

      // Act
      TraktSyncCollectionRemovePostResponse response = traktClient.RemoveCollectionItems(new TraktSyncCollectionPost { Shows = collectionPostShows });

      Assert.NotNull(response);
    }

    [Fact]
    public void CleanUpWatchedMovies()
    {
      // Arrange
      ILogger logger = Substitute.For<ILogger>();
      ITraktClient traktClient = new TraktClientProxy(ApplicationId, SecretId, logger);
      ValidateAuthorization(traktClient, new FileOperations());
      IEnumerable<TraktWatchedMovie> traktWatchedMovies = traktClient.GetWatchedMovies();
      IList<TraktSyncHistoryPostMovie> historyRemovePosts = new List<TraktSyncHistoryPostMovie>();

      foreach (TraktWatchedMovie traktSyncCollectionPostShow in traktWatchedMovies)
      {
        historyRemovePosts.Add(new TraktSyncHistoryPostMovie
        {
          Ids = traktSyncCollectionPostShow.Movie.Ids
        });
      }

      // Act
      TraktSyncHistoryRemovePostResponse response = traktClient.RemoveWatchedHistoryItems(new TraktSyncHistoryRemovePost {Movies = historyRemovePosts});

      Assert.NotNull(response);
    }

    [Fact]
    public void CleanUpWatchedShows()
    {
      // Arrange
      ILogger logger = Substitute.For<ILogger>();
      ITraktClient traktClient = new TraktClientProxy(ApplicationId, SecretId, logger);
      ValidateAuthorization(traktClient, new FileOperations());
      IEnumerable<TraktWatchedShow> traktWatchedMovies = traktClient.GetWatchedShows();
      IList<TraktSyncHistoryPostShow> historyPostShows = new List<TraktSyncHistoryPostShow>();

      foreach (TraktWatchedShow traktWatchedShow in traktWatchedMovies)
      {
        historyPostShows.Add(new TraktSyncHistoryPostShow
        {
         Ids = traktWatchedShow.Show.Ids
        });
      }

      // Act
      TraktSyncHistoryRemovePostResponse response = traktClient.RemoveWatchedHistoryItems(new TraktSyncHistoryRemovePost { Shows = historyPostShows });

      Assert.NotNull(response);
    }

    private void ValidateAuthorization(ITraktClient _traktClient, IFileOperations _fileOperations)
    {
      if (!_traktClient.TraktAuthorization.IsValid)
      {
        string authFilePath = Path.Combine(HomeUserPath, FileName.Authorization.Value);
        string savedAuthorization = _fileOperations.FileReadAllText(authFilePath);
        TraktAuthorization savedAuth = JsonConvert.DeserializeObject<TraktAuthorization>(savedAuthorization);

        if (!savedAuth.IsRefreshPossible)
        {
          throw new Exception("Saved authorization is not valid.");
        }

        TraktAuthorization refreshedAuth = _traktClient.RefreshAuthorization(savedAuth.RefreshToken);
        string serializedAuth = JsonConvert.SerializeObject(refreshedAuth);
        _fileOperations.FileWriteAllText(authFilePath, serializedAuth, Encoding.UTF8);
      }
    }
  }
}