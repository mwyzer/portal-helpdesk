using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdesk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedAgentId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_DepartmentId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_AssignedToId",
                table: "ActionItems");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 832, DateTimeKind.Utc).AddTicks(9309));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(1989));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2010));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2037));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2043));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2054));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2059));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2064));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2070));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000010"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2077));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000011"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2082));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000012"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2092));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000013"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2097));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000014"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2102));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000015"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2106));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000016"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 25, 15, 9, 56, 833, DateTimeKind.Utc).AddTicks(2112));

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedAgentId_Status",
                table: "Tickets",
                columns: new[] { "AssignedAgentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DepartmentId_Status",
                table: "Tickets",
                columns: new[] { "DepartmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status_EmployeeId",
                table: "LeaveRequests",
                columns: new[] { "Status", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_AssignedToId_Status_DueDate",
                table: "ActionItems",
                columns: new[] { "AssignedToId", "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedAgentId_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_DepartmentId_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_Status_EmployeeId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_AssignedToId_Status_DueDate",
                table: "ActionItems");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 630, DateTimeKind.Utc).AddTicks(7623));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(9));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(27));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(46));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(50));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(62));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(67));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(72));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(77));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000010"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(83));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000011"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(89));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000012"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(155));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000013"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(161));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000014"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(187));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000015"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(198));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000016"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 8, 16, 7, 20, 631, DateTimeKind.Utc).AddTicks(203));

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedAgentId",
                table: "Tickets",
                column: "AssignedAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DepartmentId",
                table: "Tickets",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_AssignedToId",
                table: "ActionItems",
                column: "AssignedToId");
        }
    }
}
