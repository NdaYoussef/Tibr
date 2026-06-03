using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tibr.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditStockTypo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Stock",
                table: "Products",
                type: "bigint",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Stock",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldPrecision: 18,
                oldScale: 4);
        }
    }
}
