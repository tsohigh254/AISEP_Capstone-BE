using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260330102910_FixSlotIsActiveDefault")]
    /// <inheritdoc />
    public partial class FixSlotIsActiveDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows inserted before AddProposeSlotsFields migration received IsActive=false and ProposedBy=''
            // by the ALTER TABLE default values. These were valid startup-proposed slots, so fix them.
            migrationBuilder.Sql(@"
                UPDATE ""MentorshipRequestedSlots""
                SET ""IsActive"" = true, ""ProposedBy"" = 'Startup'
                WHERE ""IsActive"" = false AND ""ProposedBy"" = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""MentorshipRequestedSlots""
                SET ""IsActive"" = false, ""ProposedBy"" = ''
                WHERE ""IsActive"" = true AND ""ProposedBy"" = 'Startup';
            ");
        }
    }
}
