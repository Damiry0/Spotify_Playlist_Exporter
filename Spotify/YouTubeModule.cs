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
        }

        internal async Task GeneratePlaylist(YouTubeService youTubeService,List<string> list)
        {
            var newPlaylist = new Playlist();
            newPlaylist.Snippet = new PlaylistSnippet()
            {
                Title = "Exported Playlist",
                Description = "Created by:"
            };
            newPlaylist.Status = new PlaylistStatus() {PrivacyStatus = "public"};
            newPlaylist = await youTubeService.Playlists.Insert(newPlaylist, "snippet,status").ExecuteAsync();
            
            // simplify and optimize for quota limits
            var searchListRequest = youTubeService.Search.List("snippet");
            
            var videos = new List<string>(); 
        
            foreach (var item in list)
            {
                searchListRequest.Q = item;
                searchListRequest.MaxResults = 1;
                var searchListResponse = await searchListRequest.ExecuteAsync();
                if (searchListResponse.Items[0].Id.Kind != "youtube#video") continue;
                var playlistItem = new PlaylistItem();
                playlistItem.Snippet = new PlaylistItemSnippet()
                {
                    PlaylistId = newPlaylist.Id,
                    ResourceId = new ResourceId()
                    {
                        Kind = "youtube#video",
                        VideoId = searchListResponse.Items[0].Id.VideoId,
                    }
                };
                playlistItem = await youTubeService.PlaylistItems.Insert(playlistItem, "snippet").ExecuteAsync();
            }
            Environment.Exit(0);
            
        }

    }
}