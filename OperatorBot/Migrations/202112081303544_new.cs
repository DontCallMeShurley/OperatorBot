namespace OperatorBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _new : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Drivers",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Code = c.String(),
                        Probeg = c.String(),
                        UserName = c.String(),
                        C_FIO = c.String(),
                        licenser_ID = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Licensers", t => t.licenser_ID)
                .Index(t => t.licenser_ID);
            
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Drivers", "licenser_ID", "dbo.Licensers");
            DropIndex("dbo.Drivers", new[] { "licenser_ID" });
            DropTable("dbo.Drivers");
        }
    }
}
