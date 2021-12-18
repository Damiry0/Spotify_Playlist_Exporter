using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static IEnumerable<(T item, int index)> WithIndex<T>(IEnumerable<T> self)
        {
            return self?.Select((item, index) => (item, index)) ?? new List<(T, int)>();
        }
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

        internal async Task GeneratePlaylist(YouTubeService youTubeService,List<string> list)
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
            var searchListRequest = youTubeService.Search.List("snippet");
            
            List<string> videos = new List<string>();
        
            for (int i = 0; i<list.Count; i++)
            {
                searchListRequest.Q = list[i];
                searchListRequest.MaxResults = 5; 
                 // Call the search.list method to retrieve results matching the specified query term.
               var searchListResponse = await searchListRequest.ExecuteAsync();
               if (searchListResponse.Items[0].Id.Kind == "youtube#video")
               {
                   var Item = new PlaylistItem();
                   Item.Snippet = new PlaylistItemSnippet();
                   Item.Snippet.PlaylistId = newPlaylist.Id;
                   Item.Snippet.ResourceId = new ResourceId();
                   Item.Snippet.ResourceId.Kind = "youtube#video";
                   Item.Snippet.ResourceId.VideoId = searchListResponse.Items[0].Id.VideoId;
                   //Item.Id = searchListResponse.Items[0].Id.VideoId;
                   Item = await youTubeService.PlaylistItems.Insert(Item, "snippet").ExecuteAsync();

               }
            }
            Environment.Exit(0);
            
        }

    }
}