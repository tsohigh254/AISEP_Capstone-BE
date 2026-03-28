using AISEP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Domain.Entities
{
    public class AdvisorAvailableSlot
    {
        public int SlotID { get; set; }
        public int AdvisorID { get; set; }
        public int? TemplateID { get; set; } // Reference to template if auto-generated
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsBooked { get; set; } = false;
        public int? BookedSessionID { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Advisor Advisor { get; set; } = null!;
        public MentorshipSession? BookedSession { get; set; }
        public AdvisorWeeklyScheduleTemplate? Template { get; set; }
    }
}
