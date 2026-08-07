using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdesk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkEmbeddingVectorAndDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "KnowledgeChunks",
                type: "uuid",
                nullable: true);

            // Backfill from the parent document so future writes (IndexDocumentInternalAsync)
            // and the HNSW index below don't need a join to KnowledgeDocuments to filter by department.
            migrationBuilder.Sql(@"
                UPDATE ""KnowledgeChunks"" kc
                SET ""DepartmentId"" = kd.""DepartmentId""
                FROM ""KnowledgeDocuments"" kd
                WHERE kd.""Id"" = kc.""DocumentId"";
            ");

            // Native pgvector column alongside the legacy EmbeddingJson text column (kept for one
            // release as a fallback/audit trail -- drop EmbeddingJson in a follow-up migration once
            // this is verified in production). Dimension matches AIOptions.EmbeddingModel
            // (text-embedding-3-small = 1536); changing the embedding model requires a new column
            // and a full re-embed, not just a migration.
            migrationBuilder.Sql(@"ALTER TABLE ""KnowledgeChunks"" ADD COLUMN ""Embedding"" vector(1536);");
            migrationBuilder.Sql(@"UPDATE ""KnowledgeChunks"" SET ""Embedding"" = ""EmbeddingJson""::vector WHERE ""EmbeddingJson"" IS NOT NULL AND ""EmbeddingJson"" != '[]';");

            // CREATE INDEX CONCURRENTLY cannot run inside a transaction -- suppressTransaction
            // pulls just this command out of the migration's ambient transaction.
            migrationBuilder.Sql(
                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_knowledgechunks_embedding_hnsw
                  ON ""KnowledgeChunks"" USING hnsw (""Embedding"" vector_cosine_ops)
                  WITH (m = 16, ef_construction = 64);",
                suppressTransaction: true);

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(4685));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6518));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6535));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6538));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6541));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6551));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6554));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6557));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6567));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000010"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6572));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000011"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6576));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000012"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6600));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000013"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6603));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000014"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6606));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000015"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 7, 20, 37, 22, 155, DateTimeKind.Utc).AddTicks(6610));

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_DepartmentId",
                table: "KnowledgeChunks",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS ix_knowledgechunks_embedding_hnsw;",
                suppressTransaction: true);

            migrationBuilder.Sql(@"ALTER TABLE ""KnowledgeChunks"" DROP COLUMN IF EXISTS ""Embedding"";");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeChunks_DepartmentId",
                table: "KnowledgeChunks");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "KnowledgeChunks");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(3926));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6798));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6823));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6846));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6852));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6872));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6876));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6880));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6885));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000010"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6891));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000011"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6895));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000012"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6905));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000013"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6910));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000014"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6915));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000015"),
                column: "UpdatedAt",
                value: new DateTime(2026, 8, 4, 17, 50, 2, 485, DateTimeKind.Utc).AddTicks(6919));
        }
    }
}
