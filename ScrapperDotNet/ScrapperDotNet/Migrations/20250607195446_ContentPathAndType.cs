using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrapperDotNet.Migrations
{
    /// <inheritdoc />
    public partial class ContentPathAndType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the computed column first
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Pages");

            migrationBuilder.AlterColumn<byte[]>(
                name: "CompressedContent",
                table: "Pages",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Re-add the computed column (adjust the SQL expression as needed)
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true,
                computedColumnSql: "CONVERT([nvarchar](max),Decompress([CompressedContent]))",
                stored: false);

            migrationBuilder.AddColumn<string>(
                name: "ContentPath",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentType",
                table: "Pages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentPath",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Pages");

            // Drop the computed column
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Pages");

            migrationBuilder.AlterColumn<string>(
                name: "CompressedContent",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            // Re-add the computed column (adjust the SQL expression as needed)
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true,
                computedColumnSql: "CONVERT([nvarchar](max),Decompress([CompressedContent]))",
                stored: false);
        }
    }
}
