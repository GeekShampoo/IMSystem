using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMSystem.Server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserBlockEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBlocks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BlockedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerUserId_BlockedUserId",
                table: "UserBlocks",
                columns: new[] { "BlockerUserId", "BlockedUserId" },
                unique: true);
        }
    }
}
