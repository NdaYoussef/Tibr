using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tibr.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxBudgetEgp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxBudgetEgp",
                table: "OrdersInvestments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxBudgetEgp",
                table: "OrdersInvestments");
        }
    }
}
