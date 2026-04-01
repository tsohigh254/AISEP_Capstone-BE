using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Industries",
                columns: table => new
                {
                    IndustryID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IndustryName = table.Column<string>(type: "text", nullable: false),
                    ParentIndustryID = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Industries", x => x.IndustryID);
                    table.ForeignKey(
                        name: "FK_Industries_Industries_ParentIndustryID",
                        column: x => x.ParentIndustryID,
                        principalTable: "Industries",
                        principalColumn: "IndustryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PermissionName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionID);
                });

            migrationBuilder.CreateTable(
                name: "PlatformAnalytics",
                columns: table => new
                {
                    AnalyticID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MetricName = table.Column<string>(type: "text", nullable: false),
                    MetricValue = table.Column<float>(type: "real", nullable: false),
                    MetricDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformAnalytics", x => x.AnalyticID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    UserType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "IndustryTrends",
                columns: table => new
                {
                    TrendID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IndustryID = table.Column<int>(type: "integer", nullable: false),
                    TrendPeriod = table.Column<string>(type: "text", nullable: false),
                    StartupCount = table.Column<int>(type: "integer", nullable: false),
                    AveragePotentialScore = table.Column<float>(type: "real", nullable: true),
                    TotalFundingRaised = table.Column<decimal>(type: "numeric", nullable: true),
                    AverageRoundSize = table.Column<decimal>(type: "numeric", nullable: true),
                    TopStrengths = table.Column<string>(type: "text", nullable: true),
                    CommonWeaknesses = table.Column<string>(type: "text", nullable: true),
                    AIGeneratedInsights = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndustryTrends", x => x.TrendID);
                    table.ForeignKey(
                        name: "FK_IndustryTrends_Industries_IndustryID",
                        column: x => x.IndustryID,
                        principalTable: "Industries",
                        principalColumn: "IndustryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RolePermissionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleID = table.Column<int>(type: "integer", nullable: false),
                    PermissionID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.RolePermissionID);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionID",
                        column: x => x.PermissionID,
                        principalTable: "Permissions",
                        principalColumn: "PermissionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Advisors",
                columns: table => new
                {
                    AdvisorID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Company = table.Column<string>(type: "text", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    ProfilePhotoURL = table.Column<string>(type: "text", nullable: true),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: true),
                    MentorshipPhilosophy = table.Column<string>(type: "text", nullable: true),
                    LinkedInURL = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    ProfileStatus = table.Column<string>(type: "text", nullable: true),
                    ProfileCompleteness = table.Column<int>(type: "integer", nullable: true),
                    TotalMentees = table.Column<int>(type: "integer", nullable: false),
                    TotalSessionHours = table.Column<float>(type: "real", nullable: false),
                    AverageRating = table.Column<float>(type: "real", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Advisors", x => x.AdvisorID);
                    table.ForeignKey(
                        name: "FK_Advisors_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<int>(type: "integer", nullable: true),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityID = table.Column<int>(type: "integer", nullable: true),
                    ActionDetails = table.Column<string>(type: "text", nullable: true),
                    IPAddress = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "FlaggedContents",
                columns: table => new
                {
                    FlagID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    ContentID = table.Column<int>(type: "integer", nullable: false),
                    RelatedUserID = table.Column<int>(type: "integer", nullable: true),
                    FlagReason = table.Column<string>(type: "text", nullable: false),
                    FlagSource = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<string>(type: "text", nullable: true),
                    FlagDetails = table.Column<string>(type: "text", nullable: true),
                    ModerationStatus = table.Column<string>(type: "text", nullable: false),
                    FlaggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<int>(type: "integer", nullable: true),
                    ModerationAction = table.Column<string>(type: "text", nullable: true),
                    ModeratorNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlaggedContents", x => x.FlagID);
                    table.ForeignKey(
                        name: "FK_FlaggedContents_Users_RelatedUserID",
                        column: x => x.RelatedUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlaggedContents_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Investors",
                columns: table => new
                {
                    InvestorID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    FirmName = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    ProfilePhotoURL = table.Column<string>(type: "text", nullable: true),
                    InvestmentThesis = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    LinkedInURL = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investors", x => x.InvestorID);
                    table.ForeignKey(
                        name: "FK_Investors_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    NotificationType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "text", nullable: true),
                    RelatedEntityID = table.Column<int>(type: "integer", nullable: true),
                    ActionURL = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedReports",
                columns: table => new
                {
                    ReportID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportName = table.Column<string>(type: "text", nullable: false),
                    ReportType = table.Column<string>(type: "text", nullable: false),
                    Parameters = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    IsScheduled = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduleFrequency = table.Column<string>(type: "text", nullable: true),
                    LastGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserUserID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedReports", x => x.ReportID);
                    table.ForeignKey(
                        name: "FK_SavedReports_Users_CreatedByUserUserID",
                        column: x => x.CreatedByUserUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoringModelConfigurations",
                columns: table => new
                {
                    ConfigID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<string>(type: "text", nullable: false),
                    TeamWeight = table.Column<float>(type: "real", nullable: false),
                    MarketWeight = table.Column<float>(type: "real", nullable: false),
                    ProductWeight = table.Column<float>(type: "real", nullable: false),
                    TractionWeight = table.Column<float>(type: "real", nullable: false),
                    FinancialWeight = table.Column<float>(type: "real", nullable: false),
                    ApplicableStage = table.Column<string>(type: "text", nullable: true),
                    ChangeNotes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserUserID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringModelConfigurations", x => x.ConfigID);
                    table.ForeignKey(
                        name: "FK_ScoringModelConfigurations_Users_CreatedByUserUserID",
                        column: x => x.CreatedByUserUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Startups",
                columns: table => new
                {
                    StartupID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    OneLiner = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Industry = table.Column<string>(type: "text", nullable: true),
                    SubIndustry = table.Column<string>(type: "text", nullable: true),
                    Stage = table.Column<string>(type: "text", nullable: true),
                    FoundedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TeamSize = table.Column<int>(type: "integer", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    LogoURL = table.Column<string>(type: "text", nullable: true),
                    CoverImageURL = table.Column<string>(type: "text", nullable: true),
                    FundingStage = table.Column<string>(type: "text", nullable: true),
                    FundingAmountSought = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentFundingRaised = table.Column<decimal>(type: "numeric", nullable: true),
                    Valuation = table.Column<decimal>(type: "numeric", nullable: true),
                    ProfileStatus = table.Column<string>(type: "text", nullable: true),
                    ProfileCompleteness = table.Column<int>(type: "integer", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Startups", x => x.StartupID);
                    table.ForeignKey(
                        name: "FK_Startups_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Startups_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    SettingID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SettingKey = table.Column<string>(type: "text", nullable: false),
                    SettingValue = table.Column<string>(type: "text", nullable: false),
                    SettingType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserUserID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.SettingID);
                    table.ForeignKey(
                        name: "FK_SystemSettings_Users_UpdatedByUserUserID",
                        column: x => x.UpdatedByUserUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserRoleID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    RoleID = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.UserRoleID);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdvisorAchievements",
                columns: table => new
                {
                    AchievementID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    AchievementType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    URL = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorAchievements", x => x.AchievementID);
                    table.ForeignKey(
                        name: "FK_AdvisorAchievements_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdvisorAvailabilities",
                columns: table => new
                {
                    AvailabilityID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    SessionFormats = table.Column<string>(type: "text", nullable: true),
                    TypicalSessionDuration = table.Column<int>(type: "integer", nullable: true),
                    WeeklyAvailableHours = table.Column<int>(type: "integer", nullable: true),
                    MaxConcurrentMentees = table.Column<int>(type: "integer", nullable: true),
                    ResponseTimeCommitment = table.Column<string>(type: "text", nullable: true),
                    CalendarConnected = table.Column<bool>(type: "boolean", nullable: false),
                    IsAcceptingNewMentees = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorAvailabilities", x => x.AvailabilityID);
                    table.ForeignKey(
                        name: "FK_AdvisorAvailabilities_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdvisorExpertises",
                columns: table => new
                {
                    ExpertiseID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    SubTopic = table.Column<string>(type: "text", nullable: true),
                    ProficiencyLevel = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorExpertises", x => x.ExpertiseID);
                    table.ForeignKey(
                        name: "FK_AdvisorExpertises_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdvisorIndustryFocuses",
                columns: table => new
                {
                    IndustryFocusID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    Industry = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorIndustryFocuses", x => x.IndustryFocusID);
                    table.ForeignKey(
                        name: "FK_AdvisorIndustryFocuses_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModerationActions",
                columns: table => new
                {
                    ActionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlagID = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    TargetUserID = table.Column<int>(type: "integer", nullable: true),
                    ActionDetails = table.Column<string>(type: "text", nullable: true),
                    MessageToUser = table.Column<string>(type: "text", nullable: true),
                    ActionTakenBy = table.Column<int>(type: "integer", nullable: true),
                    ActionTakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlaggedContentFlagID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationActions", x => x.ActionID);
                    table.ForeignKey(
                        name: "FK_ModerationActions_FlaggedContents_FlaggedContentFlagID",
                        column: x => x.FlaggedContentFlagID,
                        principalTable: "FlaggedContents",
                        principalColumn: "FlagID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationActions_Users_ActionTakenBy",
                        column: x => x.ActionTakenBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModerationActions_Users_TargetUserID",
                        column: x => x.TargetUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvestorIndustryFocuses",
                columns: table => new
                {
                    FocusID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestorID = table.Column<int>(type: "integer", nullable: false),
                    Industry = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestorIndustryFocuses", x => x.FocusID);
                    table.ForeignKey(
                        name: "FK_InvestorIndustryFocuses_Investors_InvestorID",
                        column: x => x.InvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvestorPreferences",
                columns: table => new
                {
                    PreferenceID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestorID = table.Column<int>(type: "integer", nullable: false),
                    MinPotentialScore = table.Column<float>(type: "real", nullable: true),
                    PreferredStages = table.Column<string>(type: "text", nullable: true),
                    PreferredIndustries = table.Column<string>(type: "text", nullable: true),
                    PreferredGeographies = table.Column<string>(type: "text", nullable: true),
                    MinInvestmentSize = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxInvestmentSize = table.Column<decimal>(type: "numeric", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestorPreferences", x => x.PreferenceID);
                    table.ForeignKey(
                        name: "FK_InvestorPreferences_Investors_InvestorID",
                        column: x => x.InvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvestorStageFocuses",
                columns: table => new
                {
                    StageFocusID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestorID = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestorStageFocuses", x => x.StageFocusID);
                    table.ForeignKey(
                        name: "FK_InvestorStageFocuses_Investors_InvestorID",
                        column: x => x.InvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioCompanies",
                columns: table => new
                {
                    PortfolioID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestorID = table.Column<int>(type: "integer", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    Industry = table.Column<string>(type: "text", nullable: true),
                    InvestmentStage = table.Column<string>(type: "text", nullable: true),
                    InvestmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvestmentAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentStatus = table.Column<string>(type: "text", nullable: true),
                    ExitType = table.Column<string>(type: "text", nullable: true),
                    ExitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExitValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CompanyLogoURL = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioCompanies", x => x.PortfolioID);
                    table.ForeignKey(
                        name: "FK_PortfolioCompanies_Investors_InvestorID",
                        column: x => x.InvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartupID = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileURL = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<int>(type: "integer", nullable: false),
                    FileFormat = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "text", nullable: true),
                    IsAnalyzed = table.Column<bool>(type: "boolean", nullable: false),
                    AnalysisStatus = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentID);
                    table.ForeignKey(
                        name: "FK_Documents_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvestorWatchlists",
                columns: table => new
                {
                    WatchlistID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestorID = table.Column<int>(type: "integer", nullable: false),
                    StartupID = table.Column<int>(type: "integer", nullable: false),
                    WatchReason = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestorWatchlists", x => x.WatchlistID);
                    table.ForeignKey(
                        name: "FK_InvestorWatchlists_Investors_InvestorID",
                        column: x => x.InvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvestorWatchlists_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfileViews",
                columns: table => new
                {
                    ViewID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ViewerUserID = table.Column<int>(type: "integer", nullable: false),
                    ViewedStartupID = table.Column<int>(type: "integer", nullable: true),
                    ViewedInvestorID = table.Column<int>(type: "integer", nullable: true),
                    ViewedAdvisorID = table.Column<int>(type: "integer", nullable: true),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileViews", x => x.ViewID);
                    table.ForeignKey(
                        name: "FK_ProfileViews_Advisors_ViewedAdvisorID",
                        column: x => x.ViewedAdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileViews_Investors_ViewedInvestorID",
                        column: x => x.ViewedInvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileViews_Startups_ViewedStartupID",
                        column: x => x.ViewedStartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileViews_Users_ViewerUserID",
                        column: x => x.ViewerUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StartupAdvisorMentorships",
                columns: table => new
                {
                    MentorshipID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartupID = table.Column<int>(type: "integer", nullable: false),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    MentorshipStatus = table.Column<string>(type: "text", nullable: false),
                    ChallengeDescription = table.Column<string>(type: "text", nullable: true),
                    SpecificQuestions = table.Column<string>(type: "text", nullable: true),
                    ExpectedScope = table.Column<string>(type: "text", nullable: true),
                    ExpectedDuration = table.Column<string>(type: "text", nullable: true),
                    PreferredFormat = table.Column<string>(type: "text", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedReason = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedByRole = table.Column<string>(type: "text", nullable: true),
                    ObligationSummary = table.Column<string>(type: "text", nullable: true),
                    CompletionConfirmedByStartup = table.Column<bool>(type: "boolean", nullable: false),
                    CompletionConfirmedByAdvisor = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupAdvisorMentorships", x => x.MentorshipID);
                    table.ForeignKey(
                        name: "FK_StartupAdvisorMentorships_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StartupAdvisorMentorships_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StartupInvestorConnections",
                columns: table => new
                {
                    ConnectionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartupID = table.Column<int>(type: "integer", nullable: false),
                    InvestorID = table.Column<int>(type: "integer", nullable: false),
                    ConnectionStatus = table.Column<string>(type: "text", nullable: false),
                    InitiatedBy = table.Column<int>(type: "integer", nullable: true),
                    MatchScore = table.Column<float>(type: "real", nullable: true),
                    PersonalizedMessage = table.Column<string>(type: "text", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttachedDocumentIDs = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupInvestorConnections", x => x.ConnectionID);
                    table.ForeignKey(
                        name: "FK_StartupInvestorConnections_Investors_InvestorID",
                        column: x => x.InvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StartupInvestorConnections_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StartupPotentialScores",
                columns: table => new
                {
                    ScoreID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartupID = table.Column<int>(type: "integer", nullable: false),
                    ConfigID = table.Column<int>(type: "integer", nullable: true),
                    OverallScore = table.Column<float>(type: "real", nullable: false),
                    TeamScore = table.Column<float>(type: "real", nullable: false),
                    MarketScore = table.Column<float>(type: "real", nullable: false),
                    ProductScore = table.Column<float>(type: "real", nullable: false),
                    TractionScore = table.Column<float>(type: "real", nullable: false),
                    FinancialScore = table.Column<float>(type: "real", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCurrentScore = table.Column<bool>(type: "boolean", nullable: false),
                    ScoringConfigurationConfigID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupPotentialScores", x => x.ScoreID);
                    table.ForeignKey(
                        name: "FK_StartupPotentialScores_ScoringModelConfigurations_ScoringCo~",
                        column: x => x.ScoringConfigurationConfigID,
                        principalTable: "ScoringModelConfigurations",
                        principalColumn: "ConfigID");
                    table.ForeignKey(
                        name: "FK_StartupPotentialScores_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    TeamMemberID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartupID = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    LinkedInURL = table.Column<string>(type: "text", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    PhotoURL = table.Column<string>(type: "text", nullable: true),
                    IsFounder = table.Column<bool>(type: "boolean", nullable: false),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.TeamMemberID);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentBlockchainProofs",
                columns: table => new
                {
                    ProofID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentID = table.Column<int>(type: "integer", nullable: false),
                    FileHash = table.Column<string>(type: "text", nullable: false),
                    HashAlgorithm = table.Column<string>(type: "text", nullable: false),
                    BlockchainNetwork = table.Column<string>(type: "text", nullable: true),
                    TransactionHash = table.Column<string>(type: "text", nullable: true),
                    BlockNumber = table.Column<string>(type: "text", nullable: true),
                    AnchoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AnchoredBy = table.Column<int>(type: "integer", nullable: true),
                    ProofStatus = table.Column<string>(type: "text", nullable: false),
                    AnchoredByUserUserID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentBlockchainProofs", x => x.ProofID);
                    table.ForeignKey(
                        name: "FK_DocumentBlockchainProofs_Documents_DocumentID",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "DocumentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentBlockchainProofs_Users_AnchoredByUserUserID",
                        column: x => x.AnchoredByUserUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "AdvisorTestimonials",
                columns: table => new
                {
                    TestimonialID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    StartupID = table.Column<int>(type: "integer", nullable: true),
                    MentorshipID = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    TestimonialText = table.Column<string>(type: "text", nullable: true),
                    IsApprovedByFounder = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorTestimonials", x => x.TestimonialID);
                    table.ForeignKey(
                        name: "FK_AdvisorTestimonials_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdvisorTestimonials_StartupAdvisorMentorships_MentorshipID",
                        column: x => x.MentorshipID,
                        principalTable: "StartupAdvisorMentorships",
                        principalColumn: "MentorshipID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvisorTestimonials_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MentorshipSessions",
                columns: table => new
                {
                    SessionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MentorshipID = table.Column<int>(type: "integer", nullable: false),
                    ScheduledStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    SessionFormat = table.Column<string>(type: "text", nullable: true),
                    MeetingURL = table.Column<string>(type: "text", nullable: true),
                    SessionStatus = table.Column<string>(type: "text", nullable: true),
                    AdvisorConfirmedConductedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartupConfirmedConductedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConductedConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TopicsDiscussed = table.Column<string>(type: "text", nullable: true),
                    KeyInsights = table.Column<string>(type: "text", nullable: true),
                    ActionItems = table.Column<string>(type: "text", nullable: true),
                    NextSteps = table.Column<string>(type: "text", nullable: true),
                    RecommendedResources = table.Column<string>(type: "text", nullable: true),
                    AdvisorInternalNotes = table.Column<string>(type: "text", nullable: true),
                    StartupNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorshipSessions", x => x.SessionID);
                    table.ForeignKey(
                        name: "FK_MentorshipSessions_StartupAdvisorMentorships_MentorshipID",
                        column: x => x.MentorshipID,
                        principalTable: "StartupAdvisorMentorships",
                        principalColumn: "MentorshipID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    ConversationID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectionID = table.Column<int>(type: "integer", nullable: true),
                    MentorshipID = table.Column<int>(type: "integer", nullable: true),
                    ConversationStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.ConversationID);
                    table.ForeignKey(
                        name: "FK_Conversations_StartupAdvisorMentorships_MentorshipID",
                        column: x => x.MentorshipID,
                        principalTable: "StartupAdvisorMentorships",
                        principalColumn: "MentorshipID");
                    table.ForeignKey(
                        name: "FK_Conversations_StartupInvestorConnections_ConnectionID",
                        column: x => x.ConnectionID,
                        principalTable: "StartupInvestorConnections",
                        principalColumn: "ConnectionID");
                });

            migrationBuilder.CreateTable(
                name: "InformationRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectionID = table.Column<int>(type: "integer", nullable: false),
                    InvestorID = table.Column<int>(type: "integer", nullable: false),
                    RequestType = table.Column<string>(type: "text", nullable: false),
                    RequestMessage = table.Column<string>(type: "text", nullable: true),
                    RequestStatus = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseDocumentIDs = table.Column<string>(type: "text", nullable: true),
                    ResponseMessage = table.Column<string>(type: "text", nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InformationRequests", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_InformationRequests_Investors_InvestorID",
                        column: x => x.InvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InformationRequests_StartupInvestorConnections_ConnectionID",
                        column: x => x.ConnectionID,
                        principalTable: "StartupInvestorConnections",
                        principalColumn: "ConnectionID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScoreImprovementRecommendations",
                columns: table => new
                {
                    RecommendationID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScoreID = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    RecommendationText = table.Column<string>(type: "text", nullable: true),
                    ExpectedImpact = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PotentialScoreScoreID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreImprovementRecommendations", x => x.RecommendationID);
                    table.ForeignKey(
                        name: "FK_ScoreImprovementRecommendations_StartupPotentialScores_Pote~",
                        column: x => x.PotentialScoreScoreID,
                        principalTable: "StartupPotentialScores",
                        principalColumn: "ScoreID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoreSubMetrics",
                columns: table => new
                {
                    SubMetricID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScoreID = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    MetricName = table.Column<string>(type: "text", nullable: false),
                    MetricValue = table.Column<string>(type: "text", nullable: true),
                    MetricScore = table.Column<float>(type: "real", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    PotentialScoreScoreID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreSubMetrics", x => x.SubMetricID);
                    table.ForeignKey(
                        name: "FK_ScoreSubMetrics_StartupPotentialScores_PotentialScoreScoreID",
                        column: x => x.PotentialScoreScoreID,
                        principalTable: "StartupPotentialScores",
                        principalColumn: "ScoreID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MentorshipFeedbacks",
                columns: table => new
                {
                    FeedbackID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MentorshipID = table.Column<int>(type: "integer", nullable: false),
                    SessionID = table.Column<int>(type: "integer", nullable: true),
                    FromRole = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorshipFeedbacks", x => x.FeedbackID);
                    table.ForeignKey(
                        name: "FK_MentorshipFeedbacks_MentorshipSessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "MentorshipSessions",
                        principalColumn: "SessionID");
                    table.ForeignKey(
                        name: "FK_MentorshipFeedbacks_StartupAdvisorMentorships_MentorshipID",
                        column: x => x.MentorshipID,
                        principalTable: "StartupAdvisorMentorships",
                        principalColumn: "MentorshipID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MentorshipReports",
                columns: table => new
                {
                    ReportID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MentorshipID = table.Column<int>(type: "integer", nullable: false),
                    SessionID = table.Column<int>(type: "integer", nullable: true),
                    CreatedByAdvisorID = table.Column<int>(type: "integer", nullable: true),
                    ReportSummary = table.Column<string>(type: "text", nullable: true),
                    DetailedFindings = table.Column<string>(type: "text", nullable: true),
                    Recommendations = table.Column<string>(type: "text", nullable: true),
                    AttachmentsURL = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedByStaff = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorshipReports", x => x.ReportID);
                    table.ForeignKey(
                        name: "FK_MentorshipReports_MentorshipSessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "MentorshipSessions",
                        principalColumn: "SessionID");
                    table.ForeignKey(
                        name: "FK_MentorshipReports_StartupAdvisorMentorships_MentorshipID",
                        column: x => x.MentorshipID,
                        principalTable: "StartupAdvisorMentorships",
                        principalColumn: "MentorshipID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    MessageID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationID = table.Column<int>(type: "integer", nullable: false),
                    SenderUserID = table.Column<int>(type: "integer", nullable: false),
                    MessageText = table.Column<string>(type: "text", nullable: false),
                    AttachmentURLs = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.MessageID);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationID",
                        column: x => x.ConversationID,
                        principalTable: "Conversations",
                        principalColumn: "ConversationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Users_SenderUserID",
                        column: x => x.SenderUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorAchievements_AdvisorID",
                table: "AdvisorAchievements",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorAvailabilities_AdvisorID",
                table: "AdvisorAvailabilities",
                column: "AdvisorID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorExpertises_AdvisorID",
                table: "AdvisorExpertises",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorIndustryFocuses_AdvisorID",
                table: "AdvisorIndustryFocuses",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_Advisors_UserID",
                table: "Advisors",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorTestimonials_AdvisorID",
                table: "AdvisorTestimonials",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorTestimonials_MentorshipID",
                table: "AdvisorTestimonials",
                column: "MentorshipID");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorTestimonials_StartupID",
                table: "AdvisorTestimonials",
                column: "StartupID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserID",
                table: "AuditLogs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ConnectionID",
                table: "Conversations",
                column: "ConnectionID");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_MentorshipID",
                table: "Conversations",
                column: "MentorshipID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBlockchainProofs_AnchoredByUserUserID",
                table: "DocumentBlockchainProofs",
                column: "AnchoredByUserUserID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBlockchainProofs_DocumentID",
                table: "DocumentBlockchainProofs",
                column: "DocumentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StartupID",
                table: "Documents",
                column: "StartupID");

            migrationBuilder.CreateIndex(
                name: "IX_FlaggedContents_RelatedUserID",
                table: "FlaggedContents",
                column: "RelatedUserID");

            migrationBuilder.CreateIndex(
                name: "IX_FlaggedContents_ReviewedBy",
                table: "FlaggedContents",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Industries_ParentIndustryID",
                table: "Industries",
                column: "ParentIndustryID");

            migrationBuilder.CreateIndex(
                name: "IX_IndustryTrends_IndustryID",
                table: "IndustryTrends",
                column: "IndustryID");

            migrationBuilder.CreateIndex(
                name: "IX_InformationRequests_ConnectionID",
                table: "InformationRequests",
                column: "ConnectionID");

            migrationBuilder.CreateIndex(
                name: "IX_InformationRequests_InvestorID",
                table: "InformationRequests",
                column: "InvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorIndustryFocuses_InvestorID",
                table: "InvestorIndustryFocuses",
                column: "InvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorPreferences_InvestorID",
                table: "InvestorPreferences",
                column: "InvestorID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Investors_UserID",
                table: "Investors",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestorStageFocuses_InvestorID",
                table: "InvestorStageFocuses",
                column: "InvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorWatchlists_InvestorID",
                table: "InvestorWatchlists",
                column: "InvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorWatchlists_StartupID",
                table: "InvestorWatchlists",
                column: "StartupID");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipFeedbacks_MentorshipID",
                table: "MentorshipFeedbacks",
                column: "MentorshipID");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipFeedbacks_SessionID",
                table: "MentorshipFeedbacks",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipReports_MentorshipID",
                table: "MentorshipReports",
                column: "MentorshipID");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipReports_SessionID",
                table: "MentorshipReports",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipSessions_MentorshipID",
                table: "MentorshipSessions",
                column: "MentorshipID");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationID",
                table: "Messages",
                column: "ConversationID");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderUserID",
                table: "Messages",
                column: "SenderUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationActions_ActionTakenBy",
                table: "ModerationActions",
                column: "ActionTakenBy");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationActions_FlaggedContentFlagID",
                table: "ModerationActions",
                column: "FlaggedContentFlagID");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationActions_TargetUserID",
                table: "ModerationActions",
                column: "TargetUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioCompanies_InvestorID",
                table: "PortfolioCompanies",
                column: "InvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileViews_ViewedAdvisorID",
                table: "ProfileViews",
                column: "ViewedAdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileViews_ViewedInvestorID",
                table: "ProfileViews",
                column: "ViewedInvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileViews_ViewedStartupID",
                table: "ProfileViews",
                column: "ViewedStartupID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileViews_ViewerUserID",
                table: "ProfileViews",
                column: "ViewerUserID");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionID",
                table: "RolePermissions",
                column: "PermissionID");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleID",
                table: "RolePermissions",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_SavedReports_CreatedByUserUserID",
                table: "SavedReports",
                column: "CreatedByUserUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreImprovementRecommendations_PotentialScoreScoreID",
                table: "ScoreImprovementRecommendations",
                column: "PotentialScoreScoreID");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreSubMetrics_PotentialScoreScoreID",
                table: "ScoreSubMetrics",
                column: "PotentialScoreScoreID");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringModelConfigurations_CreatedByUserUserID",
                table: "ScoringModelConfigurations",
                column: "CreatedByUserUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StartupAdvisorMentorships_AdvisorID",
                table: "StartupAdvisorMentorships",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_StartupAdvisorMentorships_StartupID",
                table: "StartupAdvisorMentorships",
                column: "StartupID");

            migrationBuilder.CreateIndex(
                name: "IX_StartupInvestorConnections_InvestorID",
                table: "StartupInvestorConnections",
                column: "InvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_StartupInvestorConnections_StartupID",
                table: "StartupInvestorConnections",
                column: "StartupID");

            migrationBuilder.CreateIndex(
                name: "IX_StartupPotentialScores_ScoringConfigurationConfigID",
                table: "StartupPotentialScores",
                column: "ScoringConfigurationConfigID");

            migrationBuilder.CreateIndex(
                name: "IX_StartupPotentialScores_StartupID",
                table: "StartupPotentialScores",
                column: "StartupID");

            migrationBuilder.CreateIndex(
                name: "IX_Startups_ApprovedBy",
                table: "Startups",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Startups_UserID",
                table: "Startups",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedByUserUserID",
                table: "SystemSettings",
                column: "UpdatedByUserUserID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_StartupID",
                table: "TeamMembers",
                column: "StartupID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_AssignedBy",
                table: "UserRoles",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleID",
                table: "UserRoles",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserID",
                table: "UserRoles",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvisorAchievements");

            migrationBuilder.DropTable(
                name: "AdvisorAvailabilities");

            migrationBuilder.DropTable(
                name: "AdvisorExpertises");

            migrationBuilder.DropTable(
                name: "AdvisorIndustryFocuses");

            migrationBuilder.DropTable(
                name: "AdvisorTestimonials");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DocumentBlockchainProofs");

            migrationBuilder.DropTable(
                name: "IndustryTrends");

            migrationBuilder.DropTable(
                name: "InformationRequests");

            migrationBuilder.DropTable(
                name: "InvestorIndustryFocuses");

            migrationBuilder.DropTable(
                name: "InvestorPreferences");

            migrationBuilder.DropTable(
                name: "InvestorStageFocuses");

            migrationBuilder.DropTable(
                name: "InvestorWatchlists");

            migrationBuilder.DropTable(
                name: "MentorshipFeedbacks");

            migrationBuilder.DropTable(
                name: "MentorshipReports");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "ModerationActions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PlatformAnalytics");

            migrationBuilder.DropTable(
                name: "PortfolioCompanies");

            migrationBuilder.DropTable(
                name: "ProfileViews");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SavedReports");

            migrationBuilder.DropTable(
                name: "ScoreImprovementRecommendations");

            migrationBuilder.DropTable(
                name: "ScoreSubMetrics");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Industries");

            migrationBuilder.DropTable(
                name: "MentorshipSessions");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "FlaggedContents");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "StartupPotentialScores");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "StartupAdvisorMentorships");

            migrationBuilder.DropTable(
                name: "StartupInvestorConnections");

            migrationBuilder.DropTable(
                name: "ScoringModelConfigurations");

            migrationBuilder.DropTable(
                name: "Advisors");

            migrationBuilder.DropTable(
                name: "Investors");

            migrationBuilder.DropTable(
                name: "Startups");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
