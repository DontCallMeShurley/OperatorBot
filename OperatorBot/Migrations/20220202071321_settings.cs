using Microsoft.EntityFrameworkCore.Migrations;

namespace OperatorBot.Migrations
{
    public partial class settings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    mechMsdisn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    mechPass = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    medMsdisn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    medPass = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    botToken = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
