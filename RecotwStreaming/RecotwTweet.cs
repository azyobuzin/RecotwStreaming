using System;
using Newtonsoft.Json;

namespace RecotwStreaming
{
    public class RecotwTweet
    {
        [JsonProperty("id")]
        public uint? Id { get; set; }

        [JsonProperty("tweet_id")]
        public long TweetId { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("target_id")]
        public long TargetId { get; set; }

        [JsonProperty("target_sn")]
        public string TargetSn { get; set; }

        [JsonProperty("record_date")]
        private string recordDate;

        private static readonly TimeSpan Jst = TimeSpan.FromHours(9);

        public DateTimeOffset? RecordDate
        {
            get
            {
                return this.recordDate != null
                    ? new Nullable<DateTimeOffset>(new DateTimeOffset(DateTime.Parse(this.recordDate), Jst))
                    : null;
            }
            set
            {
                this.recordDate = value.HasValue
                    ? value.Value.ToOffset(Jst).ToString("yyyy-MM-dd HH:mm:ss")
                    : null;
            }
        }
    }
}
