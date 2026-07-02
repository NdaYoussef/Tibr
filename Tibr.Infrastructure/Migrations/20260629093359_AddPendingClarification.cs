using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tibr.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingClarification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingClarification",
                table: "ChatConversations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingClarification",
                table: "ChatConversations");
        }
    }
}
