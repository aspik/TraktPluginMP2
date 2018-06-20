using System;

namespace TraktPluginMP2.Exceptions
{
  public class MediaLibraryNotConnectedException : Exception
  {
    public MediaLibraryNotConnectedException(string message) : base(message) 
    {
      
    }
  }
}