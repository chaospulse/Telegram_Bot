using HtmlAgilityPack;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram_Bot
{
	public delegate Task CommandHanlder(Message message, ITelegramBotClient botClient);
	
	internal partial class TelegramBot
	{
		private static TelegramBotClient botClient = null!;
		private static Dictionary<string, CommandHanlder> botCommands = null!;

		// create a dictionary to store chat settings for one bot session
		private static Dictionary<long, TikTokSettingsState> chatSettings = new Dictionary<long, TikTokSettingsState>();

		public static void Main()
		{
			using var cts = new CancellationTokenSource();
			var receiverOptions = new ReceiverOptions
			{
				AllowedUpdates = Array.Empty<UpdateType>()
			};
			// create bot client
			botClient = new TelegramBotClient(System.Configuration.ConfigurationManager.AppSettings["BotToken"]!);
			// configure bot commands
			botCommands = new Dictionary<string, CommandHanlder>()
			{
				{ "/help", HelpHandlerAsync },
				{ "/settings", SettingsHandlerAsync }
			};
			// start receiving messages
			botClient.StartReceiving(UpdatesHanlderAsync, ErrorsHandlerAsync, receiverOptions, cts.Token);

			Console.WriteLine($"[{DateTime.UtcNow} UTC] Bot started.");
			Console.ReadLine();
			cts.Cancel();
			Console.WriteLine($"[{DateTime.UtcNow} UTC] Bot stopped");
		}
	}
}

