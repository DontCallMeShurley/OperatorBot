using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;//Пакет JSON
using Newtonsoft.Json.Linq;
using OperatorBot.Models;
using Unity;
using Timer = System.Threading.Timer;

namespace OperatorBot
{

    public class Responser
    {
        public string msidn { get; set; }
        public string password { get; set; }
        public string employer { get; set; }
        public string BToken { get; set; }

        public Responser(string msidn, string password, string employer)
        {
            this.msidn = msidn;
            this.password = password;
            this.employer = employer;
        }
        public Responser()
        {

        }
        public async Task<string> GetFIOorErrorAsync(string IdEmployer)
        {
            await Authenticate();
            string C_FIO;
            HttpWebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/employees/" + IdEmployer);
            try
            {
                request.Method = "GET";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;

                response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                C_FIO = JObject.Parse(responseString).SelectToken("user").SelectToken("lastName").ToString() + " " + JObject.Parse(responseString).SelectToken("user").SelectToken("firstName").ToString() + " " + JObject.Parse(responseString).SelectToken("user").SelectToken("patronymic").ToString();
                var a = response.StatusCode;
            }
            catch
            {
                C_FIO = "403. Ошибка. Вы не найдены в системе, или система не отвечает, или данный водитель не принадлежит выбранному перевозчику. Повторите попытку, введя корректные данные";
            }
            return C_FIO;
        }
        public async Task Authenticate()
        {
            //Вернёт employers если он не указан, или Bearer token
            WebRequest request = WebRequest.Create("https://art.taxi.mos.ru/api/authenticate");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            var postData = $"msisdn={msidn}";
            postData += $"&password={password}";

            if (!string.IsNullOrEmpty(employer))
                postData += $"&employeeId={employer}";
            var data = Encoding.ASCII.GetBytes(postData);
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            if (BToken != null && employer != null)
            {
                Console.WriteLine($"{DateTime.Now} - Аутенфикация уже проведена");
                return;
            }
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                if (!string.IsNullOrEmpty(employer))
                {
                    BToken = JObject.Parse(responseString).SelectToken("token").ToString();
                    Console.WriteLine($"{DateTime.Now} - Получен BToken: {0} ", BToken);
                }
                else
                {
                    employer = JArray.Parse(responseString)[0].SelectToken("employeeId").ToString();
                    Console.WriteLine($"{DateTime.Now} - Получен employeeId: {0} ", employer);
                    //Выполнить через 2 секунды, чтобы ошибку не словить
                    await Task.Delay(2000);
                    await Authenticate();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка в блоке получения кода работника или BTokenb. Возможно, слишком часто стучимся к API КИС АРТ. Обратитесь к разработчику. Код ошибки - {e.Message}");
            }

        }
    public async Task<List<Cars>> GetCarsAsync()
        {
            await Authenticate();
            HttpWebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/vehicles");
            var outputData = new List<Cars>();
            try
            {
                request.Method = "GET";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;

                response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                var a = JArray.Parse(JObject.Parse(responseString).SelectToken("entries").ToString());
                foreach(var b in a)
                {
                    outputData.Add(new Cars(Convert.ToInt32(b.SelectToken("id").ToString()), b.SelectToken("shortName").ToString() ));
                }

            }
            catch
            {
                Console.WriteLine($"{DateTime.Now} -  Непредвиденная ошибка при попытке получить список доступных машин. Обратитесь к разработчику");
            }
            return outputData;
        }
       //Вернёт ошибку или ID созданного путевого листа
    public async Task<string> CreateWaybills(Driver driver, string cars)
        {
            await Authenticate();
            string outputData = "";
            HttpWebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/waybills");
            try
            {
                request.Method = "POST";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;
                request.ContentType = "application/json";
                var postData = "{\"status\":\"ISSUED\",\"vehicle\":{\"id\":"+cars+"},\"driver\":{\"id\":"+driver.Code+"}}";
                var data = Encoding.ASCII.GetBytes(postData);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                outputData = JObject.Parse(responseString).SelectToken("id").ToString();
            }
            catch (Exception e)
            {
                outputData = e.Message;
            }
            return outputData;
        }
        public async Task<Telegram.Bot.Types.InputFiles.InputOnlineFile> SaveWaybillPDF(Driver driver)
        {
            await Authenticate();
            HttpWebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/waybills/" + "840453" + "/pdf");
            try
            {
                request.Method = "POST";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;



                response = (HttpWebResponse)request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    var data = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, $"{driver.Waybill}.pdf");
                    return data;
                }
                //using (Stream output = File.OpenWrite($"{driver.Waybill}.pdf"))
                //using (Stream input = response.GetResponseStream())
                //{
                //    input.CopyTo(output);
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} - Ошибка в блоке получения файла путевого листа. Код - {e.Message}");
                return new Telegram.Bot.Types.InputFiles.InputOnlineFile(Stream.Null);
            }
           
        }
    public async Task<string> CreatePreMed(Driver driver)
        {
            var outputData = "";
            await Authenticate();
            HttpWebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/checkups/PRE_MED");
            try
            {
                request.Method = "POST";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;
                request.ContentType = "application/json";
                var postData = "{\"checkupData\": {\"bloodPressureDia\": \"70\",\"bloodPressureSys\": \"120\",\"bodyTemperature\": \"36\",\"alcoholTestPassed\": true},\"type\":\"PRE_MED\",\"specialist\": {\"id\": 37065},\"waybill\":{\"id\": "+driver.Waybill+"}}";
                var data = Encoding.ASCII.GetBytes(postData);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                outputData = "Медосмотр успешно пройден!";
            }
            catch (Exception e)
            {
                outputData = e.Message;
            }
            return outputData;
        }
        public async Task<string> CreatePreTech(Driver driver, string probeg)
        {
            var outputData = "";
            await Authenticate();
            HttpWebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/checkups/PRE_MED");
            try
            {
                request.Method = "POST";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;
                request.ContentType = "application/json";
                var postData = "{\"checkupData\": {\"desinfected\": true,\"odometerData\": \""+probeg+"\"},\"type\":\"PRE_TECH\",\"specialist\": {\"id\": 47176},\"waybill\":{\"id\": " + driver.Waybill + "}}";
                var data = Encoding.ASCII.GetBytes(postData);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                outputData = "Техосмотр успешно пройден!";
            }
            catch (Exception e)
            {
                outputData = e.Message;
            }
            return outputData;
        }
    }
}
