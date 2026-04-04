using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProposeSlotsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "MentorshipRequestedSlots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "MentorshipRequestedSlots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedBy",
                table: "MentorshipRequestedSlots",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "MentorshipRequestedSlots");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "MentorshipRequestedSlots");

            migrationBuilder.DropColumn(
                name: "ProposedBy",
                table: "MentorshipRequestedSlots");
        }
    }
}
