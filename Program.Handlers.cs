using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json.Linq;
using Tweetinvi;
using Telegram.Bot.Types.ReplyMarkups;
using Tweetinvi.Core.Models;

namespace Telegram_Bot
{
	partial class TelegramBot
	{
		//
		// /help command 
		//
		private static async Task HelpHandlerAsync(Telegram.Bot.Types.Message message, ITelegramBotClient botClient)
		{
			await botClient.SendTextMessageAsync(
			chatId: message.Chat.Id,
			text: $"Hi! I'm a Downloader Bot created by @{System.Configuration.ConfigurationManager.AppSettings["MyTelegramTag"]}.\nTo change my send config select /settings.\nGood luck!");
		}
		//
		// /settings command  (for TikTok)
		//
		private static async Task SettingsHandlerAsync(Telegram.Bot.Types.Message message, ITelegramBotClient botClient)
		{
			// get current chat settings or create new one if it doesn't exist
			if (!chatSettings.TryGetValue(message.Chat.Id, out var settingsState))
			{
				settingsState = new TikTokSettingsState();
				chatSettings[message.Chat.Id] = settingsState;
			}

			// set values for buttons based on current settings
			settingsState.DescriptionButtonText = settingsState.IsDescriptionEnabled ? "Disable Description" : "Enable Description";
			settingsState.HDVideoLinkButtonText = settingsState.IsHDVideoLinkEnabled ? "Disable HD Video Link" : "Enable HD Video Link";

			// create keyboard
			var inlineKeyboard = new InlineKeyboardMarkup(new[]
			{
				new[]
				{
					InlineKeyboardButton.WithCallbackData(settingsState.DescriptionButtonText, "toggleDescription"),
				},
				new[]
				{
					InlineKeyboardButton.WithCallbackData(settingsState.HDVideoLinkButtonText, "toggleHDVideoLink"),
				}
			});

			// send message with keyboard
			await botClient.SendTextMessageAsync(
				chatId: message.Chat.Id,
				text: "Settings:",
				replyMarkup: inlineKeyboard
			);
		}
		//
		// for Callback Query from Inline Keyboard (for TikTok)
		//
		private static async void OnCallbackQueryReceived(Update update)
		{
			var callbackQuery = update.CallbackQuery;
			var chatId = callbackQuery.Message.Chat.Id;

			// Получаем состояние опций для текущего чата или создаем новое, если его еще нет
			if (!chatSettings.TryGetValue(chatId, out var settingsState))
			{
				settingsState = new TikTokSettingsState();
				chatSettings[chatId] = settingsState;
			}
			switch (callbackQuery.Data)
			{
				case "toggleDescription":
					// turn on/off Description option
					settingsState.IsDescriptionEnabled = !settingsState.IsDescriptionEnabled;
					// edit message text based on new option state	
					settingsState.DescriptionButtonText = settingsState.IsDescriptionEnabled ? "Disable Description" : "Enable Description";

					var updatedInlineKeyboard1 = new InlineKeyboardMarkup(new[]
					{ 
						new[]
						{
							InlineKeyboardButton.WithCallbackData(settingsState.DescriptionButtonText, "toggleDescription"),
						},
						new[]
						{
							InlineKeyboardButton.WithCallbackData(settingsState.HDVideoLinkButtonText, "toggleHDVideoLink"),
						}
					});

					await botClient.EditMessageTextAsync(
						chatId: chatId,
						messageId: callbackQuery.Message.MessageId,
						text: "Settings:",
						replyMarkup: updatedInlineKeyboard1);
					break;

				case "toggleHDVideoLink":
					// turn on/off HD Video Link option
					settingsState.IsHDVideoLinkEnabled = !settingsState.IsHDVideoLinkEnabled;
					// edit message text based on new option state	
					settingsState.HDVideoLinkButtonText = settingsState.IsHDVideoLinkEnabled ? "Disable HD Video Link" : "Enable HD Video Link";

					var updatedInlineKeyboard2 = new InlineKeyboardMarkup(new[]
					{
						new[]
						{
							InlineKeyboardButton.WithCallbackData(settingsState.DescriptionButtonText, "toggleDescription"),
						},
						new[]
						{
							InlineKeyboardButton.WithCallbackData(settingsState.HDVideoLinkButtonText, "toggleHDVideoLink"),
						}
					});

					await botClient.EditMessageTextAsync(
						chatId: chatId,
						messageId: callbackQuery.Message.MessageId,
						text: "Settings:",
						replyMarkup: updatedInlineKeyboard2);
					break;

				default:
					break;
			}
		}
		//
		// /start 
		//
		private async static Task UpdatesHanlderAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			if(update.Type == UpdateType.CallbackQuery)
			{
				OnCallbackQueryReceived(update);
				return;
			}
			if (update.Message.Text != null)
			{
				Console.WriteLine($"Recived message from {update.Message.Chat.Username}: {update.Message.Text}\t| [{update.Message.Date}]");
				//
				// its a command
				//
				if (update.Message.Text!.StartsWith("/"))
				{
					// if command was send from gropup
					if(update.Message.Text.Contains('@'))
					{
						string[] parts = update.Message.Text.Split('@');
						update.Message.Text = parts[0]; 
					}
					// if command is in the list
					if (botCommands.TryGetValue(update.Message.Text, out var hanlder))
						await hanlder(update.Message, botClient);
					return;
				}
				//
				// its a tiktok link
				//
				else if (update.Message.Text.Contains("https://vm.tiktok.com"))
				{
					TikTokMediaSend(update, cancellationToken);
				}
				//
				// its a twitter link
				//
				else if (update.Message.Text.Contains("https://twitter.com"))
				{
					TwitterMediaSend(botClient, update, cancellationToken);
				}
				//
				// message type not recognized
				//
				else
				{
					//await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Sorry i can't recognize this message type.");
				}
			}
			else if (update.Message.Photo != null)
			{
				await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Please send me this in file.");

			}
			else if (update.Message.Document != null)
			{
				//	var fileId = update.Message.Document.FileId;
				//	var fileInfo = await botClient.GetFileAsync(fileId);
				//	var filePath = fileInfo.FilePath;

				//	string destinationFilePath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\{update.Message.Document.FileName}";
				//	await using FileStream fileStream = System.IO.File.OpenWrite(destinationFilePath);
				//	await botClient.DownloadFileAsync(filePath, fileStream);
				//	fileStream.Close();
				//	Console.WriteLine("download successfully!");
			}
		}
		
		
		private static async Task ErrorsHandlerAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
		{
			await botClient.DeleteWebhookAsync(true, cancellationToken);
			Console.WriteLine($"[{DateTime.Now}] Error: {exception.Message}");
		}
	}
}