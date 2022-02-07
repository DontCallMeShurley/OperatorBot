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
using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; //Пакет JSON
using OperatorBot;
using OperatorBot.Controllers;
using OperatorBot.Models;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace OperatorBot
{


    class Program
    {
        private static TelegramBotClient client;
        private static int Iteration = 0;
        private static Context _db = new Context();
        public static Dictionary<long, string> iterator;
        public static Responser responser;
        public static Responser MedicResponser;
        public static Responser MechanicResponser;
        static void Main(string[] args)
        {
            var settings = _db.Settings.FirstOrDefault();
            MedicResponser = new Responser(settings.medMsdisn, settings.medPass, null);
            MedicResponser.Authenticate();
            MechanicResponser = new Responser(settings.mechMsdisn, settings.mechPass, null);
            MechanicResponser.Authenticate();
            iterator = new Dictionary<long, string>();
            client = new TelegramBotClient(settings.botToken);
            responser = new Responser();


            client.StartReceiving();
            client.OnMessage += OnMessageHandler;
            Console.ReadLine();
            client.StopReceiving();
        }

        static async void OnMessageHandler(object sender, MessageEventArgs e)
        {
            Task.Run(async delegate
             {
                 var msg = e.Message;
                 Console.WriteLine($"{DateTime.Now} - Пришло сообщение с текстом: {msg.Text}. Имя пользователя - {msg.Chat.Username}. ID чата с пользователем - {msg.Chat.Id}", Color.Green);
                 Driver driver;
                 var Iteration = iterator.FirstOrDefault(x => x.Key == msg.Chat.Id);
                 try
                 {
                     driver = _db.Driver.FirstOrDefault(x => x.UserName == msg.Chat.Username);
                 }
                 catch
                 {
                     driver = new Driver();
                 }
                 if (driver != null)
                     if (driver.licenser_id != null)
                     {
                         var lice = _db.Licenser.FirstOrDefault(x => x.ID == driver.licenser_id);

                         if (lice != null)
                         {
                             responser.msidn = lice.msidn.Replace("\r\n", "");
                             responser.password = lice.password.Replace("\r\n", "");
                             responser.employer = lice.employerId;
                         }
                     }

                 try
                 {
                     if (msg.Text != null)
                     {
                         //Системные коды
                         if (msg.Text == "Очистить")
                         {
                             iterator.Remove(msg.Chat.Id);
                             driver.Code = null;
                             RemoveAndAdd(driver);
                             await client.SendTextMessageAsync(msg.Chat.Id, "Успешно");
                         }
                         if (msg.Text == "/start")
                         {
                             iterator.Remove(msg.Chat.Id);
                             if (driver == null)
                                 await client.SendTextMessageAsync(msg.Chat.Id, $"Добро пожаловать в Бота - Оператора, {msg.Chat.FirstName + " " + msg.Chat.LastName}. " +
                                                                          $"Я помогу получить Вам путевой лист на поездку. Для начала, выберите действие снизу", replyMarkup: GetButtons(2));
                             else
                                 await client.SendTextMessageAsync(msg.Chat.Id, $"Добро пожаловать в Бота - Оператора, {msg.Chat.FirstName + " " + msg.Chat.LastName}. " +
                                                                          $"Я помогу получить Вам путевой лист на поездку. Для начала, выберите действие снизу", replyMarkup: GetButtons(0));

                         }
                         if (msg.Text == "/get" && (msg.Chat.Username =="kisart_help" || msg.Chat.Username == "Eimooooabq"))
                         {
                             iterator.Remove(msg.Chat.Id);
                             var filesopen = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/waybills/открытые");
                             var filesclosed = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/waybills/закрытые");
                             var filescount = filesopen.Count() + filesclosed.Count();
                             if (filescount > 0)
                                 await client.SendTextMessageAsync(msg.Chat.Id, $"Приветствую Администратор. В текущий момент количество путевых листов на диске : {filescount}.  " +
                                     $"Из них открытых - {filesopen.Count()}, закрытых - {filesclosed.Count()} Приступаю к пересылке. ");
                             else
                                 await client.SendTextMessageAsync(msg.Chat.Id, $"Приветствую Администратор. В текущий момент нет путевых листов, которые я бы мог тебе прислать");

                             var zipFile = AppDomain.CurrentDomain.BaseDirectory + $"/waybills/" + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + ".zip";

                             //
                             using (ZipFile zip = new ZipFile())
                             {

                                 zip.AlternateEncoding = Encoding.UTF8;
                                 zip.ProvisionalAlternateEncoding = Encoding.GetEncoding(Console.OutputEncoding.CodePage);
                                 zip.AlternateEncodingUsage = ZipOption.AsNecessary;

                                 zip.AddFiles(filesopen, "открытые");
                                 zip.AddFiles(filesclosed, "закрытые");

                                 zip.Save(zipFile);
                                 if (msg.Chat.Username != "Eimooooabq")
                                 {
                                     foreach (var file in filesopen)
                                     {
                                         File.Delete(file);
                                     }
                                     foreach (var file in filesclosed)
                                     {
                                         File.Delete(file);
                                     }
                                 }
                             }
                             if (filescount > 0)
                             {
                                 var stream2 = File.OpenRead(zipFile);
                                 var output = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream2, Path.GetFileName(zipFile));
                                 await client.SendDocumentAsync(msg.Chat.Id, output);
                                 stream2.Flush();
                                 stream2.Close();

                                 Console.ForegroundColor = ConsoleColor.Cyan;
                                 Console.WriteLine($"{DateTime.Now} - Выгружено {filescount} путевых листов");
                                 Console.ForegroundColor = ConsoleColor.White;
                                 // File.Delete(zipFile);
                                 await client.SendTextMessageAsync(msg.Chat.Id, "Все путевые листы были отправлены. Все путевые листы были удалены с диска. Всего хорошего, Администратор!");
                             }
                         }
                         //Ответы кастомные для регистрации первоначальной при нажатии на получение путевого листа начинаются итерации. Все шаги проходят последовательно
                         if (msg.Text != "Получить/Закрыть путевой лист" && msg.Text != "Выбрать или изменить перевозчика" && Iteration.Value != "Ответ на вопрос закрытия ПЛ")
                         {
                             if (iterator.TryGetValue(msg.Chat.Id, out var state))
                             {
                                 if (Iteration.Value == "Ввод ИД водителя")
                                 {

                                     responser.msidn = driver.licenser.msidn.Replace("\r\n", "");
                                     responser.password = driver.licenser.password.Replace("\r\n", "");
                                     responser.employer = driver.licenser.employerId;

                                     var C_FIO = responser.GetFIOorErrorAsync(msg.Text).Result;
                                     if (!C_FIO.StartsWith("403"))
                                     {
                                         await client.SendTextMessageAsync(msg.Chat.Id,
                                             $"Авторизация успешна. Добро пожаловать, {C_FIO}. Теперь вы можете начать процедуру получения путевого листа, нажав на соответствующую кнопку", replyMarkup: GetButtons(0));
                                         driver.Code = msg.Text;
                                         driver.C_FIO = C_FIO.ToString();

                                         RemoveAndAdd(driver);
                                     }
                                     else
                                         await client.SendTextMessageAsync(msg.Chat.Id, $"{C_FIO}");
                                 }

                                 if (Iteration.Value == "Ввод ИД перевозчика")
                                 {
                                     var lic = _db.Licenser.FirstOrDefault(x => x.ID == msg.Text);

                                     //Обнулять каждый раз, когда вводим ИД перевозчика
                                     responser.employer = responser.BToken = null;

                                     if (lic == null)
                                     {
                                         await client.SendTextMessageAsync(msg.Chat.Id,
                                             $"Некорректный ввод. Повторите попытку, введя корректное число, которое будет стоять НАПРОТИВ имени Вашего перевозчика");
                                     }
                                     else
                                     {
                                         driver.licenser = lic;
                                         RemoveAndAdd(driver);
                                         if (driver.Code == null)
                                         {
                                             await client.SendTextMessageAsync(msg.Chat.Id,
                                                 $"Отлично. Вы выбрали перевозчика {lic.Name}. Введите Ваш единый идентификатор водителя в системе КИС АРТ: ");
                                             iterator.Remove(msg.Chat.Id);
                                             iterator.Add(msg.Chat.Id, "Ввод ИД водителя");
                                         }
                                         else
                                         {
                                             await client.SendTextMessageAsync(msg.Chat.Id,
                                             $"Перевозчик успешно изменён. В базе данных Бота для пользователя с ID {driver.Code} выбран перевозчик {driver.licenser.Name}. Если вы хотите поменять ID водителя, введите новое значение, если не хотите, введите тоже самое");
                                             iterator.Remove(msg.Chat.Id);
                                             iterator.Add(msg.Chat.Id, "Ввод ИД водителя");
                                         }
                                     }
                                 }
                                 if (Iteration.Value == "Ввод ИД машины")
                                 {
                                     var a = responser.CreateWaybills(driver, msg.Text);
                                     if (a.Result.Length > 7)
                                     {
                                         await client.SendTextMessageAsync(msg.Chat.Id, a.Result + " Проверьте введёные данные");
                                     }
                                     else
                                     {
                                         driver.Waybill = a.Result;
                                         RemoveAndAdd(driver);

                                         await client.SendTextMessageAsync(msg.Chat.Id, "Путевой лист успешно создан. Переходим к созданию осмотров");
                                         await client.SendTextMessageAsync(msg.Chat.Id, "Создание предрейсового осмотра медиком...");

                                         var ans = MedicResponser.CreateMed(driver, false);

                                         await client.SendTextMessageAsync(msg.Chat.Id, ans.Result);
                                         await client.SendTextMessageAsync(msg.Chat.Id, "Введите свой пробег: ");

                                         iterator.Remove(msg.Chat.Id);
                                         iterator.Add(msg.Chat.Id, "Ввод пробега");
                                     }
                                 }
                                 if (Iteration.Value == "Ввод пробега")
                                 {
                                     await client.SendTextMessageAsync(msg.Chat.Id, "Создание предрейсового осмотра механиком. Процесс займёт до 10 минут...");
                                     //iterator.Remove(msg.Chat.Id);
                                     //iterator.Add(msg.Chat.Id, "МеханикПре");
                                     //var ans = MechanicResponser.CreateTech(driver, msg.Text, false);
                                     //await client.SendTextMessageAsync(msg.Chat.Id, "Все осмотры созданы");
                                     //await client.SendTextMessageAsync(msg.Chat.Id, "Ваш путевой лист:");

                                     //var pdf = await responser.SaveWaybillPDF(driver);
                                     //var stream = File.OpenRead(pdf);
                                     //var output = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, pdf);

                                     //await client.SendDocumentAsync(msg.Chat.Id, output);

                                     //stream.Flush();
                                     //stream.Close();

                                     //await client.SendTextMessageAsync(msg.Chat.Id, "Все данные успешно сохранены. Спасибо!");
                                     AsyncResponser asyncResponser = new AsyncResponser();
                                     asyncResponser.Tech(driver, msg.Text, false, msg.Chat.Id, client, MechanicResponser);

                                 }
                                 //Пост теч
                                 else if (Iteration.Value == "Ввод пробега(Пост)")
                                 {
                                     await client.SendTextMessageAsync(msg.Chat.Id, "Создание послерейсового осмотра механиком. Процесс займёт до 10 минут...");
                                     AsyncResponser asyncResponser = new AsyncResponser();
                                     asyncResponser.Tech(driver, msg.Text, true, msg.Chat.Id, client, MechanicResponser);
                                     //iterator.Remove(msg.Chat.Id);
                                     //iterator.Add(msg.Chat.Id, "МеханикПост");
                                     //var ans = MechanicResponser.CreateTech(driver, msg.Text, true);
                                     //await client.SendTextMessageAsync(msg.Chat.Id, "Все послерейсовые осмотры созданы");
                                     //await client.SendTextMessageAsync(msg.Chat.Id, "Ваш путевой лист:", replyMarkup: GetButtons(0));


                                     //var pdf = await responser.SaveWaybillPDF(driver);
                                     ////Путевой лист сразу же удаляем из базы КИС АРТ - он больше нам не нужен
                                     //await responser.DeleteWaybill(driver);

                                     //var stream = File.OpenRead(pdf);
                                     //var output = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, pdf);

                                     //await client.SendDocumentAsync(msg.Chat.Id, output);

                                     //stream.Flush();
                                     //stream.Close();

                                     //await client.SendTextMessageAsync(msg.Chat.Id, "Все данные успешно сохранены. Спасибо!");
                                 }
                             }
                         }
                         else if (msg.Text == "Получить/Закрыть путевой лист")
                         {

                             iterator.Remove(msg.Chat.Id);
                             iterator.Add(msg.Chat.Id, "Получить/Закрыть путевой лист");
                             if (driver == null || driver.licenser_id == null || driver.Code == null)
                             {
                                 driver = new Driver();
                                 driver.UserName = msg.Chat.Username;
                                 //Удаляем и создаём заново
                                 RemoveAndAdd(driver);
                                 // client.SendTextMessageAsync(msg.Chat.Id, $"Бот Вас не знает. Давайте познакомимся. Введите Ваш единый идентификатор водителя в системе КИС АРТ: ");
                                 await client.SendTextMessageAsync(msg.Chat.Id, $"Бот Вас не знает. Давайте познакомимся. Для начала выберите своего перевозчика из списка ниже, введя цифру, которая будет соответствовать выбранному перевозчику." +
                                      $" Внимание! Если перевозчика нет в списке, обратитесь к администратору: ");

                                 var a = _db.Licenser.OrderBy(x => x.ID).ToList();
                                 foreach (var lic in _db.Licenser.OrderBy(x => Convert.ToInt32(x.ID)).ToList())
                                 {
                                     await client.SendTextMessageAsync(msg.Chat.Id, lic.ID + " " + lic.Name);
                                 }
                                 iterator.Remove(msg.Chat.Id);
                                 iterator.Add(msg.Chat.Id, "Ввод ИД перевозчика");
                             }
                             else
                             {
                                 var lic = _db.Licenser.FirstOrDefault(a => a.ID == driver.licenser_id);
                                 responser.msidn = lic.msidn.Replace("\r\n", "");
                                 responser.password = lic.password.Replace("\r\n", "");
                                 responser.employer = lic.employerId;
                                 var a = responser.GetWaybill(driver);

                                 //По сути здесь начинается процесс получения путевого листа. Нужно выбрать ИД машины и запустить итерационный процесс
                                 if (a.Result == "-1")
                                 {
                                     await client.SendTextMessageAsync(msg.Chat.Id, $"Здравствуйте, {driver.C_FIO}. Выберите вашу машину из списка ниже, введя число, которое стоит рядом с машиной");

                                     var Cars = responser.GetCarsAsync().Result;

                                     foreach (Cars car in Cars.OrderBy(x => x.ID))
                                     {
                                         await client.SendTextMessageAsync(msg.Chat.Id, $"[{car.ID}] -  {car.ShortName}");
                                     }
                                     iterator.Remove(msg.Chat.Id);
                                     iterator.Add(msg.Chat.Id, "Ввод ИД машины");
                                 }
                                 else
                                 {
                                     driver.Waybill = a.Result;
                                     RemoveAndAdd(driver);
                                     await client.SendTextMessageAsync(msg.Chat.Id, $"{driver.C_FIO}, на ваше имя уже есть действующий путевой лист. Выглядит он так: ");

                                     var pdf = await responser.SaveWaybillPDF(driver, true);
                                     var stream = File.OpenRead(pdf);
                                     var output = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, pdf);
                                     await client.SendDocumentAsync(msg.Chat.Id, output);

                                     stream.Flush();
                                     stream.Close();
                                     await client.SendTextMessageAsync(msg.Chat.Id, $"Желаете закрыть его? ", replyMarkup: GetButtons(1));

                                     iterator.Remove(msg.Chat.Id);
                                     iterator.Add(msg.Chat.Id, "Ответ на вопрос закрытия ПЛ");
                                 }
                             }
                         }
                         //Закрытие пл
                         else if (Iteration.Value =="Ответ на вопрос закрытия ПЛ")
                         {
                             if (msg.Text == "Да")
                             {
                                 await client.SendTextMessageAsync(msg.Chat.Id, "Создание послерейсового осмотра медиком...", replyMarkup: GetButtons(0));

                                 var ans = MedicResponser.CreateMed(driver, true);

                                 await client.SendTextMessageAsync(msg.Chat.Id, ans.Result);
                                 await client.SendTextMessageAsync(msg.Chat.Id, "Введите свой пробег: ");

                                 iterator.Remove(msg.Chat.Id);
                                 iterator.Add(msg.Chat.Id, "Ввод пробега(Пост)");
                             }
                             else if (msg.Text == "Нет")
                             {
                                 iterator.Remove(msg.Chat.Id);
                                 await client.SendTextMessageAsync(msg.Chat.Id, "Путевой лист закрываться не будет. Вы можете выбрать любое другое доступное действие", replyMarkup: GetButtons(0));
                             }
                         }
                         else if (msg.Text == "Выбрать или изменить перевозчика")
                         {
                             if (driver == null)
                             {
                                 driver = new Driver();
                                 driver.UserName = msg.Chat.Username;
                                 //Удаляем и создаём заново
                                 RemoveAndAdd(driver);
                                 // client.SendTextMessageAsync(msg.Chat.Id, $"Бот Вас не знает. Давайте познакомимся. Введите Ваш единый идентификатор водителя в системе КИС АРТ: ");
                                 await client.SendTextMessageAsync(msg.Chat.Id, $"Бот Вас не знает. Давайте познакомимся. Для начала выберите своего перевозчика из списка ниже, введя цифру, которая будет соответствовать выбранному перевозчику." +
                                      $" Внимание! Если перевозчика нет в списке, обратитесь к администратору: ");

                                 var a = _db.Licenser.OrderBy(x => x.ID).ToList();
                                 foreach (var lic in _db.Licenser.OrderBy(x => Convert.ToInt32(x.ID)).ToList())
                                 {
                                     await client.SendTextMessageAsync(msg.Chat.Id, lic.ID + " " + lic.Name);
                                 }
                                 iterator.Remove(msg.Chat.Id);
                                 iterator.Add(msg.Chat.Id, "Ввод ИД перевозчика");
                             }
                             else if (driver.licenser != null)
                                 await client.SendTextMessageAsync(msg.Chat.Id, $"Ваш текущий перевозчик {driver.licenser.Name}.Выберите своего нового перевозчика из списка ниже, введя цифру, которая будет соответствовать выбранному перевозчику." +
                                        $" Внимание! Если перевозчика нет в списке, обратитесь к администратору: ");
                             else
                                 await client.SendTextMessageAsync(msg.Chat.Id, $"Вам не установлен перевозчик.Выберите своего нового перевозчика из списка ниже, введя цифру, которая будет соответствовать выбранному перевозчику." +
                                   $" Внимание! Если перевозчика нет в списке, обратитесь к администратору: ");

                             foreach (var lic in _db.Licenser)
                             {
                                 await client.SendTextMessageAsync(msg.Chat.Id, lic.ID + " " + lic.Name);
                             }
                             iterator.Remove(msg.Chat.Id);
                             iterator.Add(msg.Chat.Id, "Ввод ИД перевозчика");

                         }
                     }
                 }
                 catch (Exception e1)
                 {
                     iterator.Remove(msg.Chat.Id);
                     await client.SendTextMessageAsync(msg.Chat.Id, $"По техническим причинам выдача путевого листа невозможна. Обратитесь к вашему администратору с указанием следующего кода ошибки");
                     await client.SendTextMessageAsync(msg.Chat.Id, $"{e1.Message}");
                     await client.SendTextMessageAsync(msg.Chat.Id, "Вам будут выданы данные, по которым вы должны заполнить путевой лист ВРУЧНУЮ ");
                     await client.SendTextMessageAsync(msg.Chat.Id, $"Номер путевого листа :  {RandomDigits(11)} - {DateTime.Now.ToString("yyyy")}{DateTime.Now.ToString("MM")}{DateTime.Now.ToString("dd")} ");
                     await client.SendTextMessageAsync(msg.Chat.Id, $"Осмотр медика: Чашечкина Р. А - {DateTime.Now.ToString("HH")}:{DateTime.Now.ToString("mm")}.");
                     await client.SendTextMessageAsync(msg.Chat.Id, $"Осмотр механика: Тихонов А. Н. - {DateTime.Now.AddMinutes(10).ToString("HH")}:{DateTime.Now.AddMinutes(10).ToString("mm")}");

                 }
             });
        }
        private static IReplyMarkup GetButtons(int state) //1-Ответ на вопрос, 2-для незареганных
        {
            if (state == 1)
                return new ReplyKeyboardMarkup
                {
                    Keyboard = new List<List<KeyboardButton>>
                    {
                        new() {new KeyboardButton {Text = "Да"}},
                        new() {new KeyboardButton {Text = "Нет"}}
                    }
                };
            if (state == 2)
                return new ReplyKeyboardMarkup
                {
                    Keyboard = new List<List<KeyboardButton>>
                    {
                        new() { new KeyboardButton { Text = "Получить/Закрыть путевой лист" } }
                    }
                };
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new() {new KeyboardButton {Text = "Выбрать или изменить перевозчика"}},
                    new() {new KeyboardButton {Text = "Получить/Закрыть путевой лист"}}

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
        private static string RandomDigits(int length)
        {
            var random = new Random();
            string s = string.Empty;
            for (int i = 0; i < length; i++)
                s = string.Concat(s, random.Next(10).ToString());
            return s;
        }
    }

}
