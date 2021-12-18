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
        public string GetFIOorError(string IdEmployer)
        {
            this.Authenticate();
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
        public async void Authenticate()
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

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                if (!string.IsNullOrEmpty(employer))
                {
                    BToken = JObject.Parse(responseString).SelectToken("token").ToString();
                    Console.WriteLine($"Получен BToken: {0} ", BToken);
                }
                else
                {
                    employer = JArray.Parse(responseString)[0].SelectToken("employeeId").ToString();
                    Console.WriteLine($"Получен employeeId: {0} ", employer);
                    //Выполнить через 2 секунды, чтобы ошибку не словить
                    await Task.Delay(2000);
                    Authenticate();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка в блоке получения кода работника или BTokena. Возможно, слишком часто стучимся к API КИС АРТ. Обратитесь к разработчику. Код ошибки - {e.Message}");
            }

        }
    }
}
