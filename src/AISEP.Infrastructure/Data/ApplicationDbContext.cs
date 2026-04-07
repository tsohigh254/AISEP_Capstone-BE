using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AISEP.Infrastructure.Data;

/// <summary>
/// Application DbContext for AISEP
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Authentication
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();

    // Startup
    public DbSet<Startup> Startups => Set<Startup>();
    public DbSet<StartupKycSubmission> StartupKycSubmissions => Set<StartupKycSubmission>();
    public DbSet<StartupKycEvidenceFile> StartupKycEvidenceFiles => Set<StartupKycEvidenceFile>();
    public DbSet<StartupKycRequestedItem> StartupKycRequestedItems => Set<StartupKycRequestedItem>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentBlockchainProof> DocumentBlockchainProofs => Set<DocumentBlockchainProof>();

    // Advisor
    public DbSet<Advisor> Advisors => Set<Advisor>();
    public DbSet<AdvisorAvailability> AdvisorAvailabilities => Set<AdvisorAvailability>();
    public DbSet<AdvisorIndustryFocus> AdvisorIndustryFocuses => Set<AdvisorIndustryFocus>();
    public DbSet<AdvisorTestimonial> AdvisorTestimonials => Set<AdvisorTestimonial>();

    // Investor
    public DbSet<Investor> Investors => Set<Investor>();
    public DbSet<InvestorWatchlist> InvestorWatchlists => Set<InvestorWatchlist>();
    public DbSet<InvestorPreferences> InvestorPreferences => Set<InvestorPreferences>();
    public DbSet<InvestorIndustryFocus> InvestorIndustryFocuses => Set<InvestorIndustryFocus>();
    public DbSet<InvestorStageFocus> InvestorStageFocuses => Set<InvestorStageFocus>();
    public DbSet<PortfolioCompany> PortfolioCompanies => Set<PortfolioCompany>();
    public DbSet<InvestorKycSubmission> InvestorKycSubmissions => Set<InvestorKycSubmission>();
    public DbSet<InvestorKycEvidenceFile> InvestorKycEvidenceFiles => Set<InvestorKycEvidenceFile>();

    // AI & Scoring
    public DbSet<StartupPotentialScore> StartupPotentialScores => Set<StartupPotentialScore>();
    public DbSet<ScoreSubMetric> ScoreSubMetrics => Set<ScoreSubMetric>();
    public DbSet<ScoreImprovementRecommendation> ScoreImprovementRecommendations => Set<ScoreImprovementRecommendation>();
    public DbSet<ScoringModelConfiguration> ScoringModelConfigurations => Set<ScoringModelConfiguration>();

    // Collaboration
    public DbSet<StartupAdvisorMentorship> StartupAdvisorMentorships => Set<StartupAdvisorMentorship>();
    public DbSet<MentorshipSession> MentorshipSessions => Set<MentorshipSession>();
    public DbSet<MentorshipReport> MentorshipReports => Set<MentorshipReport>();
    public DbSet<MentorshipFeedback> MentorshipFeedbacks => Set<MentorshipFeedback>();
    public DbSet<StartupInvestorConnection> StartupInvestorConnections => Set<StartupInvestorConnection>();
    public DbSet<InformationRequest> InformationRequests => Set<InformationRequest>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    // System
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FlaggedContent> FlaggedContents => Set<FlaggedContent>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();
    public DbSet<Industry> Industries => Set<Industry>();
    public DbSet<IndustryTrend> IndustryTrends => Set<IndustryTrend>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<PlatformAnalytics> PlatformAnalytics => Set<PlatformAnalytics>();
    public DbSet<SavedReport> SavedReports => Set<SavedReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure primary keys
        ConfigurePrimaryKeys(modelBuilder);
        
        // Configure relationships and constraints here
        ConfigureRelationships(modelBuilder);

        // Configure enum → smallint conversions
        ConfigureEnumConversions(modelBuilder);
    }

    private void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        // Shared ProfileStatus
        modelBuilder.Entity<Advisor>().Property(e => e.ProfileStatus).HasConversion<short>().HasDefaultValue(ProfileStatus.Draft);
        modelBuilder.Entity<Investor>().Property(e => e.ProfileStatus).HasConversion<short>().HasDefaultValue(ProfileStatus.Draft);
        modelBuilder.Entity<Startup>().Property(e => e.ProfileStatus).HasConversion<short>().HasDefaultValue(ProfileStatus.Draft);

        // Tags
        modelBuilder.Entity<Advisor>().Property(e => e.AdvisorTag).HasConversion<short>();
        modelBuilder.Entity<Investor>().Property(e => e.InvestorTag).HasConversion<short>();
        modelBuilder.Entity<Startup>().Property(e => e.StartupTag).HasConversion<short>();

        // Chat
        modelBuilder.Entity<Conversation>().Property(e => e.ConversationStatus).HasConversion<short>().HasDefaultValue(ConversationStatus.Active);

        // Blockchain
        modelBuilder.Entity<DocumentBlockchainProof>().Property(e => e.ProofStatus).HasConversion<short>().HasDefaultValue(ProofStatus.Anchored);

        // Moderation
        modelBuilder.Entity<FlaggedContent>().Property(e => e.ModerationStatus).HasConversion<short>().HasDefaultValue(ModerationStatus.None);

        // InformationRequest
        modelBuilder.Entity<InformationRequest>().Property(e => e.RequestStatus).HasConversion<short>().HasDefaultValue(RequestStatus.Pending);

        // Investor
        modelBuilder.Entity<InvestorStageFocus>().Property(e => e.Stage).HasConversion<short>();
        modelBuilder.Entity<InvestorWatchlist>().Property(e => e.Priority).HasConversion<short?>().HasDefaultValueSql("1");

        // Portfolio
        modelBuilder.Entity<PortfolioCompany>().Property(e => e.InvestmentStage).HasConversion<short?>();
        modelBuilder.Entity<PortfolioCompany>().Property(e => e.CurrentStatus).HasConversion<short?>();
        modelBuilder.Entity<PortfolioCompany>().Property(e => e.ExitType).HasConversion<short?>();
        modelBuilder.Entity<Document>().Property(e => e.DocumentType).HasConversion<short?>();
        modelBuilder.Entity<Document>().Property(e => e.AnalysisStatus).HasConversion<short?>();

        // Startup
        modelBuilder.Entity<Startup>().Property(e => e.Stage).HasConversion<short?>();
        modelBuilder.Entity<StartupKycSubmission>().Property(e => e.WorkflowStatus).HasConversion<short>().HasDefaultValue(StartupKycWorkflowStatus.Draft);
        modelBuilder.Entity<StartupKycSubmission>().Property(e => e.ResultLabel).HasConversion<short>().HasDefaultValue(StartupKycResultLabel.None);
        modelBuilder.Entity<StartupKycSubmission>().Property(e => e.StartupVerificationType).HasConversion<short>();
        modelBuilder.Entity<StartupKycEvidenceFile>().Property(e => e.Kind).HasConversion<short>().HasDefaultValue(StartupKycEvidenceKind.Other);

        // Report
        modelBuilder.Entity<SavedReport>().Property(e => e.ReportType).HasConversion<short>();

        // Score
        modelBuilder.Entity<ScoreImprovementRecommendation>().Property(e => e.Priority).HasConversion<short>();

        // Connection
        modelBuilder.Entity<StartupInvestorConnection>().Property(e => e.ConnectionStatus).HasConversion<short>().HasDefaultValue(ConnectionStatus.Requested);

        // Mentorship
        modelBuilder.Entity<StartupAdvisorMentorship>().Property(e => e.MentorshipStatus).HasConversion<short>().HasDefaultValue(MentorshipStatus.Requested);
    }

    private void ConfigurePrimaryKeys(ModelBuilder modelBuilder)
    {
        // Authentication
        modelBuilder.Entity<User>().HasKey(u => u.UserID);
        modelBuilder.Entity<Role>().HasKey(r => r.RoleID);
        modelBuilder.Entity<UserRole>().HasKey(ur => ur.UserRoleID);
        modelBuilder.Entity<Permission>().HasKey(p => p.PermissionID);
        modelBuilder.Entity<RolePermission>().HasKey(rp => rp.RolePermissionID);
        modelBuilder.Entity<RefreshToken>().HasKey(rt => rt.RefreshTokenID);
        modelBuilder.Entity<PasswordResetToken>().HasKey(prt => prt.PasswordResetTokenID);

        // Startup
        modelBuilder.Entity<Startup>().HasKey(s => s.StartupID);
        modelBuilder.Entity<StartupKycSubmission>().HasKey(s => s.SubmissionID);
        modelBuilder.Entity<StartupKycEvidenceFile>().HasKey(e => e.EvidenceFileID);
        modelBuilder.Entity<StartupKycRequestedItem>().HasKey(r => r.RequestedItemID);
        modelBuilder.Entity<TeamMember>().HasKey(tm => tm.TeamMemberID);
        modelBuilder.Entity<Document>().HasKey(d => d.DocumentID);
        modelBuilder.Entity<DocumentBlockchainProof>().HasKey(p => p.ProofID);

        // Advisor
        modelBuilder.Entity<Advisor>().HasKey(a => a.AdvisorID);
        modelBuilder.Entity<AdvisorAvailability>().HasKey(aa => aa.AvailabilityID);
        modelBuilder.Entity<AdvisorIndustryFocus>().HasKey(aif => aif.IndustryFocusID);
        modelBuilder.Entity<AdvisorTestimonial>().HasKey(at => at.TestimonialID);

        // Investor
        modelBuilder.Entity<Investor>().HasKey(i => i.InvestorID);
        modelBuilder.Entity<InvestorWatchlist>().HasKey(iw => iw.WatchlistID);
        modelBuilder.Entity<InvestorPreferences>().HasKey(ip => ip.PreferenceID);
        modelBuilder.Entity<InvestorIndustryFocus>().HasKey(iif => iif.FocusID);
        modelBuilder.Entity<InvestorStageFocus>().HasKey(isf => isf.StageFocusID);
        modelBuilder.Entity<PortfolioCompany>().HasKey(pc => pc.PortfolioID);

        // AI & Scoring
        modelBuilder.Entity<StartupPotentialScore>().HasKey(sps => sps.ScoreID);
        modelBuilder.Entity<ScoreSubMetric>().HasKey(ssm => ssm.SubMetricID);
        modelBuilder.Entity<ScoreImprovementRecommendation>().HasKey(sir => sir.RecommendationID);
        modelBuilder.Entity<ScoringModelConfiguration>().HasKey(smc => smc.ConfigID);

        // Collaboration
        modelBuilder.Entity<StartupAdvisorMentorship>().HasKey(sam => sam.MentorshipID);
        modelBuilder.Entity<MentorshipSession>().HasKey(ms => ms.SessionID);
        modelBuilder.Entity<MentorshipReport>().HasKey(mr => mr.ReportID);
        modelBuilder.Entity<MentorshipFeedback>().HasKey(mf => mf.FeedbackID);
        modelBuilder.Entity<StartupInvestorConnection>().HasKey(sic => sic.ConnectionID);
        modelBuilder.Entity<InformationRequest>().HasKey(ir => ir.RequestID);
        modelBuilder.Entity<Conversation>().HasKey(c => c.ConversationID);
        modelBuilder.Entity<Message>().HasKey(m => m.MessageID);

        // System
        modelBuilder.Entity<Notification>().HasKey(n => n.NotificationID);
        modelBuilder.Entity<AuditLog>().HasKey(al => al.LogID);
        modelBuilder.Entity<FlaggedContent>().HasKey(fc => fc.FlagID);
        modelBuilder.Entity<ModerationAction>().HasKey(ma => ma.ActionID);
        modelBuilder.Entity<Industry>().HasKey(i => i.IndustryID);
        modelBuilder.Entity<IndustryTrend>().HasKey(it => it.TrendID);
        modelBuilder.Entity<SystemSettings>().HasKey(ss => ss.SettingID);
        modelBuilder.Entity<Incident>().HasKey(i => i.IncidentID);
        modelBuilder.Entity<PlatformAnalytics>().HasKey(pa => pa.AnalyticID);
        modelBuilder.Entity<SavedReport>().HasKey(sr => sr.ReportID);
    }

    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // User relationships
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.AssignedByUser)
            .WithMany()
            .HasForeignKey(ur => ur.AssignedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Startup>()
            .HasOne(s => s.ApprovedByUser)
            .WithMany()
            .HasForeignKey(s => s.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Advisor>()
            .HasOne(a => a.ApprovedByUser)
            .WithMany()
            .HasForeignKey(a => a.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Investor>()
            .HasOne(i => i.ApprovedByUser)
            .WithMany()
            .HasForeignKey(i => i.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Each user can only have one startup profile
        modelBuilder.Entity<Startup>()
            .HasIndex(s => s.UserID)
            .IsUnique();

        modelBuilder.Entity<StartupKycSubmission>()
            .HasIndex(s => new { s.StartupID, s.Version })
            .IsUnique();

        modelBuilder.Entity<StartupKycSubmission>()
            .HasIndex(s => new { s.StartupID, s.IsActive })
            .HasFilter("\"IsActive\" = true");

        // Startup → Industry FK
        modelBuilder.Entity<Startup>()
            .HasOne(s => s.Industry)
            .WithMany()
            .HasForeignKey(s => s.IndustryID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StartupKycSubmission>()
            .HasOne(s => s.Startup)
            .WithMany(s => s.KycSubmissions)
            .HasForeignKey(s => s.StartupID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StartupKycSubmission>()
            .HasOne(s => s.ReviewedByUser)
            .WithMany()
            .HasForeignKey(s => s.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StartupKycEvidenceFile>()
            .HasOne(e => e.Submission)
            .WithMany(s => s.EvidenceFiles)
            .HasForeignKey(e => e.SubmissionID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StartupKycRequestedItem>()
            .HasOne(r => r.Submission)
            .WithMany(s => s.RequestedAdditionalItems)
            .HasForeignKey(r => r.SubmissionID)
            .OnDelete(DeleteBehavior.Cascade);

        // DocumentBlockchainProof - one-to-one with Document
        modelBuilder.Entity<DocumentBlockchainProof>()
            .HasOne(p => p.Document)
            .WithOne(d => d.BlockchainProof)
            .HasForeignKey<DocumentBlockchainProof>(p => p.DocumentID);

        // AdvisorAvailability - one-to-one with Advisor
        modelBuilder.Entity<AdvisorAvailability>()
            .HasOne(a => a.Advisor)
            .WithOne(adv => adv.Availability)
            .HasForeignKey<AdvisorAvailability>(a => a.AdvisorID);

        // InvestorPreferences - one-to-one with Investor
        modelBuilder.Entity<InvestorPreferences>()
            .HasOne(p => p.Investor)
            .WithOne(i => i.Preferences)
            .HasForeignKey<InvestorPreferences>(p => p.InvestorID);

        // Industry self-reference
        modelBuilder.Entity<Industry>()
            .HasOne(i => i.ParentIndustry)
            .WithMany(i => i.SubIndustries)
            .HasForeignKey(i => i.ParentIndustryID)
            .OnDelete(DeleteBehavior.Restrict);

        // FlaggedContent relationships
        modelBuilder.Entity<FlaggedContent>()
            .HasOne(f => f.RelatedUser)
            .WithMany()
            .HasForeignKey(f => f.RelatedUserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FlaggedContent>()
            .HasOne(f => f.ReviewedByUser)
            .WithMany()
            .HasForeignKey(f => f.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ModerationAction>()
            .HasOne(m => m.TargetUser)
            .WithMany()
            .HasForeignKey(m => m.TargetUserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ModerationAction>()
            .HasOne(m => m.ActionTakenByUser)
            .WithMany()
            .HasForeignKey(m => m.ActionTakenBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent cascade delete cycles
        modelBuilder.Entity<InvestorWatchlist>()
            .HasOne(iw => iw.Startup)
            .WithMany(s => s.WatchedByInvestors)
            .HasForeignKey(iw => iw.StartupID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StartupAdvisorMentorship>()
            .HasOne(m => m.Startup)
            .WithMany(s => s.Mentorships)
            .HasForeignKey(m => m.StartupID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StartupInvestorConnection>()
            .HasOne(c => c.Startup)
            .WithMany(s => s.InvestorConnections)
            .HasForeignKey(c => c.StartupID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdvisorTestimonial>()
            .HasOne(t => t.Startup)
            .WithMany(s => s.AdvisorTestimonials)
            .HasForeignKey(t => t.StartupID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdvisorTestimonial>()
            .HasOne(t => t.Mentorship)
            .WithMany(m => m.Testimonials)
            .HasForeignKey(t => t.MentorshipID)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent cascade in InformationRequest
        modelBuilder.Entity<InformationRequest>()
            .HasOne(ir => ir.Connection)
            .WithMany(c => c.InformationRequests)
            .HasForeignKey(ir => ir.ConnectionID)
            .OnDelete(DeleteBehavior.Restrict);

        // Incident relationships
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incident>()
            .HasOne(i => i.ResolvedByUser)
            .WithMany()
            .HasForeignKey(i => i.ResolvedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Document review relationship
        modelBuilder.Entity<Document>()
            .HasOne(d => d.ReviewedByUser)
            .WithMany()
            .HasForeignKey(d => d.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
