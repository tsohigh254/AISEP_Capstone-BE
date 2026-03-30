using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.Staff
{
    public class DailyActivityDto
    {
        public DateTime Date { get; set; }
        public int NewRegistrations { get; set; }
        public int ApprovedRegistrations { get; set; }
        public int SessionsCompleted { get; set; }
        public int FeedbackSubmitted { get; set; }
    }
}
