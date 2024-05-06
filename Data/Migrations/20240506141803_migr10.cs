using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDS_PROJECT.Data.Migrations
{
    public partial class migr10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Products",
                newName: "QuantityTag");

            migrationBuilder.RenameColumn(
                name: "Subtitle",
                table: "Products",
                newName: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuantityTag",
                table: "Products",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Products",
                newName: "Subtitle");
        }
    }
}
