using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataStax.Driver.Benchmarks.Models
{
    public class Video
    {
        [JsonProperty(PropertyName = "videoid")]
        public Guid VideoId { get; set; }

        [JsonProperty(PropertyName = "userid")]
        public Guid UserId { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "location_type")]
        public int LocationType { get; set; }

        [JsonProperty(PropertyName = "preview_thumbnails")]
        public SortedDictionary<string, string> PreviewThumbnails { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public string[] Tags { get; set; }

        [JsonProperty(PropertyName = "added_date")]
        public DateTimeOffset AddedDate { get; set; }
    }
}
