using System.IO;
using System.Threading.Tasks;
using System;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System.Collections.Generic;
using Newtonsoft.Json;
using static SpotifyAPI.Web.Scopes;
using Swan.Logging;

namespace Example.CLI.PersistentConfig
{
  public class Program
  {
    private const string CredentialsPath = "credentials.json";
    private static readonly string? clientId = "0dfe25d5acc5413ab2376db48064fb41";
    private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);

    private static void Exiting() => Console.CursorVisible = true;
    public static async Task<int> Main()
    {
      AppDomain.CurrentDomain.ProcessExit += (sender, e) => Exiting();

      

      if (File.Exists(CredentialsPath))
      {
        await Start();
      }
      else
      {
        await StartAuthentication();
      }

      Console.ReadKey();
      return 0;
    }

    private static async Task Start()
    {
      var json = await File.ReadAllTextAsync(CredentialsPath);
      var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

      var authenticator = new PKCEAuthenticator(clientId!, token!);
      authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(token));

      var config = SpotifyClientConfig.CreateDefault()
        .WithAuthenticator(authenticator);

      var spotify = new SpotifyClient(config);

      var me = await spotify.UserProfile.Current();
      
      
      Console.WriteLine($"Welcome {me.DisplayName} ({me.Id}), you're authenticated!");
      var recommendations = spotify.Browse.GetRecommendations(new RecommendationsRequest()).Result.Tracks;
      foreach (var track in recommendations)
      {
        Console.WriteLine("Rec:{0}",track);
      }
     // var playlists = await spotify.PaginateAll(await spotify.Playlists.CurrentUsers().ConfigureAwait(false));
      
     // Console.WriteLine($"Total Playlists in your Account: {playlists.Count}");

      _server.Dispose();
      Environment.Exit(0);
    }

    private static async Task StartAuthentication()
    {
      var (verifier, challenge) = PKCEUtil.GenerateCodes();

      await _server.Start();
      _server.AuthorizationCodeReceived += async (sender, response) =>
      {
        await _server.Stop();
        PKCETokenResponse token = await new OAuthClient().RequestToken(
          new PKCETokenRequest(clientId!, response.Code, _server.BaseUri, verifier)
        );

        await File.WriteAllTextAsync(CredentialsPath, JsonConvert.SerializeObject(token));
        await Start();
      };

      var request = new LoginRequest(_server.BaseUri, clientId!, LoginRequest.ResponseType.Code)
      {
        CodeChallenge = challenge,
        CodeChallengeMethod = "S256",
        Scope = new List<string> { UserReadEmail, UserReadPrivate, PlaylistReadPrivate, PlaylistReadCollaborative }
      };

      Uri uri = request.ToUri();
      try
      {
        BrowserUtil.Open(uri);
      }
      catch (Exception)
      {
        Console.WriteLine("Unable to open URL, manually open: {0}", uri);
      }
    }
  }
}