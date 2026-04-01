using Xunit;
using FluentAssertions;
using AISEP.Domain.Enums;

namespace AISEP.Domain.UnitTests.Enums;

public class EnumsTests
{
    [Fact]
    public void ProfileStatus_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)ProfileStatus.Draft).Should().Be(0);
        ((int)ProfileStatus.Pending).Should().Be(1);
        ((int)ProfileStatus.Approved).Should().Be(2);
        ((int)ProfileStatus.Rejected).Should().Be(3);
    }

    [Fact]
    public void ConnectionStatus_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)ConnectionStatus.Requested).Should().Be(0);
        ((int)ConnectionStatus.Accepted).Should().Be(1);
        ((int)ConnectionStatus.Rejected).Should().Be(2);
        ((int)ConnectionStatus.Withdrawn).Should().Be(3);
        ((int)ConnectionStatus.InDiscussion).Should().Be(4);
        ((int)ConnectionStatus.Closed).Should().Be(5);
    }

    [Fact]
    public void MentorshipStatus_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)MentorshipStatus.Requested).Should().Be(0);
        ((int)MentorshipStatus.Rejected).Should().Be(1);
        ((int)MentorshipStatus.Accepted).Should().Be(2);
        ((int)MentorshipStatus.InProgress).Should().Be(3);
        ((int)MentorshipStatus.Completed).Should().Be(4);
        ((int)MentorshipStatus.InDispute).Should().Be(5);
        ((int)MentorshipStatus.Resolved).Should().Be(6);
        ((int)MentorshipStatus.Cancelled).Should().Be(7);
        ((int)MentorshipStatus.Expired).Should().Be(8);
    }

    [Fact]
    public void ProofStatus_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)ProofStatus.Anchored).Should().Be(0);
        ((int)ProofStatus.Revoked).Should().Be(1);
        ((int)ProofStatus.HashComputed).Should().Be(2);
        ((int)ProofStatus.Pending).Should().Be(3);
    }

    [Fact]
    public void AnalysisStatus_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)AnalysisStatus.NOTANALYZE).Should().Be(0);
        ((int)AnalysisStatus.COMPLETED).Should().Be(1);
        ((int)AnalysisStatus.FAILED).Should().Be(2);
    }

    [Fact]
    public void ConversationStatus_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)ConversationStatus.Active).Should().Be(0);
        ((int)ConversationStatus.Archived).Should().Be(1);
        ((int)ConversationStatus.Closed).Should().Be(2);
        ((int)ConversationStatus.Deleted).Should().Be(3);
    }

    [Fact]
    public void StartupStage_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)StartupStage.Idea).Should().Be(0);
        ((int)StartupStage.PreSeed).Should().Be(1);
        ((int)StartupStage.Seed).Should().Be(2);
        ((int)StartupStage.SeriesA).Should().Be(3);
        ((int)StartupStage.SeriesB).Should().Be(4);
        ((int)StartupStage.SeriesC).Should().Be(5);
        ((int)StartupStage.Growth).Should().Be(6);
    }

    [Fact]
    public void PreferredStage_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)PreferredStage.Idea).Should().Be(0);
        ((int)PreferredStage.Validation).Should().Be(1);
        ((int)PreferredStage.MVP).Should().Be(2);
        ((int)PreferredStage.EarlyStage).Should().Be(3);
        ((int)PreferredStage.Growth).Should().Be(4);
        ((int)PreferredStage.Scale).Should().Be(5);
        ((int)PreferredStage.Mature).Should().Be(6);
    }

    [Fact]
    public void RequestStatus_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)RequestStatus.Pending).Should().Be(0);
        ((int)RequestStatus.Approved).Should().Be(1);
        ((int)RequestStatus.Rejected).Should().Be(2);
        ((int)RequestStatus.Cancelled).Should().Be(3);
        ((int)RequestStatus.Expired).Should().Be(4);
    }

    [Fact]
    public void DocumentType_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)DocumentType.Pitch_Deck).Should().Be(0);
        ((int)DocumentType.Bussiness_Plan).Should().Be(1);
    }

    [Fact]
    public void ModerationStatus_Values_Match_Expectations()
    {
        // Arrange & Act & Assert
        ((int)ModerationStatus.None).Should().Be(0);
        ((int)ModerationStatus.Approve).Should().Be(1);
        ((int)ModerationStatus.Reject).Should().Be(2);
        ((int)ModerationStatus.Flag).Should().Be(3);
        ((int)ModerationStatus.Remove).Should().Be(4);
        ((int)ModerationStatus.Warn).Should().Be(5);
        ((int)ModerationStatus.Suspend).Should().Be(6);
        ((int)ModerationStatus.Ban).Should().Be(7);
    }

    [Fact]
    public void Enum_ToStringConversion_Works()
    {
        // Arrange & Act & Assert
        ProfileStatus.Draft.ToString().Should().Be("Draft");
        ConnectionStatus.Accepted.ToString().Should().Be("Accepted");
        MentorshipStatus.Completed.ToString().Should().Be("Completed");
        ConversationStatus.Active.ToString().Should().Be("Active");
    }

    [Fact]
    public void Enum_CanParse_FromString()
    {
        // Arrange & Act
        var result1 = Enum.TryParse<ProfileStatus>("Draft", out var draft);
        var result2 = Enum.TryParse<ConnectionStatus>("Accepted", out var accepted);

        // Assert
        result1.Should().BeTrue();
        draft.Should().Be(ProfileStatus.Draft);
        result2.Should().BeTrue();
        accepted.Should().Be(ConnectionStatus.Accepted);
    }

    [Fact]
    public void Enum_CanParse_FromInt()
    {
        // Arrange & Act
        var result1 = Enum.TryParse<ProfileStatus>("0", out var draft);
        var result2 = Enum.TryParse<ConnectionStatus>("1", out var accepted);

        // Assert
        result1.Should().BeTrue();
        draft.Should().Be(ProfileStatus.Draft);
        result2.Should().BeTrue();
        accepted.Should().Be(ConnectionStatus.Accepted);
    }

    [Fact]
    public void AchievementType_Values_Match_Expectations()
    {
        // Assert
        ((short)AchievementType.Activity).Should().Be(0);
        ((short)AchievementType.Contribution).Should().Be(1);
        ((short)AchievementType.Milestone).Should().Be(2);
        ((short)AchievementType.Rank).Should().Be(3);
        ((short)AchievementType.Special).Should().Be(4);
    }

    [Fact]
    public void ProficiencyLevel_Values_Match_Expectations()
    {
        // Assert
        ((short)ProficiencyLevel.Beginner).Should().Be(0);
        ((short)ProficiencyLevel.Elementary).Should().Be(1);
        ((short)ProficiencyLevel.Intermediate).Should().Be(2);
        ((short)ProficiencyLevel.Advanced).Should().Be(3);
        ((short)ProficiencyLevel.Expert).Should().Be(4);
    }

    [Fact]
    public void WatchlistPriority_Values_Match_Expectations()
    {
        // Assert
        ((short)WatchlistPriority.Low).Should().Be(0);
        ((short)WatchlistPriority.Medium).Should().Be(1);
        ((short)WatchlistPriority.High).Should().Be(2);
    }

    [Fact]
    public void InvestmentStage_Values_Match_Expectations()
    {
        // Assert
        ((short)InvestmentStage.PreSeed).Should().Be(0);
        ((short)InvestmentStage.Seed).Should().Be(1);
        ((short)InvestmentStage.SeriesA).Should().Be(2);
        ((short)InvestmentStage.SeriesB).Should().Be(3);
        ((short)InvestmentStage.SeriesC).Should().Be(4);
        ((short)InvestmentStage.Growth).Should().Be(5);
        ((short)InvestmentStage.LateStage).Should().Be(6);
    }

    [Fact]
    public void PortfolioCompanyStatus_Values_Match_Expectations()
    {
        // Assert
        ((short)PortfolioCompanyStatus.Active).Should().Be(0);
        ((short)PortfolioCompanyStatus.Acquired).Should().Be(1);
        ((short)PortfolioCompanyStatus.IPO).Should().Be(2);
        ((short)PortfolioCompanyStatus.Closed).Should().Be(3);
        ((short)PortfolioCompanyStatus.Unknown).Should().Be(4);
    }

    [Fact]
    public void ExitType_Values_Match_Expectations()
    {
        // Assert
        ((short)ExitType.Acquisition).Should().Be(0);
        ((short)ExitType.IPO).Should().Be(1);
        ((short)ExitType.SecondarySale).Should().Be(2);
        ((short)ExitType.WriteOff).Should().Be(3);
        ((short)ExitType.Unknown).Should().Be(4);
    }

    [Fact]
    public void ReportType_Values_Match_Expectations()
    {
        // Assert
        ((short)ReportType.StartupAnalytics).Should().Be(0);
        ((short)ReportType.InvestorPortfolio).Should().Be(1);
        ((short)ReportType.AdvisorPerformance).Should().Be(2);
        ((short)ReportType.PlatformStatistics).Should().Be(3);
        ((short)ReportType.FundingTrends).Should().Be(4);
    }

    [Fact]
    public void RecommendationPriority_Values_Match_Expectations()
    {
        // Assert
        ((short)RecommendationPriority.Low).Should().Be(0);
        ((short)RecommendationPriority.Medium).Should().Be(1);
        ((short)RecommendationPriority.High).Should().Be(2);
        ((short)RecommendationPriority.Critical).Should().Be(3);
    }

    [Fact]
    public void StartupTag_Values_Match_Expectations()
    {
        // Assert
        ((short)StartupTag.VerifiedCompany).Should().Be(0);
        ((short)StartupTag.BasicVerified).Should().Be(1);
        ((short)StartupTag.PendingMoreInfo).Should().Be(2);
        ((short)StartupTag.VerificationFailed).Should().Be(3);
    }
}
