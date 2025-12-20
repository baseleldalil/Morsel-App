using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WhatsApp.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CampaignId1",
                table: "MessageHistory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "Contacts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CampaignTemplateId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalContacts = table.Column<int>(type: "integer", nullable: false),
                    MessagesSent = table.Column<int>(type: "integer", nullable: false),
                    MessagesDelivered = table.Column<int>(type: "integer", nullable: false),
                    MessagesFailed = table.Column<int>(type: "integer", nullable: false),
                    CurrentProgress = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StoppedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    MessageContent = table.Column<string>(type: "text", nullable: true),
                    UseGenderTemplates = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campaigns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Campaigns_CampaignTemplates_CampaignTemplateId",
                        column: x => x.CampaignTemplateId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 19, 4, 1, 115, DateTimeKind.Utc).AddTicks(940));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 19, 4, 1, 115, DateTimeKind.Utc).AddTicks(1217));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 19, 4, 1, 115, DateTimeKind.Utc).AddTicks(1219));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 19, 4, 1, 115, DateTimeKind.Utc).AddTicks(1220));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 19, 4, 1, 115, DateTimeKind.Utc).AddTicks(1222));

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistory_CampaignId1",
                table: "MessageHistory",
                column: "CampaignId1");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_CampaignId",
                table: "Contacts",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CampaignTemplateId",
                table: "Campaigns",
                column: "CampaignTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CreatedAt",
                table: "Campaigns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_UserId_Status",
                table: "Campaigns",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_Campaigns_CampaignId",
                table: "Contacts",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MessageHistory_Campaigns_CampaignId1",
                table: "MessageHistory",
                column: "CampaignId1",
                principalTable: "Campaigns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_Campaigns_CampaignId",
                table: "Contacts");

            migrationBuilder.DropForeignKey(
                name: "FK_MessageHistory_Campaigns_CampaignId1",
                table: "MessageHistory");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_MessageHistory_CampaignId1",
                table: "MessageHistory");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_CampaignId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "CampaignId1",
                table: "MessageHistory");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Contacts");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3243));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3599));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3601));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3603));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3604));
        }
    }
}
