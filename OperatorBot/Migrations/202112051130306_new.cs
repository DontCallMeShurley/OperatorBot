namespace OperatorBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _new : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Drivers", "C_FIO", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Drivers", "C_FIO");
        }
    }
}
