using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using Newtonsoft.Json;

namespace DataStax.Driver.Benchmarks.Models
{
    public class VideoEvent
    {
        [JsonProperty(PropertyName = "videoid")]
        public Guid VideoId { get; set; }

        [JsonProperty(PropertyName = "userid")]
        public Guid UserId { get; set; }

        [JsonProperty(PropertyName = "event")]
        public string Event { get; set; }

        [JsonProperty(PropertyName = "event_timestamp")]
        public Guid EventTimestamp { get; set; }

        [JsonProperty(PropertyName = "video_timestamp")]
        public long VideoTimestamp { get; set; }
    }
}
