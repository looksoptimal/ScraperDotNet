using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScraperDotNet.Migrations
{
    /// <inheritdoc />
    public partial class ChangingSqlTypeForEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Rename Status to Status1
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Addresses",
                newName: "Status1");

            // 2. Add new Status column with default value 'Fresh'
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Addresses",
                type: "nvarchar(30)",
                nullable: false,
                defaultValue: "Fresh");

            // 3. Update Status to 'Duplicate' where Status1 == 3
            migrationBuilder.Sql(
                "UPDATE Addresses SET Status = 'Duplicate' WHERE Status1 = 3");

            // 4. Update Status to 'ErrorOnPage' where Status1 == 6
            migrationBuilder.Sql(
                "UPDATE Addresses SET Status = 'ErrorOnPage' WHERE Status1 = 6");

            // 5. Drop Status1 column
            migrationBuilder.DropColumn(
                name: "Status1",
                table: "Addresses");



            // 1. Rename Status to Status1
            migrationBuilder.RenameColumn(
                name: "ContentType",
                table: "Pages",
                newName: "ContentType1");

            // 2. Add new ContentType column with default value 'Fresh'
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Pages",
                type: "nvarchar(30)",
                nullable: false,
                defaultValue: "Html");

            // 5. Drop ContentType1 column
            migrationBuilder.DropColumn(
                name: "ContentType1",
                table: "Pages");


            migrationBuilder.Sql(@"
ALTER PROCEDURE InsertPage
	@addressId bigint, 
	@content nvarchar(max),
	@contentType nvarchar(30),
	@pageId bigint OUTPUT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	INSERT INTO Pages (AddressId, CompressedContent, Downloaded, ContentType) 
	VALUES ( @addressId, COMPRESS(@content), GETUTCDATE(), @contentType)

    -- Insert statements for procedure here
	SET @pageId = SCOPE_IDENTITY();
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ContentType",
                table: "Pages",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Addresses",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)");
        }
    }
}
