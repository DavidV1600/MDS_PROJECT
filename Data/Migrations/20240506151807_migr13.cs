﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDS_PROJECT.Data.Migrations
{
    public partial class migr13 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Store",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Store",
                table: "Products");
        }
    }
}
