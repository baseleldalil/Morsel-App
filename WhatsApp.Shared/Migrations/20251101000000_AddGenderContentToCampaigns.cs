using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsApp.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderContentToCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaleContent",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FemaleContent",
                table: "Campaigns",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaleContent",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "FemaleContent",
                table: "Campaigns");
        }
    }
}
