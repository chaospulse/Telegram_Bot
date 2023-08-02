using HtmlAgilityPack;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YoutubeExplode;

namespace Telegram_Bot
{
	// delegate for command handlers
	public delegate Task CommandHanlder(Message message, ITelegramBotClient botClient);
	
	internal partial class TelegramBot
	{
		private static TelegramBotClient botClient = null!;
		private static Dictionary<string, CommandHanlder> botCommands = null!;
		public static void Main()
		{
			botClient = new TelegramBotClient(System.Configuration.ConfigurationManager.AppSettings["BotToken"]!);
		
			using var cts = new CancellationTokenSource();
			var updates = new QueuedUpdateReceiver(botClient);
			var receiverOptions = new ReceiverOptions
			{
				AllowedUpdates = Array.Empty<UpdateType>()
			};
			// create bot client
			
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

