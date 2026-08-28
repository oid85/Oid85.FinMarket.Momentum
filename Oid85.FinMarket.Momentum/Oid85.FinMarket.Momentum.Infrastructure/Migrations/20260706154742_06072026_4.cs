using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oid85.FinMarket.Algo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _06072026_4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NetProfit",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "TotalNetProfit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalNetProfit",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "NetProfit");
        }
    }
}
