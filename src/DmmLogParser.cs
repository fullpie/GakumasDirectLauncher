using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GakumasSmartLauncher
{
    public static class DmmLogParser
    {
        private static readonly Regex RecordRegex = new Regex(
            @"time=""(?<time>[^""]+)"".*?Execute of::\s*gakumas\s+exe:\s*(?<exe>.+?\.exe)\s+dir:(?<dir>.*?)\s+arg:(?<args>.*?)\s+admin:\s*(?<admin>true|false)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex ArgumentRegex = new Regex(
            @"/(?<name>viewer_id|onetime_token|open_id|pf_access_token)=(?<value>[^\s""]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static LaunchRecord FindLatest(string logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            {
                return null;
            }

            LaunchRecord latest = null;
            try
            {
                using (var stream = new FileStream(
                    logPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        LaunchRecord record;
                        if (TryParseLine(line, out record))
                        {
                            latest = record;
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                throw new LauncherException("目前無法讀取 DMM 啟動紀錄，請稍後再試。", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new LauncherException("沒有權限讀取 DMM 啟動紀錄。", ex);
            }

            return latest;
        }

        public static bool TryParseLine(string line, out LaunchRecord record)
        {
            record = null;
            if (string.IsNullOrWhiteSpace(line) ||
                line.IndexOf("Execute of::", StringComparison.OrdinalIgnoreCase) < 0 ||
                line.IndexOf("gakumas", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var match = RecordRegex.Match(line);
            if (!match.Success)
            {
                return false;
            }

            var arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match argumentMatch in ArgumentRegex.Matches(match.Groups["args"].Value))
            {
                arguments[argumentMatch.Groups["name"].Value] = argumentMatch.Groups["value"].Value.Trim();
            }

            string viewerId;
            string onetimeToken;
            string openId;
            string pfAccessToken;
            if (!arguments.TryGetValue("viewer_id", out viewerId) ||
                !arguments.TryGetValue("onetime_token", out onetimeToken) ||
                !arguments.TryGetValue("open_id", out openId) ||
                !arguments.TryGetValue("pf_access_token", out pfAccessToken))
            {
                return false;
            }

            DateTimeOffset capturedAt;
            if (!DateTimeOffset.TryParse(
                match.Groups["time"].Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                out capturedAt))
            {
                return false;
            }

            var parsed = new LaunchRecord
            {
                SchemaVersion = 1,
                CapturedAt = capturedAt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                ExecutablePath = UnescapeLogPath(match.Groups["exe"].Value.Trim()),
                WorkingDirectory = UnescapeLogPath(match.Groups["dir"].Value.Trim()),
                ViewerId = viewerId,
                OnetimeToken = onetimeToken,
                OpenId = openId,
                PfAccessToken = pfAccessToken,
                RunAsAdministrator = string.Equals(match.Groups["admin"].Value, "true", StringComparison.OrdinalIgnoreCase),
                SourceFingerprint = ComputeFingerprint(line)
            };

            try
            {
                parsed.Validate();
            }
            catch (LauncherException)
            {
                return false;
            }

            record = parsed;
            return true;
        }

        private static string UnescapeLogPath(string value)
        {
            return value.Replace("\\\\", "\\").Replace("\\\"", "\"");
        }

        private static string ComputeFingerprint(string line)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(line));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                {
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }
    }
}
