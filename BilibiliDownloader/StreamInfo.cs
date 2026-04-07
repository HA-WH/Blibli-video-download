using System.Text.Json.Serialization;

namespace BilibiliDownloader
{
    public class StreamInfo
    {
        public int Id { get; set; }
        public long Bandwidth { get; set; }
        [JsonPropertyName("bandwidth_text")]
        public string BandwidthText { get; set; } = "";
        public string Url { get; set; } = "";

        [JsonIgnore]
        public virtual string DisplayText => $"音频：{BandwidthText}";
    }

    public class VideoStreamInfo : StreamInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }

        [JsonIgnore]
        public override string DisplayText => $"视频：{BandwidthText}";
    }

    public class SearchResponse
    {
        public List<VideoStreamInfo> Videos { get; set; } = new();
        public List<StreamInfo> Audios { get; set; } = new();
        public string Title { get; set; } = "bilibili";
        public string? Error { get; set; }
    }
}
