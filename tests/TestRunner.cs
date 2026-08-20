using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GakumasSmartLauncher;

namespace GakumasSmartLauncherTests
{
    internal static class TestRunner
    {
        private static int _passed;
        private static int _failed;

        private static int Main()
        {
            Run("parses all four launch fields", ParsesAllFourFields);
            Run("rejects records without onetime token", RejectsMissingOnetimeToken);
            Run("chooses the latest valid log record", ChoosesLatestRecord);
            Run("protects cache with current-user DPAPI", ProtectsCache);
            Run("updates an existing cache atomically", UpdatesExistingCache);
            Run("sanitizes every known secret name", SanitizesKnownSecrets);
            Run("builds the complete game argument list", BuildsArguments);
            Run("reports safe status metadata only", ReportsSafeStatus);
            Run("updates only after a new DMM launch record", RequiresNewRecordForUpdate);

            Console.WriteLine("Passed: " + _passed.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("Failed: " + _failed.ToString(CultureInfo.InvariantCulture));
            return _failed == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                _passed++;
                Console.WriteLine("PASS  " + name);
            }
            catch (Exception ex)
            {
                _failed++;
                Console.WriteLine("FAIL  " + name + " — " + ex.GetType().Name);
            }
        }

        private static void ParsesAllFourFields()
        {
            LaunchRecord record;
            Assert(DmmLogParser.TryParseLine(BuildLine("2026-08-20T12:08:41+08:00", "a1", "b2", "c3", "d4"), out record));
            Assert(record != null);
            Assert(record.ExecutablePath == @"C:\Games\gakumas\gakumas.exe");
            Assert(record.WorkingDirectory == @"C:\Games\gakumas");
            Assert(record.ViewerId == "a1");
            Assert(record.OnetimeToken == "b2");
            Assert(record.OpenId == "c3");
            Assert(record.PfAccessToken == "d4");
            Assert(record.RunAsAdministrator);
        }

        private static void RejectsMissingOnetimeToken()
        {
            var line = BuildLine("2026-08-20T12:08:41+08:00", "a1", "b2", "c3", "d4")
                .Replace(" /onetime_token=b2", string.Empty);
            LaunchRecord record;
            Assert(!DmmLogParser.TryParseLine(line, out record));
        }

        private static void ChoosesLatestRecord()
        {
            WithTemporaryDirectory(delegate(string root)
            {
                var log = Path.Combine(root, "dll.log");
                File.WriteAllLines(log, new[]
                {
                    "unrelated line",
                    BuildLine("2026-08-20T11:00:00+08:00", "old", "one-old", "open-old", "pf-old"),
                    BuildLine("2026-08-20T12:00:00+08:00", "new", "one-new", "open-new", "pf-new")
                });
                var latest = DmmLogParser.FindLatest(log);
                Assert(latest != null && latest.ViewerId == "new");
            });
        }

        private static void ProtectsCache()
        {
            WithTemporaryDirectory(delegate(string root)
            {
                var path = Path.Combine(root, "credentials.dat");
                var store = new CacheStore(path);
                var record = CreateRecord("secret-viewer");
                store.Save(record);
                var raw = File.ReadAllBytes(path);
                Assert(!Encoding.UTF8.GetString(raw).Contains("secret-viewer"));
                var loaded = store.Load();
                Assert(loaded.ViewerId == "secret-viewer");
                Assert(loaded.OnetimeToken == record.OnetimeToken);
            });
        }

        private static void UpdatesExistingCache()
        {
            WithTemporaryDirectory(delegate(string root)
            {
                var path = Path.Combine(root, "credentials.dat");
                var store = new CacheStore(path);
                store.Save(CreateRecord("first"));
                store.Save(CreateRecord("second"));
                Assert(store.Load().ViewerId == "second");
                Assert(Directory.GetFiles(root, "*.tmp").Length == 0);
            });
        }

        private static void SanitizesKnownSecrets()
        {
            var source = "/viewer_id=alpha /onetime_token=beta /open_id=gamma /pf_access_token=delta accessToken=epsilon";
            var safe = SafeLogger.Sanitize(source);
            Assert(!safe.Contains("alpha"));
            Assert(!safe.Contains("beta"));
            Assert(!safe.Contains("gamma"));
            Assert(!safe.Contains("delta"));
            Assert(!safe.Contains("epsilon"));
        }

        private static void BuildsArguments()
        {
            var record = CreateRecord("viewer");
            var value = record.BuildArguments();
            Assert(value.Contains("/viewer_id=viewer"));
            Assert(value.Contains("/onetime_token=one"));
            Assert(value.Contains("/open_id=open"));
            Assert(value.Contains("/pf_access_token=pf"));
        }

