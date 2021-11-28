using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;//Пакет JSON
using Newtonsoft.Json.Linq;

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
        public void Authenticate()
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

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }


            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            if (!string.IsNullOrEmpty(employer))
                BToken = JObject.Parse(responseString).SelectToken("token").ToString().Replace("Bearer ","");
            else
            {
               // var a = JObject.Parse(responseString).SelectToken("token").ToString();
            }
            Console.WriteLine(responseString);
            Console.ReadLine();
        }
    }
}
