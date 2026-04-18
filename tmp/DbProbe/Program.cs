using Npgsql;

var connStr = "Host=localhost;Port=5432;Database=AISEP;Username=postgres;Password=Huydzdz123@";
using var conn = new NpgsqlConnection(connStr);
conn.Open();

// Recalculate CompletedSessions for all advisors
using (var cmd = new NpgsqlCommand(@"
    UPDATE ""Advisors"" a
    SET ""CompletedSessions"" = COALESCE(sub.cnt, 0)
    FROM (
        SELECT m.""AdvisorID"", COUNT(*) AS cnt
        FROM ""MentorshipSessions"" s
        JOIN ""StartupAdvisorMentorships"" m ON m.""MentorshipID"" = s.""MentorshipID""
        WHERE s.""SessionStatus"" = 'Completed'
        GROUP BY m.""AdvisorID""
    ) sub
    WHERE a.""AdvisorID"" = sub.""AdvisorID""
", conn))
{
    int rows = cmd.ExecuteNonQuery();
    Console.WriteLine($"Recalculated CompletedSessions for {rows} advisor(s)");
}

// Verify
using (var cmd = new NpgsqlCommand(@"
    SELECT a.""AdvisorID"", a.""FullName"", a.""CompletedSessions""
    FROM ""Advisors"" a
    ORDER BY a.""AdvisorID""
", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Advisor CompletedSessions after recalc ===");
    Console.WriteLine($"  {"AdvisorID",-10} {"FullName",-25} {"CompletedSessions"}");
    while (r.Read())
        Console.WriteLine($"  {r[0],-10} {r[1],-25} {r[2]}");
}

return;

// Fix: set IsPublic=true for all existing Startup feedbacks that are false
using (var cmd = new NpgsqlCommand(@"
    UPDATE ""MentorshipFeedbacks""
    SET ""IsPublic"" = true
    WHERE ""FromRole"" = 'Startup' AND ""IsPublic"" = false
", conn))
{
    int rows = cmd.ExecuteNonQuery();
    Console.WriteLine($"Fixed {rows} feedback(s): IsPublic false → true");
}

// Recalculate AverageRating + ReviewCount for all affected advisors
using (var cmd = new NpgsqlCommand(@"
    UPDATE ""Advisors"" a
    SET ""ReviewCount"" = sub.cnt,
        ""AverageRating"" = sub.avg
    FROM (
        SELECT m.""AdvisorID"",
               COUNT(*) AS cnt,
               AVG(f.""Rating"")::float AS avg
        FROM ""MentorshipFeedbacks"" f
        JOIN ""StartupAdvisorMentorships"" m ON m.""MentorshipID"" = f.""MentorshipID""
        WHERE f.""FromRole"" = 'Startup' AND f.""IsPublic"" = true
        GROUP BY m.""AdvisorID""
    ) sub
    WHERE a.""AdvisorID"" = sub.""AdvisorID""
", conn))
{
    int rows = cmd.ExecuteNonQuery();
    Console.WriteLine($"Recalculated rating for {rows} advisor(s)");
}

// Verify
using (var cmd = new NpgsqlCommand(@"
    SELECT f.""FeedbackID"", f.""MentorshipID"", f.""IsPublic"", f.""Rating"",
           m.""AdvisorID"", a.""AverageRating"", a.""ReviewCount""
    FROM ""MentorshipFeedbacks"" f
    JOIN ""StartupAdvisorMentorships"" m ON m.""MentorshipID"" = f.""MentorshipID""
    JOIN ""Advisors"" a ON a.""AdvisorID"" = m.""AdvisorID""
    WHERE f.""FromRole"" = 'Startup'
    ORDER BY f.""FeedbackID""
", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Verify ===");
    Console.WriteLine($"  {"FbID",-6} {"MentorshipID",-13} {"IsPublic",-9} {"Rating",-7} {"AdvisorID",-10} {"AvgRating",-11} {"ReviewCount"}");
    while (r.Read())
        Console.WriteLine($"  {r[0],-6} {r[1],-13} {r[2],-9} {r[3],-7} {r[4],-10} {r[5],-11} {r[6]}");
}

return;
using (var cmd = new NpgsqlCommand(@"
    SELECT f.""FeedbackID"", f.""MentorshipID"", f.""FromRole"", f.""Rating"",
           f.""Comment"", f.""IsPublic"", f.""SubmittedAt"",
           m.""AdvisorID"", m.""StartupID""
    FROM ""MentorshipFeedbacks"" f
    JOIN ""StartupAdvisorMentorships"" m ON m.""MentorshipID"" = f.""MentorshipID""
    WHERE f.""FromRole"" = 'Startup'
    ORDER BY f.""FeedbackID""
", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("=== MentorshipFeedbacks (FromRole=Startup) ===");
    Console.WriteLine($"  {"FeedbackID",-10} {"MentorshipID",-13} {"AdvisorID",-10} {"StartupID",-10} {"Rating",-7} {"IsPublic",-9} {"SubmittedAt",-25} {"Comment"}");
    bool any = false;
    while (r.Read())
    {
        Console.WriteLine($"  {r[0],-10} {r[1],-13} {r[7],-10} {r[8],-10} {r[3],-7} {r[5],-9} {r[6],-25} {r[4]}");
        any = true;
    }
    if (!any) Console.WriteLine("  (none)");
}

return;
using (var cmd = new NpgsqlCommand(@"
    SELECT ""AdvisorID"", ""FullName"", ""ProfilePhotoURL"", ""MentorshipPhilosophy""
    FROM ""Advisors""
    WHERE ""AdvisorID"" = 1
", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("=== Advisor 1 ===");
    if (r.Read())
    {
        Console.WriteLine($"  AdvisorID     : {r[0]}");
        Console.WriteLine($"  FullName      : {r[1]}");
        Console.WriteLine($"  ProfilePhotoURL: {r[2]}");
        Console.WriteLine($"  MentorshipPhilosophy: {r[3]}");
    }
    else Console.WriteLine("  NOT FOUND");
}

return;

using (var cmd = new NpgsqlCommand(@"
    SELECT r.""ReportID"", r.""MentorshipID"", r.""SessionID"",
           r.""ReportReviewStatus"", r.""SubmittedAt"",
           s.""SessionStatus"", s.""StartupConfirmedConductedAt""
    FROM ""MentorshipReports"" r
    LEFT JOIN ""MentorshipSessions"" s ON s.""SessionID"" = r.""SessionID""
    WHERE r.""ReportID"" = 1
", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("=== Report #1 data ===");
    if (r.Read())
        Console.WriteLine($"  ReportID={r[0]}, MentorshipID={r[1]}, SessionID={r[2]}, ReviewStatus={r[3]}, SubmittedAt={r[4]}, SessionStatus={r[5]}, ConductedAt={r[6]}");
    else
        Console.WriteLine("  Report #1 not found");
}

// CHECK: All Scheduled sessions that have a Passed report
using (var cmd = new NpgsqlCommand(@"
    SELECT s.""SessionID"", s.""MentorshipID"", s.""SessionStatus"",
           r.""ReportID"", r.""ReportReviewStatus""
    FROM ""MentorshipSessions"" s
    JOIN ""MentorshipReports"" r ON r.""SessionID"" = s.""SessionID""
    WHERE s.""SessionStatus"" = 'Scheduled'
      AND r.""ReportReviewStatus"" = 'Passed'
    ORDER BY s.""SessionID""
", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Scheduled sessions with Passed reports ===");
    bool any = false;
    while (r.Read()) { Console.WriteLine($"  SessionID={r[0]}, MentorshipID={r[1]}, SessionStatus={r[2]}, ReportID={r[3]}, ReviewStatus={r[4]}"); any = true; }
    if (!any) Console.WriteLine("  NONE");
}

Console.WriteLine("\n=== Done ===");
return;

// 1. Check migration
using (var cmd = new NpgsqlCommand("SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%AddConsultingOversight%'", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("=== Migration applied? ===");
    Console.WriteLine(r.Read() ? $"YES: {r[0]}" : "NO");
}

// 2. Remaining proposed sessions
using (var cmd = new NpgsqlCommand("SELECT \"SessionStatus\", COUNT(*) FROM \"MentorshipSessions\" WHERE \"SessionStatus\" IN ('ProposedByStartup','ProposedByAdvisor') GROUP BY \"SessionStatus\"", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Remaining Proposed sessions ===");
    bool any = false;
    while (r.Read()) { Console.WriteLine($"  {r[0]}: {r[1]}"); any = true; }
    if (!any) Console.WriteLine("  NONE — all stale sessions cancelled");
}

// 3. Recently cancelled
using (var cmd = new NpgsqlCommand(@"SELECT ""SessionID"", ""MentorshipID"", ""SessionStatus"", ""UpdatedAt"" FROM ""MentorshipSessions"" WHERE ""SessionStatus"" = 'Cancelled' AND ""UpdatedAt"" >= NOW() - INTERVAL '1 hour' ORDER BY ""MentorshipID"", ""SessionID"" LIMIT 20", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Recently cancelled (last hour) ===");
    Console.WriteLine("  SessionID | MentorshipID | Status    | UpdatedAt");
    bool any = false;
    while (r.Read()) { Console.WriteLine($"  {r[0],-9} | {r[1],-12} | {r[2],-9} | {r[3]}"); any = true; }
    if (!any) Console.WriteLine("  (none — possibly no stale data existed)");
}

// 4. New column check
using (var cmd = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name = 'MentorshipReports' AND column_name = 'ReportReviewStatus'", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== ReportReviewStatus column exists? ===");
    Console.WriteLine(r.Read() ? "  YES" : "  NO");
}

// 5. Session status distribution
using (var cmd = new NpgsqlCommand(@"SELECT ""SessionStatus"", COUNT(*) FROM ""MentorshipSessions"" GROUP BY ""SessionStatus"" ORDER BY COUNT(*) DESC", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Session status distribution ===");
    while (r.Read()) Console.WriteLine($"  {r[0],-20} : {r[1]}");
}

// 6. Detail of remaining ProposedByStartup sessions
using (var cmd = new NpgsqlCommand(@"
    SELECT s.""SessionID"", s.""MentorshipID"", s.""SessionStatus"", s.""UpdatedAt"", m.""MentorshipStatus""
    FROM ""MentorshipSessions"" s
    JOIN ""StartupAdvisorMentorships"" m ON s.""MentorshipID"" = m.""MentorshipID""
    WHERE s.""SessionStatus"" IN ('ProposedByStartup','ProposedByAdvisor')
    ORDER BY s.""MentorshipID"", s.""SessionID""", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Detail: remaining Proposed sessions ===");
    Console.WriteLine("  SessionID | MentorshipID | SessionStatus       | UpdatedAt                  | MentorshipStatus");
    while (r.Read()) Console.WriteLine($"  {r[0],-9} | {r[1],-12} | {r[2],-19} | {r[3],-26} | {r[4]}");
}

// 7a. Verify: does mentorship 2 have a Scheduled session?
using (var cmd = new NpgsqlCommand(@"
    SELECT ""SessionID"", ""SessionStatus"", ""UpdatedAt""
    FROM ""MentorshipSessions""
    WHERE ""MentorshipID"" = 2
    ORDER BY ""SessionID""", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Mentorship 2 — all sessions ===");
    while (r.Read()) Console.WriteLine($"  SessionID={r[0]} Status={r[1]} Updated={r[2]}");
}

// 7b. DB time check
using (var cmd = new NpgsqlCommand("SELECT NOW(), NOW() AT TIME ZONE 'UTC'", conn))
using (var r = cmd.ExecuteReader())
{
    r.Read();
    Console.WriteLine($"\n=== DB Time: NOW()={r[0]} UTC={r[1]} ===");
}

// 8. Manual fix: cancel stale ProposedByStartup/ProposedByAdvisor for mentorships that have Scheduled+ sessions
using (var cmd = new NpgsqlCommand(@"
    UPDATE ""MentorshipSessions""
    SET ""SessionStatus"" = 'Cancelled',
        ""UpdatedAt"" = NOW() AT TIME ZONE 'UTC'
    WHERE ""SessionStatus"" IN ('ProposedByStartup', 'ProposedByAdvisor')
      AND ""MentorshipID"" IN (
          SELECT DISTINCT ""MentorshipID""
          FROM ""MentorshipSessions""
          WHERE ""SessionStatus"" IN ('Scheduled', 'InProgress', 'Conducted', 'Completed')
      )", conn))
{
    var affected = cmd.ExecuteNonQuery();
    Console.WriteLine($"\n=== Manual fix: {affected} stale session(s) cancelled ===");
}

// 9. Also cancel proposed sessions whose mentorship is Cancelled/Rejected
using (var cmd = new NpgsqlCommand(@"
    UPDATE ""MentorshipSessions""
    SET ""SessionStatus"" = 'Cancelled',
        ""UpdatedAt"" = NOW() AT TIME ZONE 'UTC'
    WHERE ""SessionStatus"" IN ('ProposedByStartup', 'ProposedByAdvisor')
      AND ""MentorshipID"" IN (
          SELECT ""MentorshipID"" FROM ""StartupAdvisorMentorships""
          WHERE ""MentorshipStatus"" IN (1, 7)
      )", conn))
{
    var affected = cmd.ExecuteNonQuery();
    Console.WriteLine($"=== Cancelled/Rejected mentorship cleanup: {affected} session(s) cancelled ===");
}

// 10. Verify
using (var cmd = new NpgsqlCommand("SELECT \"SessionStatus\", COUNT(*) FROM \"MentorshipSessions\" GROUP BY \"SessionStatus\" ORDER BY COUNT(*) DESC", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Final session status distribution ===");
    while (r.Read()) Console.WriteLine($"  {r[0],-20} : {r[1]}");
}

// 7. Check if those mentorships have OTHER sessions
using (var cmd = new NpgsqlCommand(@"
    SELECT s.""MentorshipID"", s.""SessionID"", s.""SessionStatus""
    FROM ""MentorshipSessions"" s
    WHERE s.""MentorshipID"" IN (
        SELECT DISTINCT ""MentorshipID"" FROM ""MentorshipSessions""
        WHERE ""SessionStatus"" IN ('ProposedByStartup','ProposedByAdvisor')
    )
    ORDER BY s.""MentorshipID"", s.""SessionID""", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== All sessions for mentorships with Proposed slots ===");
    Console.WriteLine("  MentorshipID | SessionID | Status");
    while (r.Read()) Console.WriteLine($"  {r[0],-12} | {r[1],-9} | {r[2]}");
}
