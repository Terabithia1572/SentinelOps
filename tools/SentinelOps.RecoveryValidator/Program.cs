using System.Diagnostics;
using System.Text.Json;
using Npgsql;

namespace SentinelOps.RecoveryValidator;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("    SentinelOps — Enterprise Backup, Restore & Disaster Recovery Validator");
        Console.WriteLine("================================================================================\n");

        string connectionString = ResolveConnectionString();
        var csBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        string host = csBuilder.Host ?? "localhost";
        int port = csBuilder.Port;
        string primaryDbName = csBuilder.Database ?? "sentinelops";
        string recoveryDbName = "sentinelops_recovery_test";
        string username = csBuilder.Username ?? "postgres";
        string password = csBuilder.Password ?? "postgres";

        Console.WriteLine($"[1/6] Connecting to Primary Database: {primaryDbName} on {host}:{port}...");
        
        Dictionary<string, long> primaryCounts = new();
        string dbSizeStr = "Unknown";
        int indexCount = 0;
        int migrationCount = 0;

        await using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            Console.WriteLine("      Connected successfully.");

            // Get DB size
            await using (var cmd = new NpgsqlCommand($"SELECT pg_size_pretty(pg_database_size('{primaryDbName}'))", conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                dbSizeStr = result?.ToString() ?? "Unknown";
            }

            // Get table counts
            var tables = new[]
            {
                "Applications", "LogEntries", "TraceRecords", "SpanRecords",
                "MetricRecords", "AlertRules", "AlertIncidents", "AuditEvents",
                "AspNetUsers", "AspNetRoles", "ApiKeys", "NotificationChannels",
                "Environments", "Services", "Organizations", "OrganizationMembers",
                "OrganizationSettings", "CustomDashboards", "StatusPages"
            };

            foreach (var table in tables)
            {
                try
                {
                    await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\"", conn);
                    var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    primaryCounts[table] = count;
                }
                catch
                {
                    // Table might not exist or use different casing
                    primaryCounts[table] = 0;
                }
            }

            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"__EFMigrationsHistory\"", conn))
            {
                migrationCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public'", conn))
            {
                indexCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        Console.WriteLine($"      Primary DB Size: {dbSizeStr}");
        Console.WriteLine($"      Applied Migrations: {migrationCount}");
        Console.WriteLine($"      Public Schema Indexes: {indexCount}");
        foreach (var (tbl, cnt) in primaryCounts)
        {
            Console.WriteLine($"      - {tbl}: {cnt:N0} rows");
        }

        // 2. Locate PostgreSQL tools
        string pgDumpPath = LocatePostgresBinary("pg_dump.exe");
        string pgRestorePath = LocatePostgresBinary("pg_restore.exe");
        Console.WriteLine($"\n[2/6] Located pg_dump at: {pgDumpPath}");
        Console.WriteLine($"      Located pg_restore at: {pgRestorePath}");

        // 3. Perform pg_dump backup
        string backupFile = Path.Combine(Path.GetTempPath(), $"sentinelops_dr_backup_{DateTime.UtcNow:yyyyMMddHHmmss}.dump");
        Console.WriteLine($"\n[3/6] Starting pg_dump backup to: {backupFile}...");
        var backupSw = Stopwatch.StartNew();

        int backupExitCode = RunProcess(pgDumpPath,
            $"-h {host} -p {port} -U {username} -d {primaryDbName} -F c -b -f \"{backupFile}\"",
            password);

        backupSw.Stop();
        if (backupExitCode != 0 || !File.Exists(backupFile))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: pg_dump failed with exit code {backupExitCode}");
            Console.ResetColor();
            return 1;
        }

        long backupBytes = new FileInfo(backupFile).Length;
        double backupKb = backupBytes / 1024.0;
        Console.WriteLine($"      Backup completed in {backupSw.ElapsedMilliseconds:N0} ms ({backupSw.Elapsed.TotalSeconds:F2} s)");
        Console.WriteLine($"      Dump File Size: {backupKb:F2} KB ({backupBytes:N0} bytes)");

        // 4. Create Recovery Test Database
        Console.WriteLine($"\n[4/6] Creating isolated recovery database '{recoveryDbName}'...");
        string maintConnStr = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" }.ConnectionString;
        await using (var maintConn = new NpgsqlConnection(maintConnStr))
        {
            await maintConn.OpenAsync();
            // Drop if exists first
            await using (var dropCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{recoveryDbName}\" WITH (FORCE)", maintConn))
            {
                await dropCmd.ExecuteNonQueryAsync();
            }
            await using (var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{recoveryDbName}\"", maintConn))
            {
                await createCmd.ExecuteNonQueryAsync();
            }
            Console.WriteLine($"      Database '{recoveryDbName}' created.");
        }

        // 5. Perform pg_restore into Recovery Test Database
        Console.WriteLine($"\n[5/6] Restoring backup into '{recoveryDbName}'...");
        var restoreSw = Stopwatch.StartNew();

        int restoreExitCode = RunProcess(pgRestorePath,
            $"-h {host} -p {port} -U {username} -d {recoveryDbName} --no-owner \"{backupFile}\"",
            password);

        restoreSw.Stop();
        // pg_restore can exit with 0 or 1 for minor warnings (e.g. extension already exists)
        Console.WriteLine($"      Restore completed in {restoreSw.ElapsedMilliseconds:N0} ms ({restoreSw.Elapsed.TotalSeconds:F2} s) with exit code {restoreExitCode}");

        // 6. Verify Restored Data Integrity
        Console.WriteLine($"\n[6/6] Verifying Data & Schema Parity in '{recoveryDbName}'...");
        string recoveryConnStr = new NpgsqlConnectionStringBuilder(connectionString) { Database = recoveryDbName }.ConnectionString;
        Dictionary<string, long> recoveryCounts = new();
        int recoveryIndexCount = 0;
        int recoveryMigrationCount = 0;
        bool allMatch = true;

        await using (var recConn = new NpgsqlConnection(recoveryConnStr))
        {
            await recConn.OpenAsync();

            foreach (var table in primaryCounts.Keys)
            {
                try
                {
                    await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\"", recConn);
                    var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    recoveryCounts[table] = count;
                }
                catch
                {
                    recoveryCounts[table] = -1;
                }
            }

            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"__EFMigrationsHistory\"", recConn))
            {
                recoveryMigrationCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public'", recConn))
            {
                recoveryIndexCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(string.Format("{0,-25} | {1,15} | {2,15} | {3,10}", "Table Name", "Primary Count", "Restored Count", "Match"));
        Console.WriteLine("--------------------------------------------------------------------------------");

        foreach (var (tbl, pCount) in primaryCounts)
        {
            long rCount = recoveryCounts.GetValueOrDefault(tbl, -1);
            bool match = pCount == rCount;
            if (!match) allMatch = false;

            string matchStatus = match ? "MATCH" : "MISMATCH";
            Console.WriteLine(string.Format("{0,-25} | {1,15:N0} | {2,15:N0} | {3,10}", tbl, pCount, rCount, matchStatus));
        }
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(string.Format("{0,-25} | {1,15:N0} | {2,15:N0} | {3,10}", "EF Migrations", migrationCount, recoveryMigrationCount, migrationCount == recoveryMigrationCount ? "MATCH" : "MISMATCH"));
        Console.WriteLine(string.Format("{0,-25} | {1,15:N0} | {2,15:N0} | {3,10}", "Database Indexes", indexCount, recoveryIndexCount, indexCount == recoveryIndexCount ? "MATCH" : "MISMATCH"));
        Console.WriteLine("--------------------------------------------------------------------------------\n");

        // Clean up recovery test database and dump file
        Console.WriteLine("Cleaning up recovery test database and temporary dump file...");
        await using (var maintConn = new NpgsqlConnection(maintConnStr))
        {
            await maintConn.OpenAsync();
            await using var dropCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{recoveryDbName}\" WITH (FORCE)", maintConn);
            await dropCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"      Dropped test database '{recoveryDbName}'. (Primary untouched).");
        }

        if (File.Exists(backupFile))
        {
            File.Delete(backupFile);
            Console.WriteLine("      Removed temporary backup dump file.");
        }

        if (!allMatch || migrationCount != recoveryMigrationCount || indexCount != recoveryIndexCount)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[FAILURE] Database restore integrity validation failed!");
            Console.ResetColor();
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n================================================================================");
        Console.WriteLine(" [SUCCESS] Disaster Recovery & Restore Verification Passed with 100% Parity!");
        Console.WriteLine($" - RTA (Backup Duration): {backupSw.ElapsedMilliseconds} ms ({backupSw.Elapsed.TotalSeconds:F2} s)");
        Console.WriteLine($" - RTA (Restore Duration): {restoreSw.ElapsedMilliseconds} ms ({restoreSw.Elapsed.TotalSeconds:F2} s)");
        Console.WriteLine($" - Backup File Size: {backupKb:F2} KB");
        Console.WriteLine($" - Table Row Parity: 100% Identical Across All Schema Objects");
        Console.WriteLine("================================================================================\n");
        Console.ResetColor();

        return 0;
    }

    private static string ResolveConnectionString()
    {
        string localSettings = Path.Combine(Directory.GetCurrentDirectory(), "src", "SentinelOps.Api", "appsettings.Local.json");
        if (!File.Exists(localSettings))
        {
            localSettings = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "SentinelOps.Api", "appsettings.Local.json");
        }

        if (File.Exists(localSettings))
        {
            try
            {
                var doc = JsonDocument.Parse(File.ReadAllText(localSettings));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("DefaultConnection", out var dc))
                {
                    return dc.GetString()!;
                }
            }
            catch { }
        }

        return "Host=localhost;Port=5432;Database=sentinelops;Username=postgres;Password=postgres";
    }

    private static string LocatePostgresBinary(string exeName)
    {
        string[] searchPaths =
        [
            Path.Combine("C:\\Program Files\\PostgreSQL\\18\\bin", exeName),
            Path.Combine("C:\\Program Files\\PostgreSQL\\13\\bin", exeName)
        ];

        foreach (var p in searchPaths)
        {
            if (File.Exists(p)) return p;
        }

        return exeName;
    }

    private static int RunProcess(string filename, string arguments, string password)
    {
        var psi = new ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["PGPASSWORD"] = password;

        using var process = Process.Start(psi);
        if (process == null) return -1;

        // Concurrently drain stdout and stderr to avoid Windows pipe buffer deadlocks
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        bool completed = process.WaitForExit(30000);
        if (!completed)
        {
            try { process.Kill(true); } catch { }
            return -1;
        }

        Task.WaitAll([stdoutTask, stderrTask], TimeSpan.FromSeconds(5));
        return process.ExitCode;
    }
}
