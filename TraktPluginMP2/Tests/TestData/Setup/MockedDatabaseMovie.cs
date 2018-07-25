using System;
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.UserProfileDataManagement;
using TraktPluginMP2.Structures;

namespace Tests.TestData.Setup
{
  public class MockedDatabaseMovie
  {
    public MediaItem Movie { get; }

    public MockedDatabaseMovie(string imdbId, string tmdbId, string title, int year, int playPercentage)
    {
      IDictionary<Guid, IList<MediaItemAspect>> movieAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MultipleMediaItemAspect resourceAspect = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\" + title + ".mkv");
      MediaItemAspect.AddOrUpdateAspect(movieAspects, resourceAspect);
      MediaItemAspect.AddOrUpdateExternalIdentifier(movieAspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, imdbId);
      MediaItemAspect.AddOrUpdateExternalIdentifier(movieAspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, tmdbId);
      MediaItemAspect.SetAttribute(movieAspects, MovieAspect.ATTR_MOVIE_NAME, title);
      SingleMediaItemAspect smia = new SingleMediaItemAspect(MediaAspect.Metadata);
      smia.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 1, 1));
      MediaItemAspect.SetAspect(movieAspects, smia);
      IDictionary<string, string> userData = new Dictionary<string, string>();
      userData.Add(UserDataKeysKnown.KEY_PLAY_PERCENTAGE, playPercentage.ToString());
      Movie = new MediaItem(Guid.NewGuid(), movieAspects, userData);
    }

    public MockedDatabaseMovie(MediaLibraryMovie movie)
    {
      IDictionary<Guid, IList<MediaItemAspect>> movieAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MultipleMediaItemAspect resourceAspect = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\" + movie.Title + ".mkv");
      MediaItemAspect.AddOrUpdateAspect(movieAspects, resourceAspect);
      MediaItemAspect.AddOrUpdateExternalIdentifier(movieAspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, movie.Imdb);
      MediaItemAspect.SetAttribute(movieAspects, MovieAspect.ATTR_MOVIE_NAME, movie.Title);

      SingleMediaItemAspect mediaItemAspect = new SingleMediaItemAspect(MediaAspect.Metadata);
      mediaItemAspect.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, movie.PlayCount);
      mediaItemAspect.SetAttribute(MediaAspect.ATTR_LASTPLAYED, DateTime.Parse(movie.LastPlayed));
      MediaItemAspect.SetAspect(movieAspects, mediaItemAspect);

      SingleMediaItemAspect importerAspect = new SingleMediaItemAspect(ImporterAspect.Metadata);
      importerAspect.SetAttribute(ImporterAspect.ATTR_DATEADDED, DateTime.Parse(movie.AddedToDb));
      MediaItemAspect.SetAspect(movieAspects, importerAspect);

      Movie = new MediaItem(Guid.NewGuid(), movieAspects);
    }
  }
}