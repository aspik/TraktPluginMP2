using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using NSubstitute;
using Tests.TestData.Setup;
using TraktNet.Objects.Authentication;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Movies;
using TraktNet.Objects.Get.Watched;
using TraktNet.Objects.Post.Syncs.Collection;
using TraktNet.Objects.Post.Syncs.Collection.Responses;
using TraktNet.Objects.Post.Syncs.History;
using TraktNet.Objects.Post.Syncs.History.Responses;
using TraktNet.Objects.Post.Syncs.Responses;
using TraktPluginMP2;
using TraktPluginMP2.Models;
using TraktPluginMP2.Services;
using TraktPluginMP2.Structures;
using Xunit;

namespace Tests
{
  public class TraktLibrarySyncTests
  {
    [Theory]
    [ClassData(typeof(WatchedMoviesTestData))]
    public void AddWatchedMovieToTraktIfMediaLibraryAvailable(List<MediaItem> databaseMovies, List<ITraktWatchedMovie> traktMovies, int? expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<ITraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<ITraktSyncHistoryPost>()).Returns(
        new TraktSyncHistoryPostResponse {Added = new TraktSyncPostResponseGroup {Movies = expectedMoviesCount}});
      ITraktAuthorization authorization = Substitute.For<ITraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.WatchedMovies.Returns(traktMovies);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncMoviesResult result = librarySynchronization.SyncMovies();

      // Assert
      Assert.Equal(expectedMoviesCount, result.AddedToTraktWatchedHistory);
    }

    [Theory]
    [ClassData(typeof(CollectedMoviesTestData))]
    public void AddCollectedMovieToTraktIfMediaLibraryAvailable(List<MediaItem> databaseMovies, List<ITraktCollectionMovie> traktMovies, int? expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<ITraktSyncCollectionPost>()).Returns(
        new TraktSyncCollectionPostResponse { Added = new TraktSyncPostResponseGroup { Movies = expectedMoviesCount } });
      traktClient.AddWatchedHistoryItems(Arg.Any<ITraktSyncHistoryPost>()).Returns(
        new TraktSyncHistoryPostResponse());
      ITraktAuthorization authorization = Substitute.For<ITraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.CollectedMovies.Returns(traktMovies);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncMoviesResult result = librarySynchronization.SyncMovies();

