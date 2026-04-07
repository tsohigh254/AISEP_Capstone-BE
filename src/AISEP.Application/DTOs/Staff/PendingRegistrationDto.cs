using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.Staff
{
    public class PendingRegistrationDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string UserType { get; set; } // Startup, Advisor, etc.
        public DateTime RegistrationDate { get; set; }
        public string CompanyName { get; set; }
        public string Status { get; set; } // Pending, Approved, Rejected
    }

    public class RegistrationApprovalRequest
    {
        public Guid UserId { get; set; }
        public bool IsApproved { get; set; }
        public string Reason { get; set; } // Optional: reason for rejection
    }

    public class RegistrationApprovalResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid UserId { get; set; }
        public DateTime ApprovedDate { get; set; }
    }

    public class ApproveStartupRegistrationRequest
    {
        public int StartupId { get; set; }
        public int Score { get; set; }
        public string? Remarks { get; set; }
        public bool RequiresNewEvidence { get; set; }
    }

    public class ApproveAdvisorRegistrationRequest
    {
        public int AdvisorId { get; set; }
        public int Score { get; set; }
        public string? Remarks { get; set; }
    }

    public class ApproveInvestorRegistrationRequest
    {
        public int InvestorId { get; set; }
        public int Score { get; set; }
        public bool IsInstitutional { get; set; }
        public string? Remarks { get; set; }
    }

    public class RejectRegistrationRequest
    {
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool? RequiresNewEvidence { get; set; }
    }
}
