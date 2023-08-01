using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram_Bot
{
	// inline keyboard settings (for TikTok)
	public class TikTokSettingsState
	{
		public string? DescriptionButtonText { get; set; } = "Disable Description";
		public string? HDVideoLinkButtonText { get; set; } = "Disable HD Video Link";
		public bool IsDescriptionEnabled { get; set; } = true;
		public bool IsHDVideoLinkEnabled { get; set; } = true;
	}
	public static class TikTokMediaSend
	{
		// create a dictionary to store chat settings for one bot session
		public static Dictionary<long, TikTokSettingsState> chatSettings = new Dictionary<long, TikTokSettingsState>();

		// send media in chat
		public static async Task MediaSend(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			if (!chatSettings.TryGetValue(update.Message.Chat.Id, out var settingsState))
			{
				settingsState = new TikTokSettingsState();
				chatSettings[update.Message.Chat.Id] = settingsState;
			}

			string tiktokVideoUrl = update.Message.Text.Substring(22, update.Message.Text.Length - 23);

			// HttpClienet
			var client = new HttpClient();
			var request = new HttpRequestMessage
			{
				Method = System.Net.Http.HttpMethod.Get,
				RequestUri = new Uri($"https://tiktok-video-no-watermark2.p.rapidapi.com/?url=https%3A%2F%2Fvm.tiktok.com%2F{tiktokVideoUrl}%2F&hd=1"),
				Headers =
				{
					{ "X-RapidAPI-Key", System.Configuration.ConfigurationManager.AppSettings["X-RapidAPI-Key"] },
					{ "X-RapidAPI-Host", "tiktok-video-no-watermark2.p.rapidapi.com" }
				},
			};

			// RestCharp
			//var client = new RestClient($"https://tiktok-video-no-watermark2.p.rapidapi.com/?url=https%3A%2F%2Fvm.tiktok.com%2F{tiktokVideoUrl}%2F&hd=1");
			//var request = new RestRequest();
			//request.AddHeader("X-RapidAPI-Key", System.Configuration.ConfigurationManager.AppSettings["X-RapidAPI-Key"]);
			//request.AddHeader("X-RapidAPI-Host", "tiktok-video-no-watermark2.p.rapidapi.com");

			//create JSON response
			JObject jsonResponse;
			using (var response = await client.SendAsync(request))
			{
				// ensure the response is successful
				response.EnsureSuccessStatusCode();

				// read the response as a string
				var body = await response.Content.ReadAsStringAsync();

				// parse the JSON response
				jsonResponse = JObject.Parse(body);
			}

			// tiktok is consists of images
			if (jsonResponse["data"]["images"] != null)
			{
				// get images array
				JArray imagesArray = (JArray)jsonResponse["data"]["images"];

				// create media group
				List<IAlbumInputMedia> mediaList = new List<IAlbumInputMedia>();
				foreach (var imageUrl in imagesArray)
				{
					// add image to media group
					mediaList.Add(new InputMediaPhoto(InputFile.FromUri(imageUrl.ToString())));
				}

				//send media group to user
				await botClient.SendMediaGroupAsync(
				chatId: update.Message.Chat.Id,
				media: mediaList.ToArray(),
				replyToMessageId: update.Message.MessageId,
				cancellationToken: cancellationToken);
			}
			//tiktok is a video
			else
			{
				string videoUrl = jsonResponse["data"]["play"].ToString();
				string HDvideoUrl = jsonResponse["data"]["hdplay"].ToString();
				string videoDescription = jsonResponse["data"]["title"].ToString();

				string message;
				if (chatSettings[update.Message.Chat.Id].IsDescriptionEnabled && chatSettings[update.Message.Chat.Id].IsHDVideoLinkEnabled)
					message = $"Video Description: {videoDescription}\n\nHD Video Link: {HDvideoUrl}";

				else if (chatSettings[update.Message.Chat.Id].IsDescriptionEnabled && !chatSettings[update.Message.Chat.Id].IsHDVideoLinkEnabled)
					message = $"Video Description: {videoDescription}";

				else if (!chatSettings[update.Message.Chat.Id].IsDescriptionEnabled && chatSettings[update.Message.Chat.Id].IsHDVideoLinkEnabled)
					message = $"HD Video Link: {HDvideoUrl}";

				else
					message = "";

				//send video to user
				await botClient.SendVideoAsync(
				chatId: update.Message.Chat.Id,
				video: InputFile.FromUri(videoUrl),
				caption: message,
				replyToMessageId: update.Message.MessageId);

				//TikTokMediaDownload(jsonResponse, HDvideoUrl);
			}
		}
		private static async Task TikTokMediaDownload(JObject jsonResponse, string HDvideoUrl)
		{
			string videoId = jsonResponse["data"]["id"].ToString();
			string destinationFilePath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\video\{videoId}.mp4";
			using (var httpClient = new HttpClient())
			{
				try
				{
					HttpResponseMessage response = await httpClient.GetAsync(HDvideoUrl);
					response.EnsureSuccessStatusCode();

					using (Stream videoStream = await response.Content.ReadAsStreamAsync())
					{
						// create file and record video
						using (FileStream fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
						{
							await videoStream.CopyToAsync(fileStream);
						}
						Console.WriteLine($"Video was successfully download to {destinationFilePath}");
					}
				}
				catch (HttpRequestException e)
				{
					Console.WriteLine($"Error while download video: {e.Message}");
				}
			}
		}
	}
}
