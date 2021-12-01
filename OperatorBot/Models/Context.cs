using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Text;

namespace OperatorBot.Models
{
    public class Context : DbContext
    {
        public DbSet<Driver> Driver { get; set; }
        public DbSet<Licenser> Licenser { get; set; }
    }
}
