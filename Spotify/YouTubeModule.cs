using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Spotify
{
    public class YouTubeModule
    {
        internal async Task<YouTubeService> GoogleAuth()
        {
            UserCredential credential;
            //TODO change to relative path 
            using (var stream = new FileStream("/home/damiry/Documents/GitHub/Spotify_Playlist_Exporter/Spotify/client_secret_455955939328-trlhj2i7o9ihq8mqc6ulsoqqf06iha4b.apps.googleusercontent.com.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString()
            });
            return youtubeService;
            //  Console.WriteLine("Playlist item id {0} was added to playlist id {1}.", newPlaylistItem.Id, newPlaylist.Id);
        }

        internal async Task GeneratePlaylist(YouTubeService youTubeService)
        {
            var newPlaylist = new Playlist();
            newPlaylist.Snippet = new PlaylistSnippet()
            {
                Title = "Test playlist",
                Description = "lorem ipsum"
            };
            newPlaylist.Status = new PlaylistStatus() {PrivacyStatus = "public"};
            newPlaylist = await youTubeService.Playlists.Insert(newPlaylist, "snippet,status").ExecuteAsync();
            // TODO simplify
            var newPlaylistItem = new PlaylistItem();
            newPlaylistItem.Snippet = new PlaylistItemSnippet();
            newPlaylistItem.Snippet.PlaylistId = newPlaylist.Id;
            newPlaylistItem.Snippet.ResourceId = new ResourceId();
            newPlaylistItem.Snippet.ResourceId.Kind = "youtube#video";
            newPlaylistItem.Snippet.ResourceId.VideoId = "GNRMeaz6QRI";
            newPlaylistItem = await youTubeService.PlaylistItems.Insert(newPlaylistItem, "snippet").ExecuteAsync();
            Console.WriteLine("Playlist item id {0} was added to playlist id {1}.", newPlaylistItem.Id, newPlaylist.Id);


        }

    }
}