using Microsoft.EntityFrameworkCore.Migrations;

namespace OperatorBot.Migrations
{
    public partial class waybill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Waybill",
                table: "Driver",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Waybill",
                table: "Driver");
        }
    }
}
