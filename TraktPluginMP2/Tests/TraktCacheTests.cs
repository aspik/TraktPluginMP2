using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NSubstitute;
using Tests.TestData.Cache;
using TraktNet.Objects.Get.Collections;
using TraktNet.Objects.Get.Syncs.Activities;
using TraktNet.Objects.Get.Watched;
using TraktPluginMP2;
using TraktPluginMP2.Services;
using Xunit;

namespace Tests
{
  public class TraktCacheTests
  {
    private const string DataPath = @"C:\FakeTraktUserHomePath\";

    [Theory]
    [ClassData(typeof(UnWatchedMoviesTestData))]
    public void GetUnWatchedMovies(List<ITraktWatchedMovie> onlineWatchedMovies, ITraktSyncLastActivities onlineLastSyncActivities, int expectedUnWatchedMoviesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetWatchedMovies().Returns(onlineWatchedMovies);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      SetFileOperationsForFile(fileOperations, DataPath, FileName.LastActivity.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.WatchedMovies.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.CollectedMovies.Value);

      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);

      ITraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      traktCache.RefreshMoviesCache();

      // Assert
      int actualUnWatchedMoviesCount = traktCache.UnWatchedMovies.Count();
      Assert.Equal(expectedUnWatchedMoviesCount, actualUnWatchedMoviesCount);
    }

    [Theory]
    [ClassData(typeof(WatchedMoviesTestData))]
    public void GetWatchedMovies(List<ITraktWatchedMovie> onlineWatchedMovies, ITraktSyncLastActivities onlineLastSyncActivities, int expectedWatchedMoviesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetWatchedMovies().Returns(onlineWatchedMovies);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      SetFileOperationsForFile(fileOperations, DataPath, FileName.LastActivity.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.WatchedMovies.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.CollectedMovies.Value);

      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);

      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      traktCache.RefreshMoviesCache();

      // Assert
      int actualWatchedMoviesCount = traktCache.WatchedMovies.Count();
      Assert.Equal(expectedWatchedMoviesCount, actualWatchedMoviesCount);
    }

    [Theory]
    [ClassData(typeof(CollectedMoviesTestData))]
    public void GetCollectedMovies(List<ITraktCollectionMovie> onlineCollectedMovies, ITraktSyncLastActivities onlineLastSyncActivities, int expectedCollectedMoviesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetCollectedMovies().Returns(onlineCollectedMovies);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      SetFileOperationsForFile(fileOperations, DataPath, FileName.LastActivity.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.CollectedMovies.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.WatchedMovies.Value);

      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);

      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      traktCache.RefreshMoviesCache();

      // Assert
      int actualCollectedMoviesCount = traktCache.CollectedMovies.Count();
      Assert.Equal(expectedCollectedMoviesCount, actualCollectedMoviesCount);
    }

    [Theory]
    [ClassData(typeof(UnWatchedEpisodesTestData))]
    public void GetUnWatchedEpisodes(List<ITraktWatchedShow> onlineWatchedShows, ITraktSyncLastActivities onlineLastSyncActivities, int expectedUnWatchedEpisodesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetWatchedShows().Returns(onlineWatchedShows);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      SetFileOperationsForFile(fileOperations, DataPath, FileName.LastActivity.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.WatchedEpisodes.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.CollectedEpisodes.Value);

      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);

      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      traktCache.RefreshSeriesCache();

      // Assert
      int actualUnWatchedEpisodesCount = traktCache.UnWatchedEpisodes.Count();
      Assert.Equal(expectedUnWatchedEpisodesCount, actualUnWatchedEpisodesCount);
    }

    [Theory]
    [ClassData(typeof(WatchedEpisodesTestData))]
    public void GetWatchedEpisodes(List<ITraktWatchedShow> onlineWatchedShows, ITraktSyncLastActivities onlineLastSyncActivities, int expectedWatchedEpisodesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetWatchedShows().Returns(onlineWatchedShows);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      SetFileOperationsForFile(fileOperations, DataPath, FileName.LastActivity.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.WatchedEpisodes.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.CollectedEpisodes.Value);

      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);

      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);

      // Act
      traktCache.RefreshSeriesCache();

      // Assert
      int actualWatchedEpisodesCount = traktCache.WatchedEpisodes.Count();
      Assert.Equal(expectedWatchedEpisodesCount, actualWatchedEpisodesCount);
    }

    [Theory]
    [ClassData(typeof(CollectedEpisodesTestData))]
    public void GetCollectedEpisodes(List<ITraktCollectionShow> onlineCollectedShows, ITraktSyncLastActivities onlineLastSyncActivities, int expectedCollectedEpisodesCount)
    {
      // Arrange
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetCollectedShows().Returns(onlineCollectedShows);
      traktClient.GetLastActivities().Returns(onlineLastSyncActivities);

      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      SetFileOperationsForFile(fileOperations, DataPath, FileName.LastActivity.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.CollectedEpisodes.Value);
      SetFileOperationsForFile(fileOperations, DataPath, FileName.WatchedEpisodes.Value);

      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);

      TraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);
      
      // Act
      traktCache.RefreshSeriesCache();

      // Assert
      int actualCollectedEpisodesCount = traktCache.CollectedEpisodes.Count();
      Assert.Equal(expectedCollectedEpisodesCount, actualCollectedEpisodesCount);
    }

    private void SetFileOperationsForFile(IFileOperations fileOperations, string path, string fileName)
    {
      fileOperations.FileExists(Arg.Is<string>(x => x.Equals(Path.Combine(path, fileName))))
        .Returns(true);
      fileOperations.FileReadAllText(Arg.Is<string>(x => x.Equals(Path.Combine(path, fileName))))
        .Returns(File.ReadAllText(TestUtility.GetTestDataPath(Path.Combine(@"Cache\", fileName)), Encoding.UTF8));
    }
  }
}