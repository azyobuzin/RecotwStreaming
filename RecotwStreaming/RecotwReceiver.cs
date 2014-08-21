using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreTweet;
using CoreTweet.Streaming;
using CoreTweet.Streaming.Reactive;
using Microsoft.AspNet.SignalR;

namespace RecotwStreaming
{
    public class RecotwReceiver : IDisposable
    {
        private static readonly List<RecotwReceiver> instances = new List<RecotwReceiver>();

        private static readonly Regex TwitterPattern =
            new Regex(@"^https?://(?:www\.)?twitter\.com/(?:#!/)?\w+/status(?:es)?/(\d+)/?(?:\?.*)?$", RegexOptions.IgnoreCase);

        private const string ConsumerKey = "uiYQy5R2RJFZRZ4zvSk7A";
        private const string ConsumerSecret = "qzDldacVrcyXbp8pBerf1LBfnQXmkPKmyLVGGLus8";

        private OAuth2Token appToken;
        private IDisposable streaming;
        private ConcurrentDictionary<long, RecotwTweet> tweets = new ConcurrentDictionary<long, RecotwTweet>();
        private Timer timer;

        private RecotwReceiver()
        {
            instances.Add(this);
            Initialize();
        }

        private async void Initialize()
        {
            this.appToken = await OAuth2.GetTokenAsync(ConsumerKey, ConsumerSecret).ConfigureAwait(false);
            foreach (var t in await RecotwApi.GetTweetAllAsync().ConfigureAwait(false))
                this.tweets.TryAdd(t.TweetId, t);

            var streamingObservable = Observable.Defer(() =>
                Tokens.Create(ConsumerKey, ConsumerSecret, "862962650-rIcjsj0j9ZJ8khPVA8jZTtEJuq7YYDBDpx6fOAgb", "kbMQjdVldI6tFOST3SVjmyAtG1D0oCkCpL6vBv1FtA") // @imgazyobuzi ReadOnly
                    .Streaming.StartObservableStream(StreamingType.Filter, new StreamingParameters(track => "@recotw"))
                    .OfType<StatusMessage>()
                    .Select(m => m.Status)
                    .Where(s => s.Entities != null && s.Entities.UserMentions != null && s.Entities.Urls != null
                        && s.Entities.UserMentions.Any(e => e.Id == 2726556865))
                    .SelectMany(s => s.Entities.Urls.Select(e => TwitterPattern.Match(e.ExpandedUrl.AbsoluteUri))
                        .Where(m => m.Success).Select(m => long.Parse(m.Groups[1].Value)))
            );
            this.streaming = streamingObservable
                .Catch(streamingObservable.DelaySubscription(TimeSpan.FromSeconds(10)).Retry())
                .Repeat()
                .Where(id => !this.tweets.ContainsKey(id))
                .Subscribe(async i =>
                {
                    try
                    {
                        var s = await this.appToken.Statuses.ShowAsync(id => i).ConfigureAwait(false);
                        var tweet = new RecotwTweet()
                        {
                            TweetId = s.Id,
                            Content = s.Text,
                            TargetId = s.User.Id.Value,
                            TargetSn = s.User.ScreenName
                        };
                        if (this.tweets.TryAdd(tweet.TweetId, tweet))
                            await this.PushAsync(tweet);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                });

            var timerSpan = TimeSpan.FromMinutes(1);
            this.timer = new Timer(async _ =>
            {
                foreach (var t in await RecotwApi.GetTweetAllAsync().ConfigureAwait(false))
                {
                    var exists = false;
                    this.tweets.AddOrUpdate(t.TweetId, t, (__, ___) =>
                    {
                        exists = true;
                        return t;
                    });

                    if (!exists)
                        await this.PushAsync(t);
                }
            }, null, timerSpan, timerSpan);
        }

        private Task PushAsync(RecotwTweet tweet)
        {
            return Task.Run(() =>
                GlobalHost.ConnectionManager.GetHubContext<MainHub>().Clients.All.NewTweet(tweet));
        }

        public static Task PushToAllInstanceAsync(RecotwTweet tweet)
        {
            return Task.WhenAll(
                instances.Where(o => o.tweets.TryAdd(tweet.TweetId, tweet))
                    .Select(o => o.PushAsync(tweet))
            );
        }

        public void Dispose()
        {
            instances.Remove(this);
            this.tweets = null;
            if (this.streaming != null)
                this.streaming.Dispose();
            this.streaming = null;
            if (this.timer != null)
                this.timer.Dispose();
            this.timer = null;

            GC.SuppressFinalize(this);
        }

        public static RecotwReceiver Start()
        {
            return new RecotwReceiver();
        }
    }
}
