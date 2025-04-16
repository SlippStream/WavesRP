using System.Buffers.Text;
using System.Collections;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TIDAL;

namespace WavesRP
{
    public struct Artist(string name, string id)
    {
        public string Name = name;
        public string Id = id;
    }
    public struct Album(string name, string coverUrl)
    {
        public string Name = name;
        public string CoverUrl = coverUrl;
    }
    public class ListeningData(string songName, TimeSpan duration, Album album, IEnumerable<Artist> artists)
    {
        public Artist[] Artists = [.. artists];
        public string SongName = songName;
        public string TrackUrl = string.Empty;
        public Album Album = album;
        public TimeSpan Duration = duration;
        public DateTime Started = DateTime.Now;

        public ListeningData() : this(string.Empty, TimeSpan.Zero, new Album(), Array.Empty<Artist>())
        {
        }
    }
    public class TidalHttpService
    {
        private readonly HttpClient _client;
        private string _accessToken = string.Empty;
        public bool IsAuthorized => _accessToken != string.Empty;

        public TidalHttpService(string clientId, string clientSecret)
        {
            HttpClientHandler handler = new()
            {
                AutomaticDecompression = DecompressionMethods.All
            };

            _client = new HttpClient(handler);

            _ = KeepAuthed(clientId, clientSecret);
        }
        private class AlbumPopularityComparer : Comparer<Albums_Resource>
        {
            public override int Compare(Albums_Resource x, Albums_Resource y)
            {
                return x.attributes.popularity.CompareTo(y.attributes.popularity);
            }
        }
        private class TrackPopularityComparer : Comparer<Tracks_Resource>
        {
            public override int Compare(Tracks_Resource x, Tracks_Resource y)
            {
                return x.attributes.popularity.CompareTo(y.attributes.popularity);
            }
        }
        public async Task<ListeningData> SearchFromWindowTitle(string title, string delim)
        {
            if (!IsAuthorized) return default!;
            ListeningData data = new();
            Headers headers = new(
                    ("Authorization", $"Bearer {_accessToken}"),
                    ("Accept", "application/vnd.api+json")
                );


            TIDAL.SearchResultResponse res = await GetAsync<TIDAL.SearchResultResponse>(
                $"https://openapi.tidal.com/v2/searchResults/{Uri.EscapeDataString(title)}/relationships/tracks?countryCode=US&include=tracks"
                , headers
            );
            if (EqualityComparer<TIDAL.SearchResultResponse>.Default.Equals(res, default))
            {
                Console.WriteLine("Error: Search failed! Check your credentials.");
                return default!;
            }

            var track = res.included.Where(t => t.attributes.title.StartsWith(GetNaiveSongName(title, delim))).OrderDescending(new TrackPopularityComparer()).First();
            Console.WriteLine(track.relationships.artists.data);
            var trackDoc = await GetAsync<Tracks_Single_Data_Document>(
                $"https://openapi.tidal.com/v2/tracks/{track.id}?countryCode=US&include=albums,artists"
                , headers
            );
            var artistsDoc = await GetAsync<Artists_Multi_Data_Document>(
                $"https://openapi.tidal.com/v2/artists?countryCode=US&{String.Join('&', trackDoc.data.Value.relationships.artists.data.Select(x => $"filter[id]={x.id}"))}"
                , headers
            );
            var albumDoc = await GetAsync<Albums_Multi_Data_Document>(
                $"https://openapi.tidal.com/v2/albums?countryCode=US&include=coverArt&{String.Join("&", trackDoc.data.Value.relationships.albums.data.Select(x => $"filter[id]={x.id}"))}&include=artists"
                , headers
            );
            var tempAlbum = albumDoc.data.OrderDescending(new AlbumPopularityComparer()).First();
            var albumArt = await GetAsync<Artworks_Single_Data_Document>(
                $"https://openapi.tidal.com/v2/artworks/{tempAlbum.relationships.coverArt.data.First().id}?countryCode=US"
                , headers
            );

            data.Artists = [.. artistsDoc.data.Select(x => new Artist(x.attributes.Value.name, x.id))];

            data.Album = albumArt.data.HasValue ? new Album(tempAlbum.attributes.title, albumArt.data.Value.attributes.files.First().href) : new Album(tempAlbum.attributes.title, "waves");
            data.Duration = XmlConvert.ToTimeSpan(track.attributes.duration);
            data.SongName = trackDoc.data.Value.attributes.title;
            data.TrackUrl = $"https://tidal.com/browse/track/{trackDoc.data.Value.id}?u";
            return data;
        }
        private static string GetNaiveSongName(string title, string delim)
        {
            string[] parts = title.Split(delim, 2);
            if (parts.Length == 2)
            {
                return parts[0].Trim();
            }
            return title.Trim();
        }
        public async Task<string> GetAsync(string uri, Headers headers)
        {
            //TODO use GetAsyncFromJson to deserialize the response
            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri),
            };
            foreach (var header in headers)
            {
                requestMessage.Headers.Add(header.Item1, header.Item2);
            }
            using HttpResponseMessage response = await _client.GetAsync(uri);

