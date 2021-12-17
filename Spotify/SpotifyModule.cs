using System.IO;
using System.Threading.Tasks;
using System;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using static SpotifyAPI.Web.Scopes;

namespace Spotify
{
    public static class SpotifyModule
    {
        internal const string CredentialsPath = "credentials.json";
        private static readonly string? clientId = "0dfe25d5acc5413ab2376db48064fb41";
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
        private static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
        {
            return self?.Select((item, index) => (item, index)) ?? new List<(T, int)>();
        }
        private static void Exiting() => Console.CursorVisible = true;

        internal static async Task<List<string>> Start()
        {
            var json = await File.ReadAllTextAsync(CredentialsPath);
            var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

            var authenticator = new PKCEAuthenticator(clientId!, token!);
            authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(token));

            var config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(authenticator);

            var spotify = new SpotifyClient(config);

            var me = await spotify.UserProfile.Current();
      
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine($"Welcome {me.DisplayName} ({me.Id}), you're authenticated!");
            var playlists = await spotify.PaginateAll(await spotify.Playlists.CurrentUsers().ConfigureAwait(false));
            Console.WriteLine("Select your desired playlist:");
            foreach (var (playlist,index) in playlists.WithIndex())
            {
                Console.WriteLine($"{index}:{playlist.Name}");
            }
            Console.WriteLine("Playlist number:");
            var selectedPlaylistNumber = Convert.ToInt32(Console.ReadLine());
            // if (playlistRequested.Tracks != null)
            var fullPlaylist = await spotify.PaginateAll(await spotify.Playlists.GetItems(playlists[selectedPlaylistNumber].Id).ConfigureAwait(false));
            var list = fullPlaylist.Select(line => ((FullTrack) line.Track).Name).ToList();
            _server.Dispose();
            Environment.Exit(0);
            return list;
        }


        internal static async Task StartAuthentication()
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