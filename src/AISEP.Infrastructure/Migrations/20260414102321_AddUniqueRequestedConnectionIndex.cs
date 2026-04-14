using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueRequestedConnectionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StartupInvestorConnections_StartupID",
                table: "StartupInvestorConnections");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_Unique_Requested",
                table: "StartupInvestorConnections",
                columns: new[] { "StartupID", "InvestorID" },
                unique: true,
                filter: "\"ConnectionStatus\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Connections_Unique_Requested",
                table: "StartupInvestorConnections");

            migrationBuilder.CreateIndex(
                name: "IX_StartupInvestorConnections_StartupID",
                table: "StartupInvestorConnections",
                column: "StartupID");
        }
    }
}
