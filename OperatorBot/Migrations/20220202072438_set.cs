using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OperatorBot.Migrations
{
    public partial class set : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Licenser",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msidn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    employerId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenser", x => x.ID);
                });

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

            migrationBuilder.CreateTable(
                name: "Driver",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Probeg = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    C_FIO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    licenser_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Waybill = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Driver", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Driver_Licenser_licenser_id",
                        column: x => x.licenser_id,
                        principalTable: "Licenser",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Driver_licenser_id",
                table: "Driver",
                column: "licenser_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Driver");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Licenser");
        }
    }
}
