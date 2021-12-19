using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatorBot.Models
{
   public class Cars
    {
        public Cars(int ID, string ShortName)
        {
            this.ID = ID;
            this.ShortName = ShortName;
        }
        public int ID { get; set; }
        public string ShortName { get; set; }
    }
}
