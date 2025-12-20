using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WhatsApp.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingContactColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SentPhones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MessageContent = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UserId1 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SentPhones_AspNetUsers_UserId1",
                        column: x => x.UserId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SentPhones_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 19, 19, 3, 401, DateTimeKind.Utc).AddTicks(7746));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 19, 19, 3, 401, DateTimeKind.Utc).AddTicks(8021));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 19, 19, 3, 401, DateTimeKind.Utc).AddTicks(8023));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 19, 19, 3, 401, DateTimeKind.Utc).AddTicks(8024));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 19, 19, 3, 401, DateTimeKind.Utc).AddTicks(8025));

            migrationBuilder.CreateIndex(
                name: "IDX_SentPhones_CampaignId",
                table: "SentPhones",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IDX_SentPhones_SentAt",
                table: "SentPhones",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IDX_SentPhones_UserId",
                table: "SentPhones",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IDX_SentPhones_UserPhone",
                table: "SentPhones",
                columns: new[] { "UserId", "PhoneNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SentPhones_UserId1",
                table: "SentPhones",
                column: "UserId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SentPhones");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 12, 43, 52, 669, DateTimeKind.Utc).AddTicks(6866));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 12, 43, 52, 669, DateTimeKind.Utc).AddTicks(7172));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 12, 43, 52, 669, DateTimeKind.Utc).AddTicks(7174));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 12, 43, 52, 669, DateTimeKind.Utc).AddTicks(7177));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 8, 12, 43, 52, 669, DateTimeKind.Utc).AddTicks(7178));
        }
    }
}
