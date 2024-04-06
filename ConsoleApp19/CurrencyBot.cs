using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleApp19;

using Telegram.Bot;

public class CurrencyBot
{
    private readonly TelegramBotClient _telegramBotClient;

    private readonly List<string> _currenceCodes = new()
    {
        CurrencyCode.BTC, CurrencyCode.BNB, CurrencyCode.ETH, CurrencyCode.DOGE
    };

    public CurrencyBot(string token)
    {
        _telegramBotClient = new TelegramBotClient(token);
    }

    public void CreateCommands()
    {
        _telegramBotClient.SetMyCommandsAsync(new List<BotCommand>()
        {
            new()
            {
                Command = CustomBotCommands.START,
                Description = "Запуск бота."
            }, 
            new()
            {
                Command = CustomBotCommands.SHOW_CURENCIES,
                Description = "Вывод сообщения с выбором 1 из 4 валют, для получения ее цены в данный момент"
            }

    

        });
    }

    public void StartReceiving()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
//Список обрабатываемых типов сообщений
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new UpdateType[]
            {
                UpdateType.Message, UpdateType.CallbackQuery
            }
        };
        _telegramBotClient.StartReceiving(
            HandleUpdateAsync,HandleError,receiverOptions,cancellationToken);
    }

    private Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        switch (update.Type)
        {
            case UpdateType.Message :
                await HandleMessageAsync(update, cancellationToken);
                break;
            case UpdateType.CallbackQuery:
                HandleCallbackQueryAsync(update, cancellationToken);
                break;
        }
    }

    private async Task HandleMessageAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message == null)
        {
            return;
        }

        var chatId = update.Message.Chat.Id;
        await DeleteMessage(chatId, update.Message.MessageId, cancellationToken);
        if (update.Message.Text == null)
        {
            await _telegramBotClient.SendTextMessageAsync(chatId: chatId, text: "Бот принимает только строки",cancellationToken:cancellationToken);
            return;
        }

        var messageText = update.Message.Text;
        if (IsStartCommand(messageText))
        {
            await SendStartMessageAsync(chatId, cancellationToken);
            return;
        }

        if (IsShowCommand(messageText))
        {
            await SendShowMessageAsync(chatId, cancellationToken);
        }
    }

    private async Task DeleteMessage(long chatId, int messageId, CancellationToken cancellationToken)
    {
        await _telegramBotClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
    }

    private bool IsStartCommand(string messageText)
    {
        return messageText.ToLower() == CustomBotCommands.START;
    }

    private bool IsShowCommand(string messageText)
    {
        return messageText.ToLower() == CustomBotCommands.SHOW_CURENCIES;
    }

    private async Task SendStartMessageAsync(long? chatId, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Выбрать валюту.", CustomCallbackData.SHOW_CURRENCIES_MENU),
            }
        });
        await _telegramBotClient.SendTextMessageAsync(chatId,
            "Привет!\n" + "Данный бот показывает текущий курс выбранной валюты.\n",
            replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    private async Task SendShowMessageAsync(long? chatId, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Bitcoin", CurrencyCode.BTC),
                InlineKeyboardButton.WithCallbackData("Ethereum", CurrencyCode.ETH)
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("BNB", CurrencyCode.BNB),
                InlineKeyboardButton.WithCallbackData("Dogecoin", CurrencyCode.DOGE)
            }
        });
        await _telegramBotClient.SendTextMessageAsync(chatId: chatId, text: "Выберите валюту:",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleCallbackQueryAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery?.Message == null)
        {
            return;
        }

        var chatId = update.CallbackQuery.Message.Chat.Id;
        var callbackData = update.CallbackQuery.Data;
        var messageId = update.CallbackQuery.Message.MessageId;
        if (callbackData == CustomCallbackData.SHOW_CURRENCIES_MENU)
        {
            await DeleteMessage(chatId, messageId, cancellationToken);
            await SendShowMessageAsync(chatId, cancellationToken);
            return;
        }

        if (_currenceCodes.Contains(callbackData))
        {
            await DeleteMessage(chatId, messageId, cancellationToken);
            await SendCurrencyPriceAsync(chatId, callbackData, cancellationToken);
            return;
        }

        if (callbackData == CustomCallbackData.RETURN_TO_CURRENCIES_MENU)
        {
            await DeleteMessage(chatId, messageId, cancellationToken);
            await SendShowMessageAsync(chatId, cancellationToken);
        }
    }

    private async Task SendCurrencyPriceAsync(long? chatId, string currencyCode, CancellationToken cancellationToken)
    {
        var price = await CoinMarket.GetPriceAsync(currencyCode);

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Выбрать другую валюту.",
                    CustomCallbackData.RETURN_TO_CURRENCIES_MENU)
            }
        });
        await _telegramBotClient.SendTextMessageAsync(chatId,
            text: $"Валюта:{currencyCode},стоимость:{Math.Round(price, 3)}",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }
}