using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Domain.Entities
{
    public class EmailOtp
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Otp { get; set; }
        public bool IsUsed { get; set; }
        public DateTime ExpiredAt { get; set; } = DateTime.UtcNow.AddMinutes(5);
    }
}
