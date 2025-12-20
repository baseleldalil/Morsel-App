using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WhatsApp.Shared.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceContactsAndCampaignsForRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Contacts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Contacts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomField1",
                table: "Contacts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomField2",
                table: "Contacts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomField3",
                table: "Contacts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "Contacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalRowIndex",
                table: "Contacts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNormalized",
                table: "Contacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneRaw",
                table: "Contacts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentContactId",
                table: "Campaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuplicatePreventionMode",
                table: "Campaigns",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FemaleContent",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastCompletedContactId",
                table: "Campaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaleContent",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PausedAtContactId",
                table: "Campaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedBrowser",
                table: "Campaigns",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdvancedTimingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    MinDelaySeconds = table.Column<double>(type: "double precision", precision: 18, scale: 2, nullable: false),
                    MaxDelaySeconds = table.Column<double>(type: "double precision", precision: 18, scale: 2, nullable: false),
                    EnableRandomBreaks = table.Column<bool>(type: "boolean", nullable: false),
                    MinMessagesBeforeBreak = table.Column<int>(type: "integer", nullable: false),
                    MaxMessagesBeforeBreak = table.Column<int>(type: "integer", nullable: false),
                    MinBreakMinutes = table.Column<double>(type: "double precision", precision: 18, scale: 2, nullable: false),
                    MaxBreakMinutes = table.Column<double>(type: "double precision", precision: 18, scale: 2, nullable: false),
                    UseDecimalRandomization = table.Column<bool>(type: "boolean", nullable: false),
                    DecimalPrecision = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvancedTimingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SentPhoneNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SendCount = table.Column<int>(type: "integer", nullable: false),
                    LastCampaignId = table.Column<int>(type: "integer", nullable: true),
                    LastStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentPhoneNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SentPhoneNumbers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_AdvancedTimingSettings_UserId",
                table: "AdvancedTimingSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SentPhoneNumbers_PhoneNumber",
                table: "SentPhoneNumbers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SentPhoneNumbers_UserId_PhoneNumber",
                table: "SentPhoneNumbers",
                columns: new[] { "UserId", "PhoneNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvancedTimingSettings");

            migrationBuilder.DropTable(
                name: "SentPhoneNumbers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "CustomField1",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "CustomField2",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "CustomField3",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "OriginalRowIndex",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "PhoneNormalized",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "PhoneRaw",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "CurrentContactId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "DuplicatePreventionMode",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "FemaleContent",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "LastCompletedContactId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "MaleContent",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "PausedAtContactId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "SelectedBrowser",
                table: "Campaigns");

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
        }
    }
}
