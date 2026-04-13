using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGLLMS.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedToCourseVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "CourseVersions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "CourseVersions");
        }
    }
}
