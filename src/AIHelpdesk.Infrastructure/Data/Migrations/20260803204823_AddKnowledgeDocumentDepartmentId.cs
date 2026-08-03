using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdesk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeDocumentDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "KnowledgeDocuments",
                type: "uuid",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_DepartmentId",
                table: "KnowledgeDocuments",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeDocuments_Departments_DepartmentId",
                table: "KnowledgeDocuments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeDocuments_Departments_DepartmentId",
                table: "KnowledgeDocuments");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeDocuments_DepartmentId",
                table: "KnowledgeDocuments");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "KnowledgeDocuments");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 915, DateTimeKind.Utc).AddTicks(9835));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(4943));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(4992));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5033));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5049));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5073));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5088));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5100));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5111));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000010"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5126));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000011"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5140));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000012"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5209));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000013"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5227));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000014"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5242));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000015"),
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 20, 13, 33, 9, 916, DateTimeKind.Utc).AddTicks(5277));
        }
    }
}
