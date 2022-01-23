using Microsoft.EntityFrameworkCore.Migrations;

namespace OperatorBot.Migrations
{
    public partial class newcolumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "employerId",
                table: "Licenser",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "employerId",
                table: "Licenser");
        }
    }
}
