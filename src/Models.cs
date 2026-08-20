using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace GakumasSmartLauncher
{
    public sealed class LaunchRecord
    {
        public int SchemaVersion { get; set; }
        public string CapturedAt { get; set; }
        public string ExecutablePath { get; set; }
        public string WorkingDirectory { get; set; }
        public string ViewerId { get; set; }
        public string OnetimeToken { get; set; }
        public string OpenId { get; set; }
        public string PfAccessToken { get; set; }
        public bool RunAsAdministrator { get; set; }
        public string SourceFingerprint { get; set; }

        public LaunchRecord()
        {
            SchemaVersion = 1;
        }

        public DateTimeOffset? GetCapturedAt()
        {
            DateTimeOffset value;
            if (DateTimeOffset.TryParse(
                CapturedAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                out value))
            {
                return value;
            }

            return null;
        }

        public string BuildArguments()
        {
            Validate();
            return string.Format(
                CultureInfo.InvariantCulture,
                "/viewer_id={0} /onetime_token={1} /open_id={2} /pf_access_token={3}",
                ViewerId,
                OnetimeToken,
                OpenId,
                PfAccessToken);
        }

        public void Validate()
        {
            if (SchemaVersion != 1)
            {
                throw new LauncherException("快取版本不受支援，請重新從 DMM 同步。");
            }

            if (string.IsNullOrWhiteSpace(ExecutablePath) ||
                !ExecutablePath.EndsWith("gakumas.exe", StringComparison.OrdinalIgnoreCase))
            {
                throw new LauncherException("啟動資料沒有有效的 gakumas.exe 路徑。");
            }

            ValidateToken(ViewerId, "viewer_id");
            ValidateToken(OnetimeToken, "onetime_token");
            ValidateToken(OpenId, "open_id");
            ValidateToken(PfAccessToken, "pf_access_token");

            if (string.IsNullOrWhiteSpace(SourceFingerprint))
            {
                throw new LauncherException("啟動資料缺少來源指紋，請重新從 DMM 同步。");
            }
        }

        private static void ValidateToken(string value, string name)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 8192)
            {
                throw new LauncherException("啟動資料缺少必要欄位：" + name + "。");
            }

            if (value.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c == '\"' || c == '\''))
            {
                throw new LauncherException("啟動資料格式不安全，請重新從 DMM 同步。");
            }
        }
    }

    public sealed class GameInstallation
    {
        public bool Installed { get; set; }
        public string Version { get; set; }
        public string Path { get; set; }
    }

    public sealed class LauncherSnapshot
    {
        public bool CachePresent { get; set; }
        public bool CacheReadable { get; set; }
        public string CacheCapturedAt { get; set; }
        public bool LatestLogRecordPresent { get; set; }
        public string LatestLogCapturedAt { get; set; }
        public bool GamePathExists { get; set; }
        public string GamePath { get; set; }
        public string GameVersion { get; set; }
        public bool GameRunning { get; set; }
        public bool DmmRunning { get; set; }
        public bool CacheMatchesLatestLog { get; set; }
        public string StatusMessage { get; set; }
    }

    public sealed class SyncResult
    {
        public bool Updated { get; set; }
        public bool WasAlreadyCurrent { get; set; }
        public LaunchRecord Record { get; set; }
        public string Message { get; set; }
    }

    public sealed class StartResult
    {
        public Process Process { get; set; }
        public bool AlreadyRunning { get; set; }
    }

    public sealed class DmmCloseResult
    {
        public int Found { get; set; }
        public int Closed { get; set; }
        public int Remaining { get; set; }
    }

    public sealed class LauncherException : Exception
    {
        public LauncherException(string message)
            : base(message)
        {
        }

        public LauncherException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
