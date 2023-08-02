using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using AngleSharp.Css.Dom;

namespace Telegram_Bot
{
	public static class InstagramMediaSend
	{
		public static async void MediaSend(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			// encode url
			string url = System.Web.HttpUtility.UrlEncode(update.Message.Text);

			//RestSharp
			var client = new RestClient($"https://instagram-downloader-download-instagram-videos-stories.p.rapidapi.com/index?url={url}");
			var request = new RestRequest();
			request.AddHeader("X-RapidAPI-Key", System.Configuration.ConfigurationManager.AppSettings["X-RapidAPI-Key"]);
			request.AddHeader("X-RapidAPI-Host", System.Configuration.ConfigurationManager.AppSettings["X-RapidAPI-Host(Instagram)"]);
			var response = client.Execute(request);
			// parse the JSON response
			var jsonResponse = JObject.Parse(response.Content);

			// if url was a post with multiple images or videos
			JArray mediaGroupArray = (JArray)jsonResponse["media_with_thumb"];
			// if url was a post with one image or video
			var media = jsonResponse["media"];
			// if url was a story
			JArray storiesArray = (JArray)jsonResponse["stories"];

			//  logic for sending videos and images from story
			if (storiesArray != null)
			{
				List<IAlbumInputMedia> storyImagesList = new List<IAlbumInputMedia>();
				List<IAlbumInputMedia> storyVideosList = new List<IAlbumInputMedia>();
				foreach (var storyUrl in storiesArray)
				{
					if (storyUrl["Type"].ToString() != "Story-Video")
						storyImagesList.Add(new InputMediaPhoto(InputFile.FromUri(storyUrl["media"].ToString())));
					else
						storyVideosList.Add(new InputMediaVideo(InputFile.FromUri(storyUrl["media"].ToString())));
				}
				if (storyImagesList.Count > 0)
				{
					// media group is limited to 10 items
					for (int i = 0; i < storyImagesList.Count; i += 10)
					{
						// take 10 items from storyImagesList
						var mediaGroup = storyImagesList.Skip(i).Take(10).ToArray();

						// send media group to user
						await botClient.SendMediaGroupAsync(
							chatId: update.Message.Chat.Id,
							media: mediaGroup,
							replyToMessageId: update.Message.MessageId,
							cancellationToken: cancellationToken);
						await Task.Delay(250);
					}
				}
				if (storyVideosList.Count > 0)
				{
					// media group is limited to 10 items
					for (int i = 0; i < storyVideosList.Count; i += 10)
					{
						// take 10 items from storyVideosList
						var mediaGroup = storyVideosList.Skip(i).Take(10).ToArray();

						// send media group to user
						await botClient.SendMediaGroupAsync(
							chatId: update.Message.Chat.Id,
							media: mediaGroup,
							replyToMessageId: update.Message.MessageId,
							cancellationToken: cancellationToken);
						await Task.Delay(250);
					}
				}
			}
			// logic for sending videos and images from posts
			else if (mediaGroupArray != null)
			{
				List<IAlbumInputMedia> imagesList = new List<IAlbumInputMedia>();
				List<IAlbumInputMedia> videosList = new List<IAlbumInputMedia>();

				foreach (var mediaUrl in mediaGroupArray)
				{
					if (mediaUrl["Type"].ToString() != "Video")
						imagesList.Add(new InputMediaPhoto(InputFile.FromUri(mediaUrl["media"].ToString())));
					else
						videosList.Add(new InputMediaVideo(InputFile.FromUri(mediaUrl["media"].ToString())));
				}
				// send images if they exist in the post
				if (imagesList.Count > 0)
				{
					await botClient.SendMediaGroupAsync(
						chatId: update.Message.Chat.Id,
						media: imagesList,
						replyToMessageId: update.Message.MessageId,
						cancellationToken: cancellationToken);
				}
				// send videos if they exist in the post
				if (videosList.Count > 0)
				{
					await botClient.SendMediaGroupAsync(
						chatId: update.Message.Chat.Id,
						media: videosList,
						replyToMessageId: update.Message.MessageId,
						cancellationToken: cancellationToken);
				}
			}
			// logic for sending only one video or image from posts
			else if (media != null)
			{
				List<IAlbumInputMedia> mediaFile = new List<IAlbumInputMedia>()
				{
					new InputMediaVideo(InputFile.FromUri(media.ToString()))
				};
				await botClient.SendMediaGroupAsync(
						chatId: update.Message.Chat.Id,
						media: mediaFile,
						replyToMessageId: update.Message.MessageId,
						cancellationToken: cancellationToken);
			}
		}
	}
}