      // Assert
      Assert.Equal(expectedMoviesCount, result.AddedToTraktCollection);
    }

    [Theory]
    [ClassData(typeof(TraktUnwatchedMoviesTestData))]
    public void MarkMovieAsUnwatchedIfMediaLibraryAvailable(List<MediaItem> databaseMovies, List<ITraktMovie> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<ITraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<ITraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      ITraktAuthorization authorization = Substitute.For<ITraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.UnWatchedMovies.Returns(traktMovies);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncMoviesResult result = librarySynchronization.SyncMovies();

      // Assert
      Assert.Equal(expectedMoviesCount, result.MarkedAsUnWatchedInLibrary);
    }

    [Theory]
    [ClassData(typeof(TraktWatchedMoviesTestData))]
    public void MarkMovieAsWatchedIfMediaLibraryAndCacheAvailable(List<MediaItem> databaseMovies, List<ITraktWatchedMovie> traktMovies, int expectedMoviesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseMovies);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<ITraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<ITraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      ITraktAuthorization authorization = Substitute.For<ITraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.WatchedMovies.Returns(traktMovies);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncMoviesResult result = librarySynchronization.SyncMovies();

      // Assert
      Assert.Equal(expectedMoviesCount, result.MarkedAsWatchedInLibrary);
    }

    [Theory]
    [ClassData(typeof(CollectedEpisodesTestData))]
    public void AddCollectedEpisodeToTraktIfMediaLibraryAvailable(IList<MediaItem> databaseEpisodes, IList<EpisodeCollected> traktEpisodes, int? expectedEpisodesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<ITraktSyncCollectionPost>()).Returns(
        new TraktSyncCollectionPostResponse {Added = new TraktSyncPostResponseGroup {Episodes = expectedEpisodesCount} });
      traktClient.AddWatchedHistoryItems(Arg.Any<ITraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      ITraktAuthorization authorization = Substitute.For<ITraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.CollectedEpisodes.Returns(traktEpisodes);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncEpisodesResult result = librarySynchronization.SyncSeries();

      // Assert
      Assert.Equal(expectedEpisodesCount, result.AddedToTraktCollection);
    }

    [Theory]
    [ClassData(typeof(WatchedEpisodesTestData))]
    public void AddWatchedEpisodeToTraktIfMediaLibraryAvailable(IList<MediaItem> databaseEpisodes, IList<EpisodeWatched> traktEpisodes, int? expectedEpisodesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<ITraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<ITraktSyncHistoryPost>()).Returns(
        new TraktSyncHistoryPostResponse { Added = new TraktSyncPostResponseGroup { Episodes = expectedEpisodesCount } });
      ITraktAuthorization authorization = Substitute.For<ITraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.WatchedEpisodes.Returns(traktEpisodes);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncEpisodesResult result = librarySynchronization.SyncSeries();

      // Assert
      Assert.Equal(expectedEpisodesCount, result.AddedToTraktWatchedHistory);
    }

    [Theory]
    [ClassData(typeof(TraktUnWatchedEpisodesTestData))]
    public void MarkEpisodeAsUnwatchedIfMediaLibraryAvailable(List<MediaItem> databaseEpisodes, List<Episode> traktEpisodes, int expectedEpisodessCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<ITraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<ITraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      ITraktAuthorization authorization = Substitute.For<ITraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.UnWatchedEpisodes.Returns(traktEpisodes);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncEpisodesResult result = librarySynchronization.SyncSeries();

      // Assert
      Assert.Equal(expectedEpisodessCount, result.MarkedAsUnWatchedInLibrary);
    }

    [Theory]
    [ClassData(typeof(TraktWatchedEpisodesTestData))]
    public void MarkEpisodeAsWatchedIfMediaLibraryAvailable(List<MediaItem> databaseEpisodes, List<EpisodeWatched> traktEpisodes, int expectedEpisodesCount)
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = GetMockMediaPortalServices(databaseEpisodes);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.AddCollectionItems(Arg.Any<ITraktSyncCollectionPost>()).Returns(new TraktSyncCollectionPostResponse());
      traktClient.AddWatchedHistoryItems(Arg.Any<ITraktSyncHistoryPost>()).Returns(new TraktSyncHistoryPostResponse());
      ITraktAuthorization authorization = Substitute.For<ITraktAuthorization>();
      authorization.AccessToken = "ValidToken";
      traktClient.TraktAuthorization.Returns(authorization);
      ITraktCache traktCache = Substitute.For<ITraktCache>();
      traktCache.WatchedEpisodes.Returns(traktEpisodes);
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      // Act
      TraktSyncEpisodesResult result = librarySynchronization.SyncSeries();

      // Assert
      Assert.Equal(expectedEpisodesCount, result.MarkedAsWatchedInLibrary);
    }

    private IMediaPortalServices GetMockMediaPortalServices(IList<MediaItem> databaseMediaItems)
    {
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.MarkAsWatched(Arg.Any<MediaItem>()).Returns(true);
      mediaPortalServices.MarkAsUnWatched(Arg.Any<MediaItem>()).Returns(true);
      
      IContentDirectory contentDirectory = Substitute.For<IContentDirectory>();
      contentDirectory.SearchAsync(Arg.Any<MediaItemQuery>(), true, null, false).Returns(databaseMediaItems);
      mediaPortalServices.GetServerConnectionManager().ContentDirectory.Returns(contentDirectory);

      return mediaPortalServices;
    }
  }
}