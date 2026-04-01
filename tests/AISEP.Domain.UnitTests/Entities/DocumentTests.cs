using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using FluentAssertions;

namespace AISEP.Domain.UnitTests.Entities;

public class DocumentTests
{
    [Fact]
    public void DocumentConstruction_AnalysisStatusDefaultNotAnalyzed_ReturnsTrue()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        document.AnalysisStatus.Should().Be(AnalysisStatus.NOTANALYZE);
    }

    [Fact]
    public void DocumentConstruction_IsAnalyzedDefaultFalse_ReturnsTrue()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        document.IsAnalyzed.Should().BeFalse();
    }

    [Fact]
    public void DocumentConstruction_IsArchivedDefaultFalse_ReturnsTrue()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        document.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void Document_WithDocumentTypeAndTitle_StoresCorrectly()
    {
        // Arrange
        var document = new Document
        {
            DocumentType = DocumentType.Bussiness_Plan,
            Title = "Q1 2024 Business Plan",
        };

        // Act & Assert
        document.DocumentType.Should().Be(DocumentType.Bussiness_Plan);
        document.Title.Should().Be("Q1 2024 Business Plan");
    }

    [Fact]
    public void Document_WithFileURLAndVersion_StoresCorrectly()
    {
        // Arrange
        var document = new Document();
        var fileUrl = "https://storage.example.com/docs/businessplan-v2.pdf";
        var version = "2.0";

        // Act
        document.FileURL = fileUrl;
        document.Version = version;

        // Assert
        document.FileURL.Should().Be(fileUrl);
        document.Version.Should().Be(version);
    }

    [Fact]
    public void Document_WithAnalysisTimestamps_StoresCorrectly()
    {
        // Arrange
        var document = new Document();
        var uploadedTime = DateTime.UtcNow;
        var analyzedTime = DateTime.UtcNow.AddHours(2);

        // Act
        document.UploadedAt = uploadedTime;
        document.AnalyzedAt = analyzedTime;
        document.IsAnalyzed = true;
        document.AnalysisStatus = AnalysisStatus.COMPLETED;

        // Assert
        document.UploadedAt.Should().Be(uploadedTime);
        document.AnalyzedAt.Should().Be(analyzedTime);
        document.IsAnalyzed.Should().BeTrue();
        document.AnalysisStatus.Should().Be(AnalysisStatus.COMPLETED);
    }

    [Fact]
    public void Document_WithArchiveStatus_StoresCorrectly()
    {
        // Arrange
        var document = new Document();
        var archivedTime = DateTime.UtcNow.AddMonths(6);

        // Act
        document.IsArchived = true;
        document.ArchivedAt = archivedTime;

        // Assert
        document.IsArchived.Should().BeTrue();
        document.ArchivedAt.Should().Be(archivedTime);
    }
}
