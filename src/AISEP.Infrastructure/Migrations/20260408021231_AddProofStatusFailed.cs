using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProofStatusFailed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "ProofStatus",
                table: "DocumentBlockchainProofs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "ProofStatus",
                table: "DocumentBlockchainProofs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)2);
        }
    }
}
