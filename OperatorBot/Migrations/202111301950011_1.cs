namespace OperatorBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _1 : DbMigration
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
                        licenser_ID = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Licensers", t => t.licenser_ID)
                .Index(t => t.licenser_ID);
            
            CreateTable(
                "dbo.Licensers",
                c => new
                    {
                        ID = c.String(nullable: false, maxLength: 128),
                        Name = c.String(),
                        msidn = c.String(),
                        password = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Drivers", "licenser_ID", "dbo.Licensers");
            DropIndex("dbo.Drivers", new[] { "licenser_ID" });
            DropTable("dbo.Licensers");
            DropTable("dbo.Drivers");
        }
    }
}
