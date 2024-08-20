using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace SJPCORE.Util
{
    public class YoutubeHelper
    {

        public static async Task<string> GetAudioStreamLinkAsync(string videoUrl)
        {
            var _client = new YoutubeClient();
            var videoId = await _client.Videos.GetAsync(videoUrl);
            var streams = await _client.Videos.Streams.GetManifestAsync(videoUrl);
            var audioStreamInfo = streams.GetAudioOnlyStreams();
            var audioStream = audioStreamInfo.GetWithHighestBitrate();
            return audioStream.Url;
        }
    }
}