            return await response.Content.ReadAsStringAsync();
        }
        public async Task<T> GetAsync<T>(string uri, Headers headers)
        {
            //TODO use GetAsyncFromJson to deserialize the response
            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri)
            };
            foreach (var header in headers)
            {
                requestMessage.Headers.Add(header.Item1, header.Item2);
            }
            using HttpResponseMessage response = await _client.SendAsync(requestMessage);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return default!;
            }
            var jsonstring = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonstring);
        }
        public async Task<string> PostAsync(string uri, string data, Headers headers, string? contentType = null)
        {
            using HttpContent content = new StringContent(data, Encoding.UTF8, contentType);

            HttpRequestMessage requestMessage = new()
            {
                Content = content,
                Method = HttpMethod.Post,
                RequestUri = new Uri(uri),
            };
            foreach (var header in headers)
            {
                requestMessage.Headers.Add(header.Item1, header.Item2);
            }
            using HttpResponseMessage response = await _client.SendAsync(requestMessage);

            return await response.Content.ReadAsStringAsync();
        }
        public async Task<T> PostAsync<T>(string uri, string data, Headers headers, string? contentType = null)
        {
            using HttpContent content = new StringContent(data, Encoding.UTF8, contentType);

            HttpRequestMessage requestMessage = new()
            {
                Content = content,
                Method = HttpMethod.Post,
                RequestUri = new Uri(uri),
            };
            foreach (var header in headers)
            {
                requestMessage.Headers.Add(header.Item1, header.Item2);
            }
            using HttpResponseMessage response = await _client.SendAsync(requestMessage);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return default!;
            }
            return await response.Content.ReadFromJsonAsync<T>();
        }
        public async Task Authorize(string clientId, string clientSecret)
        {
            var res = await PostAsync<TIDAL.ClientCredentialsResponse>("https://auth.tidal.com/v1/oauth2/token?", "grant_type=client_credentials"
                , new Headers(
                    ("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"))}"),
                    ("Accept", "application/json")
                    )
                , "application/x-www-form-urlencoded");
            if (EqualityComparer<TIDAL.ClientCredentialsResponse>.Default.Equals(res, default))
            {
                Console.WriteLine("Error: Authorization failed! Check your credentials.");
                goto FAIL_AUTH;
            }
            Console.WriteLine($"TIDAL Authorization successful! Token expires in {res.expires_in} seconds.");
            _accessToken = res.access_token;

            await Task.Delay(res.expires_in * 1000 - 5000); // wait for the token to expire
        FAIL_AUTH:
            _accessToken = string.Empty;
        }
        private async Task KeepAuthed(string clientId, string clientSecret)
        {
            while (true)
            {
                await Authorize(clientId, clientSecret);
            }
        }
        public class Headers : IEnumerable<(string, string?)>
        {
            private List<(string, string?)> list = new();
            public Headers(params (string, string)[] values)
            {
                foreach (var value in values)
                {
                    list.Add(value);
                }
            }
            public IEnumerator<(string, string?)> GetEnumerator()
            {
                return list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return list.GetEnumerator();
            }
        }
    }
}