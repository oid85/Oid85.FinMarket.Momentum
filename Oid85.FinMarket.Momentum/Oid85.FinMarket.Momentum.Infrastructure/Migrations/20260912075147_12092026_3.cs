using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oid85.FinMarket.Momentum.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _12092026_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxDrawdown",
                schema: "public",
                table: "StrategyExecuteResultEntities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaxDrawdown",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
