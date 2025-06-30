using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScraperDotNet.Migrations
{
    /// <inheritdoc />
    public partial class UrlSplitted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "Addresses");

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "Addresses",
                type: "nvarchar(253)",
                maxLength: 253,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "Addresses",
                type: "nvarchar(2083)",
                maxLength: 2083,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "Addresses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QueryString",
                table: "Addresses",
                type: "nvarchar(2083)",
                maxLength: 2083,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scheme",
                table: "Addresses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Domain",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "QueryString",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Scheme",
                table: "Addresses");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
