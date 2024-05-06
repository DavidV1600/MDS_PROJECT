using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDS_PROJECT.Data.Migrations
{
    public partial class migr18 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Searched",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Searched",
                table: "Products");
        }
    }
}
