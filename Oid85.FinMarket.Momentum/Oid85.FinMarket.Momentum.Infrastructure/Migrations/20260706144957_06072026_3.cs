using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oid85.FinMarket.Algo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _06072026_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AverageProfitPercent",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "AverageNetProfitPercent");

            migrationBuilder.RenameColumn(
                name: "AverageProfit",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "AverageNetProfit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AverageNetProfitPercent",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "AverageProfitPercent");

            migrationBuilder.RenameColumn(
                name: "AverageNetProfit",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "AverageProfit");
        }
    }
}
