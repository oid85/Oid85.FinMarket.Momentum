using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oid85.FinMarket.Momentum.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _12092026_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StrategyExecuteResultEntities",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Ticker = table.Column<string>(type: "text", nullable: false),
                    StrategyDescription = table.Column<string>(type: "text", nullable: false),
                    StrategyName = table.Column<string>(type: "text", nullable: false),
                    StrategyParams = table.Column<string>(type: "text", nullable: false),
                    RecoveryFactor = table.Column<double>(type: "double precision", nullable: false),
                    NetProfit = table.Column<double>(type: "double precision", nullable: false),
                    MaxDrawdown = table.Column<double>(type: "double precision", nullable: false),
                    MaxDrawdownPercent = table.Column<double>(type: "double precision", nullable: false),
                    StartMoney = table.Column<double>(type: "double precision", nullable: false),
                    EndMoney = table.Column<double>(type: "double precision", nullable: false),
                    TotalReturn = table.Column<double>(type: "double precision", nullable: false),
                    AnnualYieldReturn = table.Column<double>(type: "double precision", nullable: false),
                    ResultMessage = table.Column<string>(type: "text", nullable: false),
                    EquityCurve = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyExecuteResultEntities", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StrategyExecuteResultEntities",
                schema: "public");
        }
    }
}
