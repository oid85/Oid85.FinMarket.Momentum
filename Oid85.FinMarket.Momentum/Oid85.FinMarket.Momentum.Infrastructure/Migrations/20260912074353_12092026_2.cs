using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oid85.FinMarket.Momentum.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _12092026_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StrategyDescription",
                schema: "public",
                table: "StrategyExecuteResultEntities");

            migrationBuilder.RenameColumn(
                name: "Ticker",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "Tickers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tickers",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                newName: "Ticker");

            migrationBuilder.AddColumn<string>(
                name: "StrategyDescription",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
