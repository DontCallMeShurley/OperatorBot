using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; //Пакет JSON
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
        public static Responser MedicResponser;
        public static Responser MechanicResponser;
        static void Main(string[] args)
        {
            //MedicResponser = new Responser("9663648005", "Ua44NkV0", null);
            //MedicResponser.Authenticate();
            //MechanicResponser = new Responser("9519997810", "31spFKX5", null);
            //MechanicResponser.Authenticate();
            iterator = new Dictionary<long, string>();
            client = new TelegramBotClient(token);
            responser = new Responser();
            client.StartReceiving();
            client.OnMessage += OnMessageHandler;
            Console.ReadLine();
            client.StopReceiving();
        }

        static void OnMessageHandler(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            Console.WriteLine($"{DateTime.Now} - Пришло сообщение с текстом: {msg.Text}. Имя пользователя - {msg.Chat.Username}. ID чата с пользователем - {msg.Chat.Id}", Color.Green);
            var driver = _db.Driver.FirstOrDefault(x => x.UserName == msg.Chat.Username);
            if (msg.Text != null)
            {
                if (msg.Text == "/start")
                {
                    iterator.Remove(msg.Chat.Id);
                    client.SendTextMessageAsync(msg.Chat.Id, $"Добро пожаловать в Бота - Оператора, {msg.Chat.FirstName + " " + msg.Chat.LastName}. " +
                                                             $"Я помогу получить Вам путевой лист на поездку. Для начала, выберите действие снизу", replyMarkup: GetButtons());

                }
                //Ответы кастомные при нажатии на получение путевого листа начинаются итерации. Все шаги проходят последовательно
                if (msg.Text != "Получить путевой лист")
                {
                    if (iterator.TryGetValue(msg.Chat.Id, out var state))
                    {
                        var Iteration = iterator.FirstOrDefault(x => x.Key == msg.Chat.Id);
                        if (Iteration.Value == "Ввод ИД водителя")
                        {
                            responser.msidn = driver.licenser.msidn;
                            responser.password = driver.licenser.password;
                            var C_FIO = responser.GetFIOorError(msg.Text);
                            if (!C_FIO.StartsWith("403"))
                            {
                                client.SendTextMessageAsync(msg.Chat.Id,
                                    $"Авторизация успешна. Добро пожаловать, {C_FIO}. Теперь вы можете начать процедуру получения путевого листа, нажав на соответствующую кнопку");
                                driver.Code = msg.Text;
                                driver.C_FIO = C_FIO;

                                RemoveAndAdd(driver);
                            }
                            else
                                client.SendTextMessageAsync(msg.Chat.Id, $"{C_FIO}");
                        }

                        if (Iteration.Value == "Ввод ИД перевозчика")
                        {

                            var lic = _db.Licenser.FirstOrDefault(x => x.ID == msg.Text);
                            if (lic == null)
                            {
                                client.SendTextMessageAsync(msg.Chat.Id,
                                    $"Некорректный ввод. Повторите попытку, введя корректное число, которое будет стоять НАПРОТИВ имени Вашего перевозчика");
                            }
                            else
                            {
                                driver.licenser = lic;
                                RemoveAndAdd(driver);
                                client.SendTextMessageAsync(msg.Chat.Id,
                                    $"Отлично. Вы выбрали перевозчика {lic.Name}. Введите Ваш единый идентификатор водителя в системе КИС АРТ: ");
                                iterator.Remove(msg.Chat.Id);
                                iterator.Add(msg.Chat.Id, "Ввод ИД водителя");
                            }
                        }
                    }
                }
                else if (msg.Text == "Получить путевой лист")
                {
                    iterator.Remove(msg.Chat.Id);
                    iterator.Add(msg.Chat.Id, "Получить путевой лист");
                    if (driver == null || driver.licenser == null || driver.Code == null)
                    {
                        driver = new Driver();
                        driver.UserName = msg.Chat.Username;
                        //Удаляем и создаём заново
                        RemoveAndAdd(driver);
                        // client.SendTextMessageAsync(msg.Chat.Id, $"Бот Вас не знает. Давайте познакомимся. Введите Ваш единый идентификатор водителя в системе КИС АРТ: ");
                        client.SendTextMessageAsync(msg.Chat.Id, $"Бот Вас не знает. Давайте познакомимся. Для начала выберите своего перевозчика из списка ниже, введя цифру, которая будет соответствовать выбранному перевозчику." +
                            $" Внимание! Если перевозчика нет в списке, обратитесь к администратору: ");

                        foreach (var lic in _db.Licenser)
                        {
                            client.SendTextMessageAsync(msg.Chat.Id, lic.ID + " " + lic.Name);
                        }
                        iterator.Remove(msg.Chat.Id);
                        iterator.Add(msg.Chat.Id, "Ввод ИД перевозчика");
                    }
                    else
                    {
                        client.SendTextMessageAsync(msg.Chat.Id, $"Здравствуйте, {driver.C_FIO} Тут будет получение путевого листа");
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
                    new List<KeyboardButton> { new KeyboardButton { Text = "Выбрать или изменить перевозчика" } }
                }
            };
        }
        //Необходимо постоянно иметь какую то версию драйвера в базе данных. На каждое сообщение при регистрации создаётся модель в базе данных и удаляется старая. Для увеличения возможных итераций использую GUID
        private static void RemoveAndAdd(Driver driver)
        {
         //   var a = _db.Driver.Where(x => x.UserName == driver.UserName).ToList();
            _db.Driver.RemoveRange(_db.Driver.Where(x => x.UserName == driver.UserName).ToList());
            _db.SaveChanges();
            _db.Driver.Add(driver);
            _db.SaveChanges();

        }
    }

}
