using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsApp.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowMessageAndAttachmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentBase64",
                table: "CampaignWorkflows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentContentType",
                table: "CampaignWorkflows",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "CampaignWorkflows",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AttachmentSize",
                table: "CampaignWorkflows",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "CampaignWorkflows",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FemaleMessage",
                table: "CampaignWorkflows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaleMessage",
                table: "CampaignWorkflows",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 18, 25, 772, DateTimeKind.Utc).AddTicks(9893));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 18, 25, 773, DateTimeKind.Utc).AddTicks(197));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 18, 25, 773, DateTimeKind.Utc).AddTicks(199));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 18, 25, 773, DateTimeKind.Utc).AddTicks(201));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 26, 17, 18, 25, 773, DateTimeKind.Utc).AddTicks(203));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentBase64",
                table: "CampaignWorkflows");

            migrationBuilder.DropColumn(
                name: "AttachmentContentType",
                table: "CampaignWorkflows");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "CampaignWorkflows");

            migrationBuilder.DropColumn(
                name: "AttachmentSize",
                table: "CampaignWorkflows");

            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "CampaignWorkflows");

            migrationBuilder.DropColumn(
                name: "FemaleMessage",
                table: "CampaignWorkflows");

            migrationBuilder.DropColumn(
                name: "MaleMessage",
                table: "CampaignWorkflows");

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
        }
    }
}
