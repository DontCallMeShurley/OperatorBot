using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace OperatorBot.Models
{
    public class Driver
    {
        public Guid ID { get; set; }
        public string Code { get; set; }
        public string Probeg { get; set; }
        public string UserName { get; set; }
        public string C_FIO { get; set; }
        public Licenser licenser { get; set; }
        public string licenser_id { get; set; }
    }
}