        private static void ReportsSafeStatus()
        {
            WithTemporaryDirectory(delegate(string root)
            {
                var dmm = Path.Combine(root, "dmm");
                var data = Path.Combine(root, "data");
                Directory.CreateDirectory(Path.Combine(dmm, "logs"));
                var game = Path.Combine(root, "game");
                Directory.CreateDirectory(game);
                File.WriteAllBytes(Path.Combine(game, "gakumas.exe"), new byte[] { 0 });

                var line = BuildLine("2026-08-20T12:00:00+08:00", "private-viewer", "private-one", "private-open", "private-pf")
                    .Replace(@"C:\\Games\\gakumas\\gakumas.exe", EscapePath(Path.Combine(game, "gakumas.exe")))
                    .Replace(@"C:\\Games\\gakumas", EscapePath(game));
                File.WriteAllText(Path.Combine(dmm, "logs", "dll.log"), line);
                File.WriteAllText(
                    Path.Combine(dmm, "dmmgame.cnf"),
                    "{\"contents\":[{\"productId\":\"gakumas\",\"detail\":{\"installed\":true,\"version\":\"3.3.0\",\"path\":\"" + EscapeJson(game) + "\"}}]}");

                var environment = new LauncherEnvironment(dmm, data);
                environment.SyncLatest(null);
                var status = environment.GetStatusJson();
                Assert(status.Contains("3.3.0"));
                Assert(!status.Contains("private-viewer"));
                Assert(!status.Contains("private-one"));
                Assert(!status.Contains("private-open"));
                Assert(!status.Contains("private-pf"));
            });
        }

        private static void RequiresNewRecordForUpdate()
        {
            WithTemporaryDirectory(delegate(string root)
            {
                var dmm = Path.Combine(root, "dmm");
                var data = Path.Combine(root, "data");
                var game = Path.Combine(root, "game");
                Directory.CreateDirectory(Path.Combine(dmm, "logs"));
                Directory.CreateDirectory(game);
                File.WriteAllBytes(Path.Combine(game, "gakumas.exe"), new byte[] { 0 });

                var firstLine = BuildLine("2026-08-20T12:00:00+08:00", "first", "one-first", "open-first", "pf-first")
                    .Replace(@"C:\\Games\\gakumas\\gakumas.exe", EscapePath(Path.Combine(game, "gakumas.exe")))
                    .Replace(@"C:\\Games\\gakumas", EscapePath(game));
                var logPath = Path.Combine(dmm, "logs", "dll.log");
                File.WriteAllText(logPath, firstLine);

                var environment = new LauncherEnvironment(dmm, data);
                environment.SyncLatest(null);
                var firstFingerprint = environment.GetCachedRecord().SourceFingerprint;
                var unchanged = environment.SyncLatest(firstFingerprint);
                Assert(!unchanged.Updated);

                var secondLine = BuildLine("2026-08-20T13:00:00+08:00", "second", "one-second", "open-second", "pf-second")
                    .Replace(@"C:\\Games\\gakumas\\gakumas.exe", EscapePath(Path.Combine(game, "gakumas.exe")))
                    .Replace(@"C:\\Games\\gakumas", EscapePath(game));
                File.AppendAllText(logPath, Environment.NewLine + secondLine);
                var updated = environment.SyncLatest(firstFingerprint);
                Assert(updated.Updated);
                Assert(environment.GetCachedRecord().ViewerId == "second");
            });
        }

        private static LaunchRecord CreateRecord(string viewerId)
        {
            return new LaunchRecord
            {
                SchemaVersion = 1,
                CapturedAt = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                ExecutablePath = @"C:\Games\gakumas\gakumas.exe",
                WorkingDirectory = @"C:\Games\gakumas",
                ViewerId = viewerId,
                OnetimeToken = "one",
                OpenId = "open",
                PfAccessToken = "pf",
                RunAsAdministrator = true,
                SourceFingerprint = Guid.NewGuid().ToString("N")
            };
        }

        private static string BuildLine(string timestamp, string viewer, string one, string open, string pf)
        {
            return "time=\"" + timestamp + "\" level=info msg=\"Execute of:: gakumas exe: C:\\\\Games\\\\gakumas\\\\gakumas.exe dir:C:\\\\Games\\\\gakumas arg:/viewer_id=" +
                viewer + " /onetime_token=" + one + " /open_id=" + open + " /pf_access_token=" + pf + " admin: true\"";
        }

        private static string EscapePath(string path)
        {
            return path.Replace("\\", "\\\\");
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void WithTemporaryDirectory(Action<string> action)
        {
            var root = Path.Combine(Path.GetTempPath(), "GakumasSmartLauncherTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                action(root);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        private static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Assertion failed.");
            }
        }
    }
}
