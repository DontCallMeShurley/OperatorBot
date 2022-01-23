using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace OperatorBot.Models
{
    public class Licenser
    {
        public string Name { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ID { get; set; }
        public string msidn { get; set; }
        public string password { get; set; }
        public string employerId { get; set; }
        public List<Driver> drivers { get; set; }
    }
}
