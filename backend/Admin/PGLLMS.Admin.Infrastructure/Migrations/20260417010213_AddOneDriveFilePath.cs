using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGLLMS.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOneDriveFilePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OneDriveFilePath",
                table: "ChapterContents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OneDriveFilePath",
                table: "ChapterContents");
        }
    }
}
