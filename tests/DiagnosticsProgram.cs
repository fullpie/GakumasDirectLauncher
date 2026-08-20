using System;
using System.IO;
using System.Text;
using GakumasSmartLauncher;

namespace GakumasSmartLauncherDiagnostics
{
    internal static class DiagnosticsProgram
    {
        private static int Main(string[] args)
        {
            try
            {
                var environment = LauncherEnvironment.CreateDefault();
                var command = args.Length == 0 ? "status" : args[0].ToLowerInvariant();

                if (command == "sync")
                {
                    var result = environment.SyncLatest(null);
                    Console.WriteLine("syncUpdated=" + result.Updated.ToString().ToLowerInvariant());
                }
                else if (command == "verify-cache")
                {
                    var record = environment.GetCachedRecord();
                    if (record == null || !File.Exists(environment.CachePath))
                    {
                        Console.WriteLine("cachePresent=false");
                        return 2;
                    }

                    var rawText = Encoding.UTF8.GetString(File.ReadAllBytes(environment.CachePath));
                    var plaintextPresent = rawText.Contains(record.ViewerId) ||
                        rawText.Contains(record.OnetimeToken) ||
                        rawText.Contains(record.OpenId) ||
                        rawText.Contains(record.PfAccessToken);
                    Console.WriteLine("cachePresent=true");
                    Console.WriteLine("plaintextCredentialPresent=" + plaintextPresent.ToString().ToLowerInvariant());
                }
                else if (command == "launch-no-elevation")
                {
                    var result = environment.StartGame(true);
                    Console.WriteLine("alreadyRunning=" + result.AlreadyRunning.ToString().ToLowerInvariant());
                    Console.WriteLine("processStarted=" + (result.Process != null).ToString().ToLowerInvariant());
                }
                else if (command != "status")
                {
                    Console.Error.WriteLine("Unknown command.");
                    return 3;
                }

                Console.WriteLine(environment.GetStatusJson());
                return 0;
            }
            catch (LauncherException ex)
            {
                Console.Error.WriteLine(SafeLogger.Sanitize(ex.Message));
                return 1;
            }
        }
    }
}
