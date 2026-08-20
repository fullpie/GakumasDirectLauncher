using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace GakumasSmartLauncher
{
    public sealed class LauncherEnvironment
    {
        private readonly CacheStore _cache;
        private readonly SafeLogger _logger;

        public LauncherEnvironment(string dmmRoot, string dataRoot)
        {
            if (string.IsNullOrWhiteSpace(dmmRoot))
            {
                throw new ArgumentException("DMM root is required.", "dmmRoot");
            }

            if (string.IsNullOrWhiteSpace(dataRoot))
            {
                throw new ArgumentException("Data root is required.", "dataRoot");
            }

            DmmRoot = dmmRoot;
            DataRoot = dataRoot;
            DmmLogPath = Path.Combine(dmmRoot, "logs", "dll.log");
            DmmConfigPath = Path.Combine(dmmRoot, "dmmgame.cnf");
            _cache = new CacheStore(Path.Combine(dataRoot, "credentials.dat"));
            _logger = new SafeLogger(Path.Combine(dataRoot, "launcher.log"));
        }

        public string DmmRoot { get; private set; }
        public string DataRoot { get; private set; }
        public string DmmLogPath { get; private set; }
        public string DmmConfigPath { get; private set; }
        public string CachePath { get { return _cache.CachePath; } }

        public static LauncherEnvironment CreateDefault()
        {
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var currentDataRoot = Path.Combine(local, "GakumasDirectLauncher");
            var legacyDataRoot = Path.Combine(local, "GakumasSmartLauncher");
            var dataRoot = File.Exists(Path.Combine(currentDataRoot, "credentials.dat"))
                ? currentDataRoot
                : (File.Exists(Path.Combine(legacyDataRoot, "credentials.dat")) ? legacyDataRoot : currentDataRoot);
            return new LauncherEnvironment(
                Path.Combine(roaming, "dmmgameplayer5"),
                dataRoot);
        }

        public LaunchRecord GetLatestLogRecord()
        {
            return DmmLogParser.FindLatest(DmmLogPath);
        }

        public LaunchRecord GetCachedRecord()
        {
            return _cache.Load();
        }

        public SyncResult SyncLatest(string requiredDifferentFingerprint)
        {
            var latest = GetLatestLogRecord();
            if (latest == null)
            {
                throw new LauncherException("找不到有效的學園偶像大師啟動紀錄。請先從 DMM 啟動遊戲一次。");
            }

            if (!File.Exists(latest.ExecutablePath))
            {
                throw new LauncherException("DMM 紀錄中的 gakumas.exe 不存在；請讓 DMM 檢查安裝內容後再試。");
            }

            if (!string.IsNullOrEmpty(requiredDifferentFingerprint) &&
                string.Equals(requiredDifferentFingerprint, latest.SourceFingerprint, StringComparison.Ordinal))
            {
                return new SyncResult
                {
                    Updated = false,
                    WasAlreadyCurrent = true,
                    Record = latest,
                    Message = "尚未偵測到新的 DMM 啟動紀錄。"
                };
            }

            LaunchRecord current = null;
            if (_cache.Exists)
            {
                try
                {
                    current = _cache.Load();
                }
                catch (LauncherException)
                {
                    current = null;
                }
            }

            if (current != null &&
                string.Equals(current.SourceFingerprint, latest.SourceFingerprint, StringComparison.Ordinal))
            {
                return new SyncResult
                {
                    Updated = false,
                    WasAlreadyCurrent = true,
                    Record = current,
                    Message = "啟動資料已經是最新狀態。"
                };
            }

            _cache.Save(latest);
            _logger.Write("cache_updated", "source=dmm_log; fields=4; executable_present=true");
            return new SyncResult
            {
                Updated = true,
                WasAlreadyCurrent = false,
                Record = latest,
                Message = "已安全同步最新的 DMM 啟動資料。"
            };
        }

        public LaunchRecord EnsureUsableCache()
        {
            LaunchRecord cached = null;
            if (_cache.Exists)
            {
                try
                {
                    cached = _cache.Load();
                }
                catch (LauncherException ex)
                {
                    _logger.Write("cache_unreadable", ex.Message);
                }
            }

            var latest = GetLatestLogRecord();
            if (latest != null &&
                (cached == null || !string.Equals(cached.SourceFingerprint, latest.SourceFingerprint, StringComparison.Ordinal)))
            {
                if (File.Exists(latest.ExecutablePath))
                {
                    _cache.Save(latest);
                    cached = latest;
                    _logger.Write("cache_auto_refreshed", "source=newer_dmm_log; fields=4");
                }
            }

            if (cached == null)
            {
                throw new LauncherException("尚未建立可用的啟動資料。請先從 DMM 啟動遊戲一次。");
            }

            cached.Validate();
            if (!File.Exists(cached.ExecutablePath))
            {
                throw new LauncherException("找不到 gakumas.exe；遊戲可能已移動或需要由 DMM 修復。");
            }

            return cached;
        }

        public StartResult StartGame(bool skipElevation)
        {
            var running = Process.GetProcessesByName("gakumas");
            if (running.Length > 0)
            {
                return new StartResult { AlreadyRunning = true, Process = running[0] };
            }

            var record = EnsureUsableCache();
            var workingDirectory = record.WorkingDirectory;
            if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
            {
                workingDirectory = Path.GetDirectoryName(record.ExecutablePath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = record.ExecutablePath,
                WorkingDirectory = workingDirectory,
                Arguments = record.BuildArguments(),
                UseShellExecute = true
            };

            if (record.RunAsAdministrator && !skipElevation && !IsAdministrator())
            {
                startInfo.Verb = "runas";
            }

            try
            {
                var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new LauncherException("Windows 沒有回傳遊戲程序。");
                }

                _logger.Write("game_started", "elevated_requested=" + (!string.IsNullOrEmpty(startInfo.Verb)).ToString().ToLowerInvariant());
                return new StartResult { AlreadyRunning = false, Process = process };
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223)
                {
                    throw new LauncherException("已取消 Windows 系統管理員權限提示，因此沒有啟動遊戲。", ex);
                }

                throw new LauncherException("Windows 無法啟動遊戲。", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new LauncherException("Windows 無法啟動遊戲。", ex);
            }
        }

        public void OpenDmmRepair()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "dmmgameplayer://play/GCL/gakumas/cl/win",
                    UseShellExecute = true
                });
                _logger.Write("dmm_repair_opened", "protocol=dmmgameplayer; product=gakumas");
            }
            catch (Exception ex)
            {
                if (ex is Win32Exception || ex is InvalidOperationException)
                {
                    throw new LauncherException("無法開啟 DMM Game Player。請確認它仍有安裝。", ex);
                }

                throw;
            }
        }

        public DmmCloseResult CloseDmm()
        {
            var initial = Process.GetProcessesByName("DMMGamePlayer");
            var result = new DmmCloseResult { Found = initial.Length };
            try
            {
                foreach (var process in initial)
                {
                    try
                    {
                        if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                        {
                            process.CloseMainWindow();
                        }
                    }
                    catch
                    {
                        // A background helper may exit while DMM is closing.
                    }
                }

                if (initial.Length > 0)
                {
                    Thread.Sleep(600);
                }

                var remaining = Process.GetProcessesByName("DMMGamePlayer");
                try
                {
                    foreach (var process in remaining)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                                process.WaitForExit(1000);
                            }
                        }
                        catch
                        {
                            // Report any process that Windows did not allow us to close.
                        }
                    }
                }
                finally
                {
                    foreach (var process in remaining)
                    {
                        process.Dispose();
                    }
                }

                var final = Process.GetProcessesByName("DMMGamePlayer");
                try
                {
                    result.Remaining = final.Length;
                }
                finally
                {
                    foreach (var process in final)
                    {
                        process.Dispose();
                    }
                }

                result.Closed = Math.Max(0, result.Found - result.Remaining);
                _logger.Write(
                    "dmm_close_requested",
                    "found=" + result.Found.ToString(CultureInfo.InvariantCulture) +
                    "; closed=" + result.Closed.ToString(CultureInfo.InvariantCulture) +
                    "; remaining=" + result.Remaining.ToString(CultureInfo.InvariantCulture));
                return result;
            }
            finally
            {
                foreach (var process in initial)
                {
                    process.Dispose();
                }
            }
        }

        public LauncherSnapshot GetSnapshot()
        {
            var snapshot = new LauncherSnapshot();
            LaunchRecord cached = null;
            LaunchRecord latest = null;

            snapshot.CachePresent = _cache.Exists;
            if (_cache.Exists)
            {
                try
                {
                    cached = _cache.Load();
                    snapshot.CacheReadable = true;
                    snapshot.CacheCapturedAt = cached.CapturedAt;
                }
                catch (LauncherException ex)
                {
                    snapshot.CacheReadable = false;
                    snapshot.StatusMessage = ex.Message;
                }
            }

            try
            {
                latest = GetLatestLogRecord();
                snapshot.LatestLogRecordPresent = latest != null;
                snapshot.LatestLogCapturedAt = latest == null ? null : latest.CapturedAt;
            }
            catch (LauncherException ex)
            {
                if (string.IsNullOrWhiteSpace(snapshot.StatusMessage))
                {
                    snapshot.StatusMessage = ex.Message;
                }
            }

            var installation = ReadGameInstallation();
            snapshot.GameVersion = installation == null ? null : installation.Version;
            snapshot.GamePath = cached != null
                ? cached.ExecutablePath
                : (latest != null ? latest.ExecutablePath : (installation == null ? null : Path.Combine(installation.Path ?? string.Empty, "gakumas.exe")));
            snapshot.GamePathExists = !string.IsNullOrWhiteSpace(snapshot.GamePath) && File.Exists(snapshot.GamePath);
            snapshot.GameRunning = Process.GetProcessesByName("gakumas").Length > 0;
            snapshot.DmmRunning = Process.GetProcessesByName("DMMGamePlayer").Length > 0;
            snapshot.CacheMatchesLatestLog = cached != null && latest != null &&
                string.Equals(cached.SourceFingerprint, latest.SourceFingerprint, StringComparison.Ordinal);

            if (string.IsNullOrWhiteSpace(snapshot.StatusMessage))
            {
                if (!snapshot.LatestLogRecordPresent && !snapshot.CacheReadable)
                {
                    snapshot.StatusMessage = "尚未找到可用的啟動資料。";
                }
                else if (!snapshot.GamePathExists)
                {
                    snapshot.StatusMessage = "找不到遊戲執行檔。";
                }
                else if (snapshot.CacheReadable)
                {
                    snapshot.StatusMessage = "已準備好直接啟動。";
                }
                else
                {
                    snapshot.StatusMessage = "可以從最新 DMM 紀錄建立安全快取。";
                }
            }

            return snapshot;
        }

        public string GetStatusJson()
        {
            var snapshot = GetSnapshot();
            var safe = new Dictionary<string, object>
            {
                { "cachePresent", snapshot.CachePresent },
                { "cacheReadable", snapshot.CacheReadable },
                { "latestLogRecordPresent", snapshot.LatestLogRecordPresent },
                { "gamePathExists", snapshot.GamePathExists },
                { "gameVersion", snapshot.GameVersion ?? string.Empty },
                { "gameRunning", snapshot.GameRunning },
                { "dmmRunning", snapshot.DmmRunning },
                { "cacheMatchesLatestLog", snapshot.CacheMatchesLatestLog },
                { "status", snapshot.StatusMessage ?? string.Empty }
            };
            return new JavaScriptSerializer().Serialize(safe);
        }

        public void Log(string eventName, string detail)
        {
            _logger.Write(eventName, detail);
        }

        private GameInstallation ReadGameInstallation()
        {
            if (!File.Exists(DmmConfigPath))
            {
                return null;
            }

            try
            {
                string json;
                using (var stream = new FileStream(
                    DmmConfigPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    json = reader.ReadToEnd();
                }

                var config = new JavaScriptSerializer().Deserialize<DmmConfig>(json);
                if (config == null || config.contents == null)
                {
                    return null;
                }

                var item = config.contents.FirstOrDefault(value =>
                    value != null && string.Equals(value.productId, "gakumas", StringComparison.OrdinalIgnoreCase));
                if (item == null || item.detail == null)
                {
                    return null;
                }

                return new GameInstallation
                {
                    Installed = item.detail.installed,
                    Version = item.detail.version,
                    Path = item.detail.path
                };
            }
            catch
            {
                return null;
            }
        }

        private static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private sealed class DmmConfig
        {
            public List<DmmContent> contents { get; set; }
        }

        private sealed class DmmContent
        {
            public string productId { get; set; }
            public DmmDetail detail { get; set; }
        }

        private sealed class DmmDetail
        {
            public bool installed { get; set; }
            public string version { get; set; }
            public string path { get; set; }
        }
    }
}
