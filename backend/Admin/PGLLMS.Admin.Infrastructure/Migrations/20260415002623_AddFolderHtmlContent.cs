using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGLLMS.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderHtmlContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HtmlContent",
                table: "Folders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HtmlContent",
                table: "Folders");
        }
    }
}
