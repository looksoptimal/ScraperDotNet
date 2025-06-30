using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrapperDotNet.Migrations
{
    /// <inheritdoc />
    public partial class CompressPageContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // fix the CompressedContent type 
            migrationBuilder.Sql(@"
alter table Pages
drop column [CompressedContent]
GO

alter table Pages
add [CompressedContent] [varbinary](max) NULL, [Content]  AS (CONVERT([nvarchar](max),Decompress([CompressedContent])))
");

            migrationBuilder.Sql(@"
CREATE PROCEDURE InsertPage
	@addressId bigint, 
	@content nvarchar(max)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	INSERT INTO Pages (Id, CompressedContent) 
	VALUES ( @addressId, COMPRESS(@content) )

	SELECT SCOPE_IDENTITY() AS [Id];
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
