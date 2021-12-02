using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;//Пакет JSON
using OperatorBot;
using OperatorBot.Models;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

namespace OperatorBot
{

    class Program
    {
        private static string token { get; set; } = "2137487671:AAGI3yeW8Tx_QgaQ7oExAJVjBgvypRDv2rQ";
        private static TelegramBotClient client;
        private static int Iteration = 0;
        private static Context _db = new Context();
        public static Dictionary<long, string> iterator;
        public static Responser responser;
        static void Main(string[] args)
        {
            iterator = new Dictionary<long, string>();
            client = new TelegramBotClient(token);
            responser = new Responser("9083045670", "980Q2Qgu", "230225");
            responser.Authenticate();
            client.StartReceiving();
            client.OnMessage += OnMessageHandler;
            Console.ReadLine();
            client.StopReceiving();
        }

        static void OnMessageHandler(object sender, MessageEventArgs e)
        {

            var msg = e.Message;
            if (msg.Text != null)
            {
                if (msg.Text == "/start")
                {
                    Console.WriteLine($"Пришло сообщение с текстом: {msg.Text}");
                    iterator.Remove(msg.Chat.Id);
                    client.SendTextMessageAsync(msg.Chat.Id, $"Добро пожаловать в Бота - Оператора, {msg.Chat.FirstName + " " + msg.Chat.LastName}. Я помогу получить Вам путевой лист на поездку. Для начала, выберите действие снизу", replyMarkup: GetButtons());
                }
                //Ответы кастомные
                if (iterator.TryGetValue(msg.Chat.Id, out var state))
                {
                    var Iteration = iterator.FirstOrDefault(x => x.Key == msg.Chat.Id);
                    if (Iteration.Value == "Ввод ИД водителя")
                    {
                        var userId = msg.Text;
                        var employer = responser.GetEmployes(userId);
                        if (!employer.C_FIO.StartsWith("403"))
                        {
                            client.SendTextMessageAsync(msg.Chat.Id, $"Авторизация успешна. Добро пожаловать, {employer.C_FIO}");
                        }
                        else
                            client.SendTextMessageAsync(msg.Chat.Id, $"{employer.C_FIO}");
                    }
                }
                if (msg.Text == "Получить путевой лист")
                {
                    iterator.Add(msg.Chat.Id, "Получить путевой лист");
                    var driver = _db.Driver.FirstOrDefault(x => x.UserName == msg.Chat.Username);
                    if (driver == null)
                    {
                        client.SendTextMessageAsync(msg.Chat.Id, $"Бот Вас не знает. Давайте познакомимся. Введите Ваш единый идентификатор водителя в системе КИС АРТ: ");
                        iterator.Remove(msg.Chat.Id);
                        iterator.Add(msg.Chat.Id, "Ввод ИД водителя");
                    }

                }

            }
        }

        private static IReplyMarkup GetButtons()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton> { new KeyboardButton { Text = "Получить путевой лист" } },
                    new List<KeyboardButton> { new KeyboardButton { Text = "Выбрать или изменить перевозчика" } },
                    new List<KeyboardButton> { new KeyboardButton { Text = "Как меня зовут?" } }
                }
            };
        }
    }
}
