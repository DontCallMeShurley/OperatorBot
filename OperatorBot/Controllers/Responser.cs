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
            Authenticate().Wait();
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
            catch (Exception e)
            {
                C_FIO = "403. Ошибка. Вы не найдены в системе, или система не отвечает, или данный водитель не принадлежит выбранному перевозчику. Повторите попытку, введя корректные данные";
            }
            return C_FIO;
        }
        public async Task Authenticate()
        {
            Task.Delay(1000).Wait();
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

            //буду проверять BToken на предмет возможности отправки запроса
            if (BToken != null && employer != null)
            {
                try
                {
                    var request1 = WebRequest.Create($"https://art.taxi.mos.ru/api/employees/");
                    request1.Method = "GET";
                    request1.Headers.Add("Authorization", $"{BToken}");
                    request1.PreAuthenticate = true;
                    var res = (HttpWebResponse)request1.GetResponseAsync().Result;
                    return;
                }
                catch (Exception e)
                {
                }
            }
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                
                if (!string.IsNullOrEmpty(employer))
                {
                    BToken = JObject.Parse(responseString).SelectToken("token").ToString();
                    Console.WriteLine($"{DateTime.Now} - Получен BToken");
                }
                else
                {
                    employer = JArray.Parse(responseString)[0].SelectToken("employeeId").ToString();
                    Console.WriteLine($"{DateTime.Now} - Получен employeeId ");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}- Ошибка в блоке получения кода работника или BTokena. Обратитесь к разработчику. Код ошибки - {e.Message}");
                throw new Exception($"{DateTime.Now}- Ошибка в блоке получения кода работника или BTokena.  Обратитесь к разработчику. Код ошибки - {e.Message}");
            }
            //Выполнить через 2 секунды, чтобы ошибку не словить
            if (BToken == null)
            {
                Task.Delay(1000).Wait();
                Authenticate().Wait();
            }

        }
        public async Task<List<Cars>> GetCarsAsync()
        {
            Authenticate().Wait();
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
                foreach (var b in a)
                {
                    outputData.Add(new Cars(Convert.ToInt32(b.SelectToken("id").ToString()), b.SelectToken("shortName").ToString()));
                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} -  Непредвиденная ошибка при попытке получить список доступных машин. Обратитесь к разработчику");
                throw new Exception($"{DateTime.Now} -  Непредвиденная ошибка при попытке получить список доступных машин. Обратитесь к разработчику. Код ошибки - {e.Message}");
            }
            return outputData;
        }
        //Вернёт ошибку или ID созданного путевого листа
        public async Task<string> CreateWaybills(Driver driver, string cars)
        {
            Authenticate().Wait();
            string outputData = "";
            HttpWebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/waybills");
            try
            {
                request.Method = "POST";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;
                request.ContentType = "application/json";
                var postData = "{\"status\":\"ISSUED\",\"vehicle\":{\"id\":" + cars + "},\"driver\":{\"id\":" + driver.Code + "}}";
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

        //Получение ИД путевого листа
        public async Task<string> GetWaybill(Driver driver)
        {
            Authenticate().Wait();
            HttpWebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/waybills?search=" + driver.C_FIO.Substring(0, driver.C_FIO.IndexOf(" ")) + "&" + driver.C_FIO.Substring(driver.C_FIO.IndexOf(" ") + 1, driver.C_FIO.IndexOf(" ", driver.C_FIO.IndexOf(" ")) + 1));
            //var outputData = new List<Cars>();
            var outputData = "";
            try
            {
                request.Method = "GET";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;

                response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                //Если найдено 0 путевых, то возвращаем -1
                if (JObject.Parse(responseString).SelectToken("total").ToString() == "0")
                {
                    return "-1";
                }
                var a = JArray.Parse(JObject.Parse(responseString).SelectToken("entries").ToString());
                foreach (var b in a)
                {
                    outputData = b.SelectToken("id").ToString();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} -  Непредвиденная ошибка при попытке получить ID путевого листа. Обратитесь к разработчику. Код ошибки - {e.Message}");
            }
            return outputData;
        }
        //Получение путевого листа в файл
        public async Task<string> SaveWaybillPDF(Driver driver)
        {
            Authenticate().Wait();
            WebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/waybills/" + driver.Waybill + "/pdf");
            try
            {
                request.Method = "GET";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;
                request.Timeout = 3000;
                response = request.GetResponse();
                var remoteStream = response.GetResponseStream();
                string FileName = $"{DateTime.Now.Month}.{DateTime.Now.Day}-{driver.C_FIO}-{driver.Waybill}.pdf";
                var localStream = File.Create(FileName);
                int bytesProcessed = 0;
                byte[] buffer = new byte[1024];

                //перевожу ответ в стрим и запихиваю его в файл
                int bytesRead;
                do
                {
                    bytesRead = remoteStream.Read(buffer, 0, buffer.Length);
                    localStream.Write(buffer, 0, bytesRead);
                    bytesProcessed += bytesRead;
                } while (bytesRead > 0);

                localStream.Flush();
                localStream.Close();
                Console.WriteLine("test");
                return FileName;

            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} - Ошибка в блоке получения файла путевого листа. Код - {e.Message}");
                return "";
            }

        }

        public async Task DeleteWaybill(Driver driver)
        {
            Authenticate().Wait();
            WebResponse response;
            WebRequest request = WebRequest.Create($"https://art.taxi.mos.ru/api/waybills/" + driver.Waybill);
            try
            {
                request.Method = "DELETE";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;
                request.Timeout = 3000;
                response = request.GetResponse();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} - Ошибка в блоке получения файла путевого листа. Код - {e.Message}");
                throw new Exception($"{DateTime.Now} - Ошибка в блоке получения файла путевого листа. Код - {e.Message}");
            }

        }

        public async Task<string> CreateMed(Driver driver, bool B_Post)
        {
            var outputData = "";
            Authenticate().Wait();
            HttpWebResponse response;
            WebRequest request;
            if (!B_Post)
                request = WebRequest.Create($"https://art.taxi.mos.ru/api/checkups/PRE_MED");
            else
                request = WebRequest.Create($"https://art.taxi.mos.ru/api/checkups/POST_MED");
            try
            {
                request.Method = "POST";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;
                request.ContentType = "application/json";
                string postData = "";

                if (!B_Post)
                    postData = "{\"checkupData\": {\"bloodPressureDia\": \"70\",\"bloodPressureSys\": \"120\",\"bodyTemperature\": \"36\",\"alcoholTestPassed\": true},\"type\":\"PRE_MED\",\"specialist\": {\"id\": 37065},\"waybill\":{\"id\": " + driver.Waybill + "}}";
                else
                    postData = "{\"checkupData\": {\"bloodPressureDia\": \"70\",\"bloodPressureSys\": \"120\",\"bodyTemperature\": \"36\",\"alcoholTestPassed\": true},\"type\":\"POST_MED\",\"specialist\": {\"id\": 37065},\"waybill\":{\"id\": " + driver.Waybill + "}}";

                var data = Encoding.ASCII.GetBytes(postData);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                if (!B_Post)
                    outputData = "Пререйсовый медосмотр успешно пройден!";
                else
                    outputData = "Послерейсовый медосмотр успешно пройден!";
            }
            catch (Exception e)
            {
                outputData = e.Message;
            }
            return outputData;
        }
        public async Task<string> CreateTech(Driver driver, string probeg, bool B_Post)
        {
            var outputData = "";
            Authenticate().Wait();
            Task.Delay(6000).Wait();
            WebRequest request;
            HttpWebResponse response;
            if (!B_Post)
                request = WebRequest.Create($"https://art.taxi.mos.ru/api/checkups/PRE_TECH");
            else
                request = WebRequest.Create($"https://art.taxi.mos.ru/api/checkups/POST_TECH");
            try
            {
                request.Method = "POST";
                request.Headers.Add("Authorization", $"{BToken}");
                request.PreAuthenticate = true;
                request.ContentType = "application/json";
                string postData = "";
                //Если параметр передаётся не послерейсовый, то и бади другой будет
                if (!B_Post)
                    postData = "{\"checkupData\": {\"desinfected\": true,\"odometerData\": \"" + probeg + "\"},\"type\":\"PRE_TECH\",\"specialist\": {\"id\": 47176},\"waybill\":{\"id\": " + driver.Waybill + "}}";
                else
                    postData = "{\"checkupData\": {\"desinfected\": true,\"odometerData\": \"" + probeg + "\"},\"type\":\"POST_TECH\",\"specialist\": {\"id\": 47176},\"waybill\":{\"id\": " + driver.Waybill + "}}";

                var data = Encoding.ASCII.GetBytes(postData);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                if (!B_Post)
                    outputData = "Пререйсовый техосмотр успешно пройден!";
                else
                    outputData = "Послерейсовый техосмотр успешно пройден!";
            }
            catch (Exception e)
            {
                outputData = e.Message;
            }
            return outputData;
        }
    }
}
