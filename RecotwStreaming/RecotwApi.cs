using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RecotwStreaming
{
    public static class RecotwApi
    {
        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RecotwStreaming", "1.0"));
            return client;
        }

        private static async Task<T> Deserialize<T>(this HttpResponseMessage msg)
        {
            using (msg)
            using (var sr = new StreamReader(await msg.Content.ReadAsStreamAsync().ConfigureAwait(false)))
            using (var jr = new JsonTextReader(sr))
                return await Task.Run(() => JsonSerializer.CreateDefault().Deserialize<T>(jr));
        }

        public static async Task<IReadOnlyList<RecotwTweet>> GetTweetAllAsync()
        {
            using (var client = CreateClient())
                return await
                    (await client.GetAsync("http://recotw.tk/api/tweet/get_tweet_all").ConfigureAwait(false))
                    .Deserialize<List<RecotwTweet>>().ConfigureAwait(false);
        }

        public static async Task<RecotwTweet> RecordTweetAsync(long id)
        {
            using (var client = CreateClient())
                return await
                    (await client.PostAsync("http://recotw.tk/api/tweet/record_tweet",
                        new FormUrlEncodedContent(new Dictionary<string, string>()
                        {
                            { "id", id.ToString("D") }
                        })
                    )).Deserialize<RecotwTweet>();
        }
    }
}
