using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Telegram_Bot
{
	public static class YouTubeMediaSend
	{
		private static Dictionary<long, YoutubeClient> youtubeSettings = new Dictionary<long, YoutubeClient>();
		private static string GetYouTubeVideoId(string url)
		{
			// Regex pattern for YouTube Shorts and YouTube Video URLs
			string pattern = @"(?:youtube\.com/(?:[^/\n\s]+/[^/\n\s]+/?|(?:v|e(?:mbed)?)/|[^/\n\s]+[?#](?:v=|.*\bv=))|youtu\.be/|youtube\.com/shorts/)([a-zA-Z0-9_-]{11})";

			// Regex for YouTube Video URLs
			var regex = new Regex(pattern);

			// Match URL against regex
			Match match = regex.Match(url);

			// if match found, return video ID
			if (match.Success)
				return match.Groups[1].Value;

			// if no match found, return empty string
			return string.Empty;
		}
		public static async Task MediaSend(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			// Create a new instance of the YouTubeClient class if it doesn't exist or get the existing one
			if (!youtubeSettings.TryGetValue(update.Message.Chat.Id, out var youtubeClient))
			{
				youtubeClient = new YoutubeClient();
				youtubeSettings[update.Message.Chat.Id] = youtubeClient;
			}
			// Get video ID from URL
			YoutubeExplode.Videos.VideoId videoId = GetYouTubeVideoId(update.Message.Text);
			try
			{
				// Get video stream manifests
				var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(videoId);
				// Get muxed stream with highest video quality
				var videoStreamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

				if (videoStreamInfo != null)
				{
					using (var videoStream = await youtubeClient.Videos.Streams.GetAsync(videoStreamInfo))
					{
						// Send video to chat
						await botClient.SendVideoAsync(
							chatId: update.Message.Chat.Id,
							video: InputFile.FromUri(videoStreamInfo.Url),
							replyToMessageId: update.Message.MessageId);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Error while uploading video.");
			}
		}
	}
}
