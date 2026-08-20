using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace GakumasSmartLauncher
{
    public sealed class SafeLogger
    {
        private static readonly Regex NamedSecretRegex = new Regex(
            @"(?i)(viewer_id|onetime_token|open_id|pf_access_token|accessToken)(\s*[:=]\s*|/)([^\s,;]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly string _path;

        public SafeLogger(string path)
        {
            _path = path;
        }

        public void Write(string eventName, string detail)
        {
            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var safeEvent = Sanitize(eventName ?? "event").Replace("\r", " ").Replace("\n", " ");
                var safeDetail = Sanitize(detail ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
                var line = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:o}\t{1}\t{2}{3}",
                    DateTimeOffset.UtcNow,
                    safeEvent,
                    safeDetail,
                    Environment.NewLine);
                File.AppendAllText(_path, line);
            }
            catch
            {
                // Logging must never prevent the game from launching.
            }
        }

        public static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return NamedSecretRegex.Replace(value, delegate(Match match)
            {
                return match.Groups[1].Value + "=<redacted>";
            });
        }
    }
}
