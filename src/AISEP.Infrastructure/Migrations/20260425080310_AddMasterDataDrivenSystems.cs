using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDataDrivenSystems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "SubIndustry",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "InvestorStageFocuses");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "InvestorIndustryFocuses");

            migrationBuilder.RenameColumn(
                name: "PreferredStages",
                table: "InvestorPreferences",
                newName: "PreferredStageIDs");

            migrationBuilder.RenameColumn(
                name: "PreferredIndustries",
                table: "InvestorPreferences",
                newName: "PreferredIndustryIDs");

            migrationBuilder.AddColumn<int>(
                name: "StageID",
                table: "Startups",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubIndustryID",
                table: "Startups",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("DELETE FROM \"InvestorStageFocuses\"");
            migrationBuilder.Sql("DELETE FROM \"InvestorIndustryFocuses\"");

            migrationBuilder.AddColumn<int>(
                name: "StageID",
                table: "InvestorStageFocuses",
                type: "integer",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "IndustryID",
                table: "InvestorIndustryFocuses",
                type: "integer",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Industries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Stages",
                columns: table => new
                {
                    StageID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StageName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stages", x => x.StageID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Startups_StageID",
                table: "Startups",
                column: "StageID");

            migrationBuilder.CreateIndex(
                name: "IX_Startups_SubIndustryID",
                table: "Startups",
                column: "SubIndustryID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorStageFocuses_StageID",
                table: "InvestorStageFocuses",
                column: "StageID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorIndustryFocuses_IndustryID",
                table: "InvestorIndustryFocuses",
                column: "IndustryID");

            migrationBuilder.AddForeignKey(
                name: "FK_InvestorIndustryFocuses_Industries_IndustryID",
                table: "InvestorIndustryFocuses",
                column: "IndustryID",
                principalTable: "Industries",
                principalColumn: "IndustryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvestorStageFocuses_Stages_StageID",
                table: "InvestorStageFocuses",
                column: "StageID",
                principalTable: "Stages",
                principalColumn: "StageID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Startups_Industries_SubIndustryID",
                table: "Startups",
                column: "SubIndustryID",
                principalTable: "Industries",
                principalColumn: "IndustryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Startups_Stages_StageID",
                table: "Startups",
                column: "StageID",
                principalTable: "Stages",
                principalColumn: "StageID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvestorIndustryFocuses_Industries_IndustryID",
                table: "InvestorIndustryFocuses");

            migrationBuilder.DropForeignKey(
                name: "FK_InvestorStageFocuses_Stages_StageID",
                table: "InvestorStageFocuses");

            migrationBuilder.DropForeignKey(
                name: "FK_Startups_Industries_SubIndustryID",
                table: "Startups");

            migrationBuilder.DropForeignKey(
                name: "FK_Startups_Stages_StageID",
                table: "Startups");

            migrationBuilder.DropTable(
                name: "Stages");

            migrationBuilder.DropIndex(
                name: "IX_Startups_StageID",
                table: "Startups");

            migrationBuilder.DropIndex(
                name: "IX_Startups_SubIndustryID",
                table: "Startups");

            migrationBuilder.DropIndex(
                name: "IX_InvestorStageFocuses_StageID",
                table: "InvestorStageFocuses");

            migrationBuilder.DropIndex(
                name: "IX_InvestorIndustryFocuses_IndustryID",
                table: "InvestorIndustryFocuses");

            migrationBuilder.DropColumn(
                name: "StageID",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "SubIndustryID",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "StageID",
                table: "InvestorStageFocuses");

            migrationBuilder.DropColumn(
                name: "IndustryID",
                table: "InvestorIndustryFocuses");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Industries");

            migrationBuilder.RenameColumn(
                name: "PreferredStageIDs",
                table: "InvestorPreferences",
                newName: "PreferredStages");

            migrationBuilder.RenameColumn(
                name: "PreferredIndustryIDs",
                table: "InvestorPreferences",
                newName: "PreferredIndustries");

            migrationBuilder.AddColumn<short>(
                name: "Stage",
                table: "Startups",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubIndustry",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Stage",
                table: "InvestorStageFocuses",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "InvestorIndustryFocuses",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
