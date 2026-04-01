using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnumStandardization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ----------------------------------------------------------
            //  1) Startups — IndustryID FK (populate from old Industry string)
            // ----------------------------------------------------------
            migrationBuilder.AddColumn<int>(
                name: "IndustryID",
                table: "Startups",
                type: "integer",
                nullable: true);

            // Map existing Industry text ? IndustryID via Industries lookup
            migrationBuilder.Sql("""
                UPDATE "Startups" s
                SET    "IndustryID" = i."IndustryID"
                FROM   "Industries" i
                WHERE  LOWER(s."Industry") = LOWER(i."IndustryName")
                  AND  s."Industry" IS NOT NULL;
            """);

            migrationBuilder.DropColumn(name: "Industry", table: "Startups");
            migrationBuilder.DropColumn(name: "FundingStage", table: "Startups");

            migrationBuilder.CreateIndex(
                name: "IX_Startups_IndustryID",
                table: "Startups",
                column: "IndustryID");

            migrationBuilder.AddForeignKey(
                name: "FK_Startups_Industries_IndustryID",
                table: "Startups",
                column: "IndustryID",
                principalTable: "Industries",
                principalColumn: "IndustryID",
                onDelete: ReferentialAction.Restrict);

            // ----------------------------------------------------------
            //  2) Startups.Stage  (text ? smallint)
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "Startups"
                ALTER COLUMN "Stage" TYPE smallint
                USING CASE "Stage"
                    WHEN 'Idea'        THEN 0
                    WHEN 'Pre-Seed'    THEN 1  WHEN 'PreSeed'   THEN 1
                    WHEN 'Seed'        THEN 2
                    WHEN 'Series A'    THEN 3  WHEN 'SeriesA'   THEN 3
                    WHEN 'Series B'    THEN 4  WHEN 'SeriesB'   THEN 4
                    WHEN 'Series C+'   THEN 5  WHEN 'SeriesC'   THEN 5
                    WHEN 'Growth'      THEN 6
                    WHEN 'IPO Ready'   THEN 6
                    ELSE NULL
                END;
            """);

            // ----------------------------------------------------------
            //  3) Startups.ProfileStatus  (text? ? smallint NOT NULL default 0)
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "Startups"
                ALTER COLUMN "ProfileStatus" TYPE smallint
                USING CASE "ProfileStatus"
                    WHEN 'Draft'           THEN 0
                    WHEN 'Pending'         THEN 1  WHEN 'PendingApproval' THEN 1
                    WHEN 'Approved'        THEN 2
                    WHEN 'Rejected'        THEN 3
                    ELSE 0
                END;
                ALTER TABLE "Startups" ALTER COLUMN "ProfileStatus" SET NOT NULL;
                ALTER TABLE "Startups" ALTER COLUMN "ProfileStatus" SET DEFAULT 0;
            """);

            // ----------------------------------------------------------
            //  4) StartupInvestorConnections.ConnectionStatus
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "StartupInvestorConnections"
                ALTER COLUMN "ConnectionStatus" TYPE smallint
                USING CASE "ConnectionStatus"
                    WHEN 'Sent'          THEN 0  WHEN 'Requested'    THEN 0
                    WHEN 'Accepted'      THEN 1
                    WHEN 'Rejected'      THEN 2
                    WHEN 'Withdrawn'     THEN 3
                    WHEN 'InDiscussion'  THEN 4
                    WHEN 'Closed'        THEN 5
                    ELSE 0
                END;
                ALTER TABLE "StartupInvestorConnections" ALTER COLUMN "ConnectionStatus" SET DEFAULT 0;
            """);

            // ----------------------------------------------------------
            //  5) StartupAdvisorMentorships.MentorshipStatus
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "StartupAdvisorMentorships"
                ALTER COLUMN "MentorshipStatus" TYPE smallint
                USING CASE "MentorshipStatus"
                    WHEN 'Requested'   THEN 0
                    WHEN 'Rejected'    THEN 1
                    WHEN 'Accepted'    THEN 2
                    WHEN 'InProgress'  THEN 3
                    WHEN 'Completed'   THEN 4
                    WHEN 'InDispute'   THEN 5
                    WHEN 'Resolved'    THEN 6
                    WHEN 'Cancelled'   THEN 7
                    WHEN 'Expired'     THEN 8
                    ELSE 0
                END;
                ALTER TABLE "StartupAdvisorMentorships" ALTER COLUMN "MentorshipStatus" SET DEFAULT 0;
            """);

            // ----------------------------------------------------------
            //  6) ScoreImprovementRecommendations.Priority
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "ScoreImprovementRecommendations"
                ALTER COLUMN "Priority" TYPE smallint
                USING CASE "Priority"
                    WHEN 'Low'      THEN 0
                    WHEN 'Medium'   THEN 1
                    WHEN 'High'     THEN 2
                    WHEN 'Critical' THEN 3
                    ELSE 1
                END;
            """);

            // ----------------------------------------------------------
            //  7) SavedReports.ReportType
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "SavedReports"
                ALTER COLUMN "ReportType" TYPE smallint
                USING CASE "ReportType"
                    WHEN 'StartupAnalytics'    THEN 0
                    WHEN 'InvestorPortfolio'   THEN 1
                    WHEN 'AdvisorPerformance'  THEN 2
                    WHEN 'PlatformStatistics'  THEN 3
                    WHEN 'FundingTrends'       THEN 4
                    ELSE 0
                END;
            """);

            // ----------------------------------------------------------
            //  8) PortfolioCompanies.InvestmentStage  (nullable)
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "PortfolioCompanies"
                ALTER COLUMN "InvestmentStage" TYPE smallint
                USING CASE "InvestmentStage"
                    WHEN 'Pre-Seed'   THEN 0  WHEN 'PreSeed'    THEN 0
                    WHEN 'Seed'       THEN 1
                    WHEN 'Series A'   THEN 2  WHEN 'SeriesA'    THEN 2
                    WHEN 'Series B'   THEN 3  WHEN 'SeriesB'    THEN 3
                    WHEN 'Series C'   THEN 4  WHEN 'SeriesC'    THEN 4
                    WHEN 'Growth'     THEN 5
                    WHEN 'Late Stage' THEN 6  WHEN 'LateStage'  THEN 6
                    ELSE NULL
                END;
            """);

            // ----------------------------------------------------------
            //  9) PortfolioCompanies.CurrentStatus  (nullable)
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "PortfolioCompanies"
                ALTER COLUMN "CurrentStatus" TYPE smallint
                USING CASE "CurrentStatus"
                    WHEN 'Active'   THEN 0
                    WHEN 'Acquired' THEN 1
                    WHEN 'IPO'      THEN 2
                    WHEN 'Closed'   THEN 3
                    WHEN 'Unknown'  THEN 4
                    ELSE NULL
                END;
            """);

            // ----------------------------------------------------------
            //  10) PortfolioCompanies.ExitType  (nullable)
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "PortfolioCompanies"
                ALTER COLUMN "ExitType" TYPE smallint
                USING CASE "ExitType"
                    WHEN 'Acquisition'     THEN 0
                    WHEN 'IPO'             THEN 1
                    WHEN 'Secondary Sale'  THEN 2  WHEN 'SecondarySale' THEN 2
                    WHEN 'Write-Off'       THEN 3  WHEN 'WriteOff'     THEN 3
                    WHEN 'Unknown'         THEN 4
                    ELSE NULL
                END;
            """);

            // ----------------------------------------------------------
            //  11) InvestorWatchlists.Priority
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "InvestorWatchlists"
                ALTER COLUMN "Priority" TYPE smallint
                USING CASE "Priority"
                    WHEN 'Low'    THEN 0
                    WHEN 'Medium' THEN 1
                    WHEN 'High'   THEN 2
                    ELSE 1
                END;
                ALTER TABLE "InvestorWatchlists" ALTER COLUMN "Priority" SET DEFAULT 1;
            """);

            // ----------------------------------------------------------
            //  12) InvestorStageFocuses.Stage
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "InvestorStageFocuses"
                ALTER COLUMN "Stage" TYPE smallint
                USING CASE "Stage"
                    WHEN 'Idea'       THEN 0
                    WHEN 'Pre-Seed'   THEN 1  WHEN 'PreSeed'  THEN 1
                    WHEN 'Seed'       THEN 2
                    WHEN 'Series A'   THEN 3  WHEN 'SeriesA'  THEN 3
                    WHEN 'Series B'   THEN 4  WHEN 'SeriesB'  THEN 4
                    WHEN 'Series C'   THEN 5  WHEN 'SeriesC'  THEN 5
                    WHEN 'Growth'     THEN 6
                    ELSE 0
                END;
            """);

            // ----------------------------------------------------------
            //  13) Investors.ProfileStatus  (NEW column)
            // ----------------------------------------------------------
            migrationBuilder.AddColumn<short>(
                name: "ProfileStatus",
                table: "Investors",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            // ----------------------------------------------------------
            //  14) InformationRequests.RequestStatus
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "InformationRequests"
                ALTER COLUMN "RequestStatus" TYPE smallint
                USING CASE "RequestStatus"
                    WHEN 'Pending'   THEN 0
                    WHEN 'Approved'  THEN 1  WHEN 'Fulfilled' THEN 1
                    WHEN 'Rejected'  THEN 2
                    WHEN 'Cancelled' THEN 3
                    WHEN 'Expired'   THEN 4
                    ELSE 0
                END;
                ALTER TABLE "InformationRequests" ALTER COLUMN "RequestStatus" SET DEFAULT 0;
            """);

            // ----------------------------------------------------------
            //  15) FlaggedContents.ModerationStatus + drop ModerationAction
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "FlaggedContents"
                ALTER COLUMN "ModerationStatus" TYPE smallint
                USING CASE "ModerationStatus"
                    WHEN 'Pending'   THEN 0  WHEN 'None'    THEN 0
                    WHEN 'InReview'  THEN 0
                    WHEN 'Resolved'  THEN 1  WHEN 'Approve' THEN 1
                    WHEN 'Rejected'  THEN 2  WHEN 'Reject'  THEN 2
                    WHEN 'Flag'      THEN 3
                    WHEN 'Remove'    THEN 4
                    WHEN 'Warn'      THEN 5
                    WHEN 'Suspend'   THEN 6
                    WHEN 'Ban'       THEN 7
                    ELSE 0
                END;
                ALTER TABLE "FlaggedContents" ALTER COLUMN "ModerationStatus" SET DEFAULT 0;
            """);

            migrationBuilder.DropColumn(name: "ModerationAction", table: "FlaggedContents");

            // ----------------------------------------------------------
            //  16) DocumentBlockchainProofs.ProofStatus
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "DocumentBlockchainProofs"
                ALTER COLUMN "ProofStatus" TYPE smallint
                USING CASE "ProofStatus"
                    WHEN 'Anchored'     THEN 0  WHEN 'Confirmed' THEN 0
                    WHEN 'Revoked'      THEN 1
                    WHEN 'HashComputed' THEN 2
                    WHEN 'Pending'      THEN 3
                    ELSE 0
                END;
                ALTER TABLE "DocumentBlockchainProofs" ALTER COLUMN "ProofStatus" SET DEFAULT 0;
            """);

            // ----------------------------------------------------------
            //  17) Conversations.ConversationStatus
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "Conversations"
                ALTER COLUMN "ConversationStatus" TYPE smallint
                USING CASE "ConversationStatus"
                    WHEN 'Open'     THEN 0  WHEN 'Active'   THEN 0
                    WHEN 'Archived' THEN 1
                    WHEN 'Closed'   THEN 2
                    WHEN 'Deleted'  THEN 3
                    ELSE 0
                END;
                ALTER TABLE "Conversations" ALTER COLUMN "ConversationStatus" SET DEFAULT 0;
            """);

            // ----------------------------------------------------------
            //  18) Advisors.ProfileStatus  (text? ? smallint NOT NULL default 0)
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                UPDATE "Advisors" SET "ProfileStatus" = 'Draft' WHERE "ProfileStatus" IS NULL;
                ALTER TABLE "Advisors"
                ALTER COLUMN "ProfileStatus" TYPE smallint
                USING CASE "ProfileStatus"
                    WHEN 'Draft'    THEN 0
                    WHEN 'Pending'  THEN 1
                    WHEN 'Approved' THEN 2
                    WHEN 'Rejected' THEN 3
                    ELSE 0
                END;
                ALTER TABLE "Advisors" ALTER COLUMN "ProfileStatus" SET NOT NULL;
                ALTER TABLE "Advisors" ALTER COLUMN "ProfileStatus" SET DEFAULT 0;
            """);

            // ----------------------------------------------------------
            //  19) AdvisorExpertises.ProficiencyLevel  (nullable)
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "AdvisorExpertises"
                ALTER COLUMN "ProficiencyLevel" TYPE smallint
                USING CASE "ProficiencyLevel"
                    WHEN 'Beginner'     THEN 0
                    WHEN 'Elementary'   THEN 1
                    WHEN 'Intermediate' THEN 2
                    WHEN 'Advanced'     THEN 3
                    WHEN 'Expert'       THEN 4
                    ELSE NULL
                END;
            """);

            // ----------------------------------------------------------
            //  20) AdvisorAchievements.AchievementType
            // ----------------------------------------------------------
            migrationBuilder.Sql("""
                ALTER TABLE "AdvisorAchievements"
                ALTER COLUMN "AchievementType" TYPE smallint
                USING CASE "AchievementType"
                    WHEN 'Activity'     THEN 0
                    WHEN 'Contribution' THEN 1
                    WHEN 'Milestone'    THEN 2
                    WHEN 'Rank'         THEN 3
                    WHEN 'Special'      THEN 4
                    ELSE 0
                END;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // -- Reverse all smallint ? text conversions --------------
            migrationBuilder.Sql("""
                ALTER TABLE "AdvisorAchievements"
                ALTER COLUMN "AchievementType" TYPE text
                USING CASE "AchievementType"
                    WHEN 0 THEN 'Activity' WHEN 1 THEN 'Contribution' WHEN 2 THEN 'Milestone'
                    WHEN 3 THEN 'Rank' WHEN 4 THEN 'Special' ELSE 'Activity' END;

                ALTER TABLE "AdvisorExpertises"
                ALTER COLUMN "ProficiencyLevel" TYPE text
                USING CASE "ProficiencyLevel"
                    WHEN 0 THEN 'Beginner' WHEN 1 THEN 'Elementary' WHEN 2 THEN 'Intermediate'
                    WHEN 3 THEN 'Advanced' WHEN 4 THEN 'Expert' ELSE NULL END;

                ALTER TABLE "Advisors" ALTER COLUMN "ProfileStatus" DROP DEFAULT;
                ALTER TABLE "Advisors" ALTER COLUMN "ProfileStatus" DROP NOT NULL;
                ALTER TABLE "Advisors"
                ALTER COLUMN "ProfileStatus" TYPE text
                USING CASE "ProfileStatus"
                    WHEN 0 THEN 'Draft' WHEN 1 THEN 'Pending' WHEN 2 THEN 'Approved'
                    WHEN 3 THEN 'Rejected' ELSE NULL END;

                ALTER TABLE "Conversations" ALTER COLUMN "ConversationStatus" DROP DEFAULT;
                ALTER TABLE "Conversations"
                ALTER COLUMN "ConversationStatus" TYPE text
                USING CASE "ConversationStatus"
                    WHEN 0 THEN 'Open' WHEN 1 THEN 'Archived' WHEN 2 THEN 'Closed'
                    WHEN 3 THEN 'Deleted' ELSE 'Open' END;

                ALTER TABLE "DocumentBlockchainProofs" ALTER COLUMN "ProofStatus" DROP DEFAULT;
                ALTER TABLE "DocumentBlockchainProofs"
                ALTER COLUMN "ProofStatus" TYPE text
                USING CASE "ProofStatus"
                    WHEN 0 THEN 'Confirmed' WHEN 1 THEN 'Revoked' WHEN 2 THEN 'HashComputed'
                    WHEN 3 THEN 'Pending' ELSE 'Confirmed' END;
            """);

            migrationBuilder.AddColumn<string>(
                name: "ModerationAction",
                table: "FlaggedContents",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE "FlaggedContents" ALTER COLUMN "ModerationStatus" DROP DEFAULT;
                ALTER TABLE "FlaggedContents"
                ALTER COLUMN "ModerationStatus" TYPE text
                USING CASE "ModerationStatus"
                    WHEN 0 THEN 'Pending' WHEN 1 THEN 'Resolved' WHEN 2 THEN 'Rejected'
                    ELSE 'Pending' END;

                ALTER TABLE "InformationRequests" ALTER COLUMN "RequestStatus" DROP DEFAULT;
                ALTER TABLE "InformationRequests"
                ALTER COLUMN "RequestStatus" TYPE text
                USING CASE "RequestStatus"
                    WHEN 0 THEN 'Pending' WHEN 1 THEN 'Fulfilled' WHEN 2 THEN 'Rejected'
                    WHEN 3 THEN 'Cancelled' WHEN 4 THEN 'Expired' ELSE 'Pending' END;
            """);

            migrationBuilder.DropColumn(name: "ProfileStatus", table: "Investors");

            migrationBuilder.Sql("""
                ALTER TABLE "InvestorStageFocuses"
                ALTER COLUMN "Stage" TYPE text
                USING CASE "Stage"
                    WHEN 0 THEN 'Idea' WHEN 1 THEN 'PreSeed' WHEN 2 THEN 'Seed'
                    WHEN 3 THEN 'SeriesA' WHEN 4 THEN 'SeriesB' WHEN 5 THEN 'SeriesC'
                    WHEN 6 THEN 'Growth' ELSE 'Idea' END;

                ALTER TABLE "InvestorWatchlists" ALTER COLUMN "Priority" DROP DEFAULT;
                ALTER TABLE "InvestorWatchlists"
                ALTER COLUMN "Priority" TYPE text
                USING CASE "Priority"
                    WHEN 0 THEN 'Low' WHEN 1 THEN 'Medium' WHEN 2 THEN 'High' ELSE 'Medium' END;

                ALTER TABLE "PortfolioCompanies"
                ALTER COLUMN "ExitType" TYPE text
                USING CASE "ExitType"
                    WHEN 0 THEN 'Acquisition' WHEN 1 THEN 'IPO' WHEN 2 THEN 'SecondarySale'
                    WHEN 3 THEN 'WriteOff' WHEN 4 THEN 'Unknown' ELSE NULL END;

                ALTER TABLE "PortfolioCompanies"
                ALTER COLUMN "CurrentStatus" TYPE text
                USING CASE "CurrentStatus"
                    WHEN 0 THEN 'Active' WHEN 1 THEN 'Acquired' WHEN 2 THEN 'IPO'
                    WHEN 3 THEN 'Closed' WHEN 4 THEN 'Unknown' ELSE NULL END;

                ALTER TABLE "PortfolioCompanies"
                ALTER COLUMN "InvestmentStage" TYPE text
                USING CASE "InvestmentStage"
                    WHEN 0 THEN 'PreSeed' WHEN 1 THEN 'Seed' WHEN 2 THEN 'SeriesA'
                    WHEN 3 THEN 'SeriesB' WHEN 4 THEN 'SeriesC' WHEN 5 THEN 'Growth'
                    WHEN 6 THEN 'LateStage' ELSE NULL END;

                ALTER TABLE "SavedReports"
                ALTER COLUMN "ReportType" TYPE text
                USING CASE "ReportType"
                    WHEN 0 THEN 'StartupAnalytics' WHEN 1 THEN 'InvestorPortfolio'
                    WHEN 2 THEN 'AdvisorPerformance' WHEN 3 THEN 'PlatformStatistics'
                    WHEN 4 THEN 'FundingTrends' ELSE 'StartupAnalytics' END;

                ALTER TABLE "ScoreImprovementRecommendations"
                ALTER COLUMN "Priority" TYPE text
                USING CASE "Priority"
                    WHEN 0 THEN 'Low' WHEN 1 THEN 'Medium' WHEN 2 THEN 'High'
                    WHEN 3 THEN 'Critical' ELSE 'Medium' END;

                ALTER TABLE "StartupAdvisorMentorships" ALTER COLUMN "MentorshipStatus" DROP DEFAULT;
                ALTER TABLE "StartupAdvisorMentorships"
                ALTER COLUMN "MentorshipStatus" TYPE text
                USING CASE "MentorshipStatus"
                    WHEN 0 THEN 'Requested' WHEN 1 THEN 'Rejected' WHEN 2 THEN 'Accepted'
                    WHEN 3 THEN 'InProgress' WHEN 4 THEN 'Completed' WHEN 5 THEN 'InDispute'
                    WHEN 6 THEN 'Resolved' WHEN 7 THEN 'Cancelled' WHEN 8 THEN 'Expired'
                    ELSE 'Requested' END;

                ALTER TABLE "StartupInvestorConnections" ALTER COLUMN "ConnectionStatus" DROP DEFAULT;
                ALTER TABLE "StartupInvestorConnections"
                ALTER COLUMN "ConnectionStatus" TYPE text
                USING CASE "ConnectionStatus"
                    WHEN 0 THEN 'Sent' WHEN 1 THEN 'Accepted' WHEN 2 THEN 'Rejected'
                    WHEN 3 THEN 'Withdrawn' WHEN 4 THEN 'InDiscussion' WHEN 5 THEN 'Closed'
                    ELSE 'Sent' END;

                ALTER TABLE "Startups" ALTER COLUMN "ProfileStatus" DROP DEFAULT;
                ALTER TABLE "Startups" ALTER COLUMN "ProfileStatus" DROP NOT NULL;
                ALTER TABLE "Startups"
                ALTER COLUMN "ProfileStatus" TYPE text
                USING CASE "ProfileStatus"
                    WHEN 0 THEN 'Draft' WHEN 1 THEN 'PendingApproval' WHEN 2 THEN 'Approved'
                    WHEN 3 THEN 'Rejected' ELSE NULL END;

                ALTER TABLE "Startups"
                ALTER COLUMN "Stage" TYPE text
                USING CASE "Stage"
                    WHEN 0 THEN 'Idea' WHEN 1 THEN 'Pre-Seed' WHEN 2 THEN 'Seed'
                    WHEN 3 THEN 'Series A' WHEN 4 THEN 'Series B' WHEN 5 THEN 'Series C+'
                    WHEN 6 THEN 'Growth' ELSE NULL END;
            """);

            migrationBuilder.DropForeignKey(
                name: "FK_Startups_Industries_IndustryID",
                table: "Startups");

            migrationBuilder.DropIndex(
                name: "IX_Startups_IndustryID",
                table: "Startups");

            // Restore Industry string column from IndustryID FK
            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Startups" s
                SET    "Industry" = i."IndustryName"
                FROM   "Industries" i
                WHERE  s."IndustryID" = i."IndustryID";
            """);

            migrationBuilder.DropColumn(name: "IndustryID", table: "Startups");

            migrationBuilder.AddColumn<string>(
                name: "FundingStage",
                table: "Startups",
                type: "text",
                nullable: true);
        }
    }
}
