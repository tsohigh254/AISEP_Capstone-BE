using System.Net;
using System.Text;
using AISEP.Application.Configuration;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
using AISEP.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AISEP.Tests.Services;

public class AiEvaluationServiceTests
{
    [Fact]
    public async Task GetEvaluationReportAsync_PitchDeckOnlyRun_DoesNotCallBusinessPlanSourceEndpoint()
    {
        using var db = TestDbContextFactory.Create();
        var startup = SeedStartup(db, userId: 10);
        var run = SeedCompletedRunWithCachedReport(db, startup.StartupID, "pitch_deck");

        var handler = new RecordingPythonHandler();
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://python-ai.local")
        };
        var options = Options.Create(new PythonAiOptions());
        var pythonClient = new PythonAiClient(http, options, Mock.Of<ILogger<PythonAiClient>>());
        var sut = new AiEvaluationService(
            db,
            pythonClient,
            Mock.Of<ICloudinaryService>(),
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<ILogger<AiEvaluationService>>(),
            Mock.Of<AISEP.Application.Interfaces.INotificationDeliveryService>());

        var result = await sut.GetEvaluationReportAsync(run.Id, currentUserId: startup.UserID, currentUserType: "Startup");

        result.Success.Should().BeTrue();
        handler.RequestedPaths.Should().Contain($"/api/v1/evaluations/{run.PythonRunId}");
        handler.RequestedPaths.Should().Contain($"/api/v1/evaluations/{run.PythonRunId}/report/source/pitch_deck");
        handler.RequestedPaths.Should().NotContain($"/api/v1/evaluations/{run.PythonRunId}/report/source/business_plan");
    }

    private static Startup SeedStartup(ApplicationDbContext db, int userId)
    {
        var startup = new Startup
        {
            UserID = userId,
            CompanyName = "Test Startup",
            OneLiner = "Test",
            FullNameOfApplicant = "Owner",
            RoleOfApplicant = "Founder",
            ContactEmail = "owner@test.local",
            BusinessCode = "BIZ",
            CreatedAt = DateTime.UtcNow
        };
        db.Startups.Add(startup);
        db.SaveChanges();
        return startup;
    }

    private static AiEvaluationRun SeedCompletedRunWithCachedReport(ApplicationDbContext db, int startupId, string evaluatedTypesCsv)
    {
        var reportJson = """
                         {
                           "startup_id": "1",
                           "status": "completed",
                           "overall_result": { "overall_score": 7.4 },
                           "criteria_results": [
                             { "criterion": "Team", "final_score": 7.4, "status": "scored" }
                           ]
                         }
                         """;

        var run = new AiEvaluationRun
        {
            StartupId = startupId,
            PythonRunId = 128,
            Status = "completed",
            CorrelationId = "corr-1",
            ReportJson = reportJson,
            IsReportValid = true,
            EvaluatedDocumentTypes = evaluatedTypesCsv,
            SubmittedAt = DateTime.UtcNow.AddMinutes(-1),
            UpdatedAt = DateTime.UtcNow
        };
        db.AiEvaluationRuns.Add(run);
        db.SaveChanges();
        return run;
    }

    private sealed class RecordingPythonHandler : HttpMessageHandler
    {
        public List<string> RequestedPaths { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            RequestedPaths.Add(path);

            if (path.EndsWith("/api/v1/evaluations/128", StringComparison.Ordinal))
            {
                return Task.FromResult(Json(HttpStatusCode.OK, """
                    {
                      "id": 128,
                      "startup_id": "1",
                      "status": "completed",
                      "evaluation_mode": "pitch_deck_only",
                      "has_pitch_deck_result": true,
                      "has_business_plan_result": false,
                      "has_merged_result": true
                    }
                    """));
            }

            if (path.EndsWith("/api/v1/evaluations/128/report/source/pitch_deck", StringComparison.Ordinal))
            {
                return Task.FromResult(Json(HttpStatusCode.OK, """
                    {
                      "report_mode": "source",
                      "evaluation_mode": "pitch_deck_only",
                      "has_merged_result": true,
                      "available_sources": ["pitch_deck"],
                      "source_document_type": "pitch_deck",
                      "report": {
                        "startup_id": "1",
                        "status": "completed",
                        "overall_result": { "overall_score": 7.4 },
                        "criteria_results": [
                          { "criterion": "Team", "final_score": 7.4, "status": "scored" }
                        ]
                      }
                    }
                    """));
            }

            if (path.EndsWith("/api/v1/evaluations/128/report/source/business_plan", StringComparison.Ordinal))
            {
                return Task.FromResult(Json(HttpStatusCode.NotFound, """
                    {
                      "code": "DOCUMENT_NOT_FOUND",
                      "message": "No completed business_plan document found for run 128.",
                      "retryable": false
                    }
                    """));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage Json(HttpStatusCode code, string json)
        {
            return new HttpResponseMessage(code)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
