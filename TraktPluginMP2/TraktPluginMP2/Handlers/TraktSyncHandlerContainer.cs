using TraktPluginMP2.Services;

namespace TraktPluginMP2.Handlers
{
  internal static class TraktSyncHandlerContainer
  {
    const string ApplicationId = "aea41e88de3cd0f8c8b2404d84d2e5d7317789e67fad223eba107aea2ef59068";
    const string SecretId = "adafedb5cd065e6abeb9521b8b64bc66adb010a7c08128811bf32c989f35b77a";

    internal static TraktSyncHandlerManager ResolveManager()
    {
      IMediaPortalServices mediaPortalServices = new MediaPortalServices();
      IFileOperations fileOperations = new FileOperations();
      ITraktClient traktClient = new TraktClientProxy(ApplicationId, SecretId, mediaPortalServices.GetLogger());
      ITraktCache traktCache = new TraktCache(mediaPortalServices, traktClient, fileOperations);
      ILibrarySynchronization librarySynchronization = new LibrarySynchronization(mediaPortalServices, traktClient, traktCache, fileOperations);

      return new TraktSyncHandlerManager(mediaPortalServices, librarySynchronization, fileOperations);
    }
  }
}