using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsApp.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddContactAttachmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "Contacts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "Contacts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AttachmentSize",
                table: "Contacts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "Contacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttachmentUploadedAt",
                table: "Contacts",
                type: "timestamp with time zone",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "AttachmentSize",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "AttachmentUploadedAt",
                table: "Contacts");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 7, 21, 40, 19, 595, DateTimeKind.Utc).AddTicks(4425));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 7, 21, 40, 19, 595, DateTimeKind.Utc).AddTicks(4719));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 7, 21, 40, 19, 595, DateTimeKind.Utc).AddTicks(4720));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 7, 21, 40, 19, 595, DateTimeKind.Utc).AddTicks(4722));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 7, 21, 40, 19, 595, DateTimeKind.Utc).AddTicks(4724));
        }
    }
}
