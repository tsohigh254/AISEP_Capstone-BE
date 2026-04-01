using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStartupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "CurrentNeeds",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "MetricSummary",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "ProductStatus",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "TeamSize",
                table: "Startups");

            migrationBuilder.RenameColumn(
                name: "SubIndustry",
                table: "Startups",
                newName: "FileCertificateBusiness");

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "Startups",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "Startups",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullNameOfApplicant",
                table: "Startups",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoleOfApplicant",
                table: "Startups",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<short>(
                name: "StartupTag",
                table: "Startups",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "CompletedSessions",
                table: "Advisors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DomainTags",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperiencesJson",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Expertise",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "Advisors",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Advisors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "Advisors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuitableFor",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportedDurations",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "Advisors",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "FullNameOfApplicant",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "RoleOfApplicant",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "StartupTag",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "CompletedSessions",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "DomainTags",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "ExperiencesJson",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "Expertise",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "SuitableFor",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "SupportedDurations",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "Advisors");

            migrationBuilder.RenameColumn(
                name: "FileCertificateBusiness",
                table: "Startups",
                newName: "SubIndustry");

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "Startups",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentNeeds",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetricSummary",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductStatus",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamSize",
                table: "Startups",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
