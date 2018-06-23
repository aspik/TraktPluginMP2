using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TraktApiSharp.Exceptions;
using TraktPluginMP2;
using TraktPluginMP2.Models;
using TraktPluginMP2.Services;
using Xunit;

namespace Tests
{
  public class TraktSetupModelTests
  {
    private const string DataPath = @"C:\FakeTraktUserHomePath\";

    [Fact]
    public void ShouldAuthorizeUserForValidData()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      ILibrarySynchronization librarySynchronization = Substitute.For<ILibrarySynchronization>();
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.DirectoryExists(DataPath).Returns(true);
      string expectedStatus = "[Trakt.AuthorizationSucceed]";

      TraktSetupModel setupModel = new TraktSetupModel(mediaPortalServices, traktClient, librarySynchronization, fileOperations);
      
      // Act
      setupModel.AuthorizeUser();
      string actualStatus = setupModel.TestStatus;

      // Assert
      Assert.Equal(expectedStatus, actualStatus);
      Assert.True(setupModel.IsUserAuthorized);
    }

    [Fact]
    public void ShouldFailToAuthorizeUserWhenTraktAuthorizationThrowsException()
    {
      // Arrange
      IMediaPortalServices mediaPortalServices = Substitute.For<IMediaPortalServices>();
      mediaPortalServices.GetTraktUserHomePath().Returns(DataPath);
      ITraktClient traktClient = Substitute.For<ITraktClient>();
      traktClient.GetAuthorization(Arg.Any<string>()).Throws(new TraktException("Pin code is not valid"));
      ILibrarySynchronization librarySynchronization = Substitute.For<ILibrarySynchronization>();
      IFileOperations fileOperations = Substitute.For<IFileOperations>();
      fileOperations.DirectoryExists(DataPath).Returns(true);
      string expectedStatus = "[Trakt.AuthorizationFailed]";

      TraktSetupModel setupModel = new TraktSetupModel(mediaPortalServices, traktClient, librarySynchronization, fileOperations);

      // Act
      setupModel.AuthorizeUser();
      string actualStatus = setupModel.TestStatus;

      // Assert
      Assert.Equal(expectedStatus, actualStatus);
      Assert.False(setupModel.IsUserAuthorized);
    }
  }
}