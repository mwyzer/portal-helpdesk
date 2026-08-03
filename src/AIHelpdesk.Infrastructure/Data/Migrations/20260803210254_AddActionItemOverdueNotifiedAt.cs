using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdesk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActionItemOverdueNotifiedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueNotifiedAt",
                table: "ActionItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(3280));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4790));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4801));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4817));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4820));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4830));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4833));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4835));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4838));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000010"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4842));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000011"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4845));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000012"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4850));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000013"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4853));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000014"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4855));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000015"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 21, 2, 53, 170, DateTimeKind.Utc).AddTicks(4871));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverdueNotifiedAt",
                table: "ActionItems");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(92));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1595));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1606));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1609));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1612));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1632));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1635));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1638));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1640));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000010"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1644));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000011"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1647));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000012"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1650));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000013"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1652));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000014"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1658));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000015"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 3, 20, 48, 22, 122, DateTimeKind.Utc).AddTicks(1660));
        }
    }
}
