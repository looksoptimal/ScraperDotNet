using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrapperDotNet.Migrations
{
    /// <inheritdoc />
    public partial class PageIdOutputParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER PROCEDURE InsertPage
	@addressId bigint, 
	@content nvarchar(max),
	@pageId bigint OUTPUT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	INSERT INTO Pages (AddressId, CompressedContent, Downloaded) 
	VALUES ( @addressId, COMPRESS(@content), GETUTCDATE())

    -- Insert statements for procedure here
	SET @pageId = SCOPE_IDENTITY();
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
