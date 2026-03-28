using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.Slot
{
    public class AvailableSlotDto
    {
        public int SlotID { get; set; }
        public int AdvisorID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsBooked { get; set; }
        public int? BookedSessionID { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class BookSessionFromSlotRequest
    {
        public int MentorshipID { get; set; }
        public int AvailableSlotID { get; set; }
        public string? MeetingUrl { get; set; }
    }

    public class UpdateAvailableSlotRequest
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateAvailableSlotRequest
    {
        public DateTime StartTime { get; set; }  // 2026-04-01 01:00:00
        public DateTime EndTime { get; set; }    // 2026-04-01 01:30:00
        public string? Notes { get; set; }
    }

    public class CreateMultipleAvailableSlotsRequest
    {
        public List<CreateAvailableSlotRequest> Slots { get; set; } = new();
    }

}
