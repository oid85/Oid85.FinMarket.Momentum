using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oid85.FinMarket.Algo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _07072026_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalNetProfit",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "NetProfit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NetProfit",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "TotalNetProfit");
        }
    }
}
