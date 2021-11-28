using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;//Пакет JSON
using OperatorBot;

namespace OperatorBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Responser responser = new Responser("9519997810", "31spFKX5", "37065");
            responser.Authenticate() ;
        }
    }
}
