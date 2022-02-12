using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OperatorBot.Models;
using Telegram.Bot;

namespace OperatorBot.Controllers
{
    public class AsyncResponser
    {

        //TODO: сам класс изжил себя. Необходимо убрать его и перейти на обычный Responser
        public async Task Tech(Driver driver, string probeg, bool B_Post, long chatId, TelegramBotClient client, Responser mechanicResponser)
        {

            try
            {
                Task.Run(async delegate
                {
                    try
                    {
                        var mainResponser = new Responser(driver.licenser.msidn, driver.licenser.password,
                            driver.licenser.employerId);
                        if (!B_Post)
                            Task.Delay(600000).Wait();
                        else
                            Task.Delay(6000).Wait();
                        mechanicResponser.CreateTech(driver, probeg, B_Post);
                        Task.Delay(1000).Wait();


                        Task.Delay(1000).Wait();
                        var fileName = mechanicResponser.SaveWaybillPDF(driver, !B_Post).Result;
                        await client.SendTextMessageAsync(chatId, "Все осмотры созданы");
                        await client.SendTextMessageAsync(chatId, "Ваш путевой лист:");

                        ////Путевой лист сразу же удаляем из базы КИС АРТ - он больше нам не нужен
                        if (B_Post)
                            await mainResponser.DeleteWaybill(driver);

                        var stream = File.OpenRead(fileName);
                        var output = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, fileName);

                        await client.SendDocumentAsync(chatId, output);

                        stream.Flush();
                        stream.Close();

                        await client.SendTextMessageAsync(chatId, "Все данные успешно сохранены. Спасибо!");
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(
                            $"{DateTime.Now} - Ошибка при прохождении осмотра механиком. Внутри блока очереди. Код - {e.Message}");
                        await client.SendTextMessageAsync(chatId, "Непредвиденная ошибка. Повторите попытку позже. Если ошибка повторится, обратитесь к администратору");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                });
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now} - Ошибка при прохождении осмотра механиком. Код - {e.Message}");
                Console.ForegroundColor = ConsoleColor.White;

            }

        }
    }
}

