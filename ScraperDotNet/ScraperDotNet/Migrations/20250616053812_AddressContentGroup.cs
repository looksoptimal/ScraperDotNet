using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScraperDotNet.Migrations
{
    /// <inheritdoc />
    public partial class AddressContentGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentGroup",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentGroup",
                table: "Addresses");
        }
    }
}
