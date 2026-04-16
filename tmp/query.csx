#r "nuget: Npgsql, 8.0.3"
using Npgsql;

var connStr = "Host=localhost;Port=5432;Database=AISEP;Username=postgres;Password=Huydzdz123@";
using var conn = new NpgsqlConnection(connStr);
conn.Open();

// 1. Check if migration was applied
using (var cmd = new NpgsqlCommand("SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%AddConsultingOversight%'", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("=== Migration applied? ===");
    if (r.Read()) Console.WriteLine($"YES: {r[0]}");
    else Console.WriteLine("NO — migration not found");
}

// 2. Count remaining ProposedByStartup/ProposedByAdvisor
using (var cmd = new NpgsqlCommand("SELECT \"SessionStatus\", COUNT(*) FROM \"MentorshipSessions\" WHERE \"SessionStatus\" IN ('ProposedByStartup','ProposedByAdvisor') GROUP BY \"SessionStatus\"", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Remaining Proposed sessions ===");
    bool any = false;
    while (r.Read()) { Console.WriteLine($"{r[0]}: {r[1]}"); any = true; }
    if (!any) Console.WriteLine("NONE — all stale sessions cancelled");
}

// 3. Show sessions that were cancelled by migration (recently updated ProposedBy* → Cancelled)
using (var cmd = new NpgsqlCommand(@"
    SELECT ""SessionID"", ""MentorshipID"", ""SessionStatus"", ""UpdatedAt""
    FROM ""MentorshipSessions""
    WHERE ""SessionStatus"" = 'Cancelled'
      AND ""UpdatedAt"" >= NOW() - INTERVAL '1 hour'
    ORDER BY ""MentorshipID"", ""SessionID""
    LIMIT 20", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== Recently cancelled sessions (last hour) ===");
    Console.WriteLine("SessionID | MentorshipID | Status | UpdatedAt");
    bool any = false;
    while (r.Read()) { Console.WriteLine($"{r[0]} | {r[1]} | {r[2]} | {r[3]}"); any = true; }
    if (!any) Console.WriteLine("(none found — possibly no stale data existed)");
}

// 4. Check new columns exist
using (var cmd = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name = 'MentorshipReports' AND column_name = 'ReportReviewStatus'", conn))
using (var r = cmd.ExecuteReader())
{
    Console.WriteLine("\n=== New column ReportReviewStatus exists? ===");
    Console.WriteLine(r.Read() ? "YES" : "NO");
}
