using System.IO;
using System.Threading.Tasks;
using System;
using static Spotify.SpotifyModule;
using static Spotify.YouTubeModule;

namespace Spotify
{
  public static class Program
  {
    
    private static void Exiting() => Console.CursorVisible = true;
    public static async Task<int> Main()
    {
      AppDomain.CurrentDomain.ProcessExit += (sender, e) => Exiting();
      
      if (File.Exists(CredentialsPath))
      {
        var based = new YouTubeModule();
        await based.GeneratePlaylist(await based.GoogleAuth());
        //  await Start();
      }
      else
      {
      //  await StartAuthentication();
      }

      Console.ReadKey();
      return 0;
    }

  }
}