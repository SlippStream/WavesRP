using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace WavesRP
{
    class ListeningData
    {
        //public Artist Artist { get; set; } = new Artist();
        public static ListeningData FromWindowTitle(string title, string delim)
        {
            ListeningData data = new();
            string[] parts = title.Split(delim);
            if (parts.Length < 2) throw new ArgumentException("Invalid title format", nameof(title));
            if (parts.Length > 2)
            {
                //TODO if an artist or song contains the delimiter, this will break
            }
            //data.Artist.Name = parts[0];
            //data.Song.Name = parts[1];
            return data;
        }
    }
    public class TidalHttpService
    {
        private readonly HttpClient _client;

        public TidalHttpService()
        {
            HttpClientHandler handler = new()
            {
                AutomaticDecompression = DecompressionMethods.All
            };

            _client = new HttpClient(handler);
        }

        public async Task<string> GetAsync(string uri)
        {
            //TODO use GetAsyncFromJson to deserialize the response
            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri),
            };
            requestMessage.Headers.Add("accept", "application/vnd.api+json");
            requestMessage.Headers.Add("Authorization", $"Bearer {}");
            using HttpResponseMessage response = await _client.GetAsync(uri);

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> PostAsync(string uri, string data, string contentType)
        {
            using HttpContent content = new StringContent(data, Encoding.UTF8, contentType);

            HttpRequestMessage requestMessage = new()
            {
                Content = content,
                Method = HttpMethod.Post,
                RequestUri = new Uri(uri),
            };
            using HttpResponseMessage response = await _client.SendAsync(requestMessage);

            return await response.Content.ReadAsStringAsync();
        }
    }
}