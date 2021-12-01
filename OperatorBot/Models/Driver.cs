using System;
using System.Collections.Generic;
using System.Text;

namespace OperatorBot.Models
{
    public class Driver
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Probeg { get; set; }
        public string UserName { get; set; }
        public Licenser licenser { get; set;}
    }
}
