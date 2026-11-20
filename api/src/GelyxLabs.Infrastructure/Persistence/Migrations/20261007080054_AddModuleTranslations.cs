using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GelyxLabs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "translations",
                schema: "public",
                table: "modules",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "translations",
                schema: "public",
                table: "modules");
        }
    }
}
