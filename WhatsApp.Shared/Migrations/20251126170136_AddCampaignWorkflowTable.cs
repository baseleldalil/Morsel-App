using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WhatsApp.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignWorkflowTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampaignWorkflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    ContactId = table.Column<int>(type: "integer", nullable: false),
                    WorkflowStatus = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClickedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignWorkflows_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignWorkflows_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 1, 33, 287, DateTimeKind.Utc).AddTicks(5978));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 1, 33, 287, DateTimeKind.Utc).AddTicks(6457));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 1, 33, 287, DateTimeKind.Utc).AddTicks(6460));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 1, 33, 287, DateTimeKind.Utc).AddTicks(6462));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 1, 33, 287, DateTimeKind.Utc).AddTicks(6465));

            migrationBuilder.CreateIndex(
                name: "IDX_CampaignWorkflows_AddedAt",
                table: "CampaignWorkflows",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IDX_CampaignWorkflows_CampaignContact",
                table: "CampaignWorkflows",
                columns: new[] { "CampaignId", "ContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_CampaignWorkflows_CampaignId",
                table: "CampaignWorkflows",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IDX_CampaignWorkflows_ContactId",
                table: "CampaignWorkflows",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IDX_CampaignWorkflows_Status",
                table: "CampaignWorkflows",
                column: "WorkflowStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignWorkflows");

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
        }
    }
}
