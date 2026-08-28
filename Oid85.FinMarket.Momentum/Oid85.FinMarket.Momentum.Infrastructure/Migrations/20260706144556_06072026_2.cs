using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oid85.FinMarket.Algo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _06072026_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResultMessage",
                schema: "public",
                table: "StrategyExecuteResultEntities",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultMessage",
                schema: "public",
                table: "StrategyExecuteResultEntities");
        }
    }
}
