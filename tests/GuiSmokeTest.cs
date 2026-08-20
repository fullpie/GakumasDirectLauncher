using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using GakumasSmartLauncher;

namespace GakumasSmartLauncherGuiSmokeTest
{
    internal static class GuiSmokeTest
    {
        [STAThread]
        private static int Main(string[] args)
        {
            var outputPath = args.Length > 0
                ? args[0]
                : Path.Combine(Environment.CurrentDirectory, "gui-smoke.png");
            var mode = args.Length > 1 ? args[1].ToLowerInvariant() : "default";
            if (mode.StartsWith("en", StringComparison.Ordinal))
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }
            else if (mode.StartsWith("zh-cn", StringComparison.Ordinal))
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
            }
            string temporaryRoot = null;
            LauncherEnvironment environment;
            if (mode == "empty")
            {
                temporaryRoot = Path.Combine(Path.GetTempPath(), "GakumasSmartLauncherGuiTest", Guid.NewGuid().ToString("N"));
                var dmmRoot = Path.Combine(temporaryRoot, "dmm");
                var dataRoot = Path.Combine(temporaryRoot, "data");
                Directory.CreateDirectory(Path.Combine(dmmRoot, "logs"));
                environment = new LauncherEnvironment(dmmRoot, dataRoot);
            }
            else
            {
                environment = LauncherEnvironment.CreateDefault();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                using (var form = new MainForm(environment, false, true))
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-30000, -30000);
                    form.Show();
                    Application.DoEvents();
                    if (mode.EndsWith("error", StringComparison.Ordinal))
                    {
                        var method = typeof(MainForm).GetMethod(
                            "BeginRecovery",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (method == null)
                        {
                            throw new InvalidOperationException("The recovery-state method was not found.");
                        }

                        method.Invoke(form, null);
                        Application.DoEvents();
                    }
                    else if (mode.EndsWith("click-update", StringComparison.Ordinal))
                    {
                        var updateButton = FindButton(form, "更新啟動資料");
                        if (updateButton == null)
                        {
                            throw new InvalidOperationException("The update button was not found.");
                        }

                        updateButton.PerformClick();
                        Application.DoEvents();
                    }

                    if (mode.EndsWith("public", StringComparison.Ordinal))
                    {
                        var text = UiStrings.Detect();
                        SetLabelField(form, "_statusValue", text.Ready);
                        SetLabelField(form, "_messageLabel", text.ReadyMessage);
                        SetLabelField(form, "_versionValue", "v3.x");
                        SetLabelField(form, "_cacheValue", "05/16 12:00");
                        SetLabelField(form, "_dmmValue", text.DmmClosed);
                        SetLabelField(form, "_pathValue", @"C:\Games\Gakumas\gakumas.exe");
                        Application.DoEvents();
                    }

                    form.PerformLayout();
                    Application.DoEvents();

                    var textControls = new List<Control>();
                    CollectTextControls(form, textControls);
                    var overlaps = CountOverlaps(form, textControls);
                    var clippedButtons = textControls
                        .OfType<Button>()
                        .Count(button => TextRenderer.MeasureText(button.Text, button.Font).Width > Math.Max(1, button.ClientSize.Width - 22));
                    var languageCheck = mode.StartsWith("en", StringComparison.Ordinal)
                        ? textControls.Any(control => control.Text == "START GAME  ★")
                        : (mode.StartsWith("zh-cn", StringComparison.Ordinal)
                            ? textControls.Any(control => control.Text == "學園偶像大師 Direct") &&
                              !textControls.Any(control => control.Text.Contains("学园"))
                            : true);

                    using (var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.ClientSize));
                        bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    Console.WriteLine("mode=" + mode);
                    Console.WriteLine("deviceDpi=" + form.DeviceDpi);
                    Console.WriteLine("clientSize=" + form.ClientSize.Width + "x" + form.ClientSize.Height);
                    Console.WriteLine("visibleTextControls=" + textControls.Count);
                    Console.WriteLine("textOverlaps=" + overlaps);
                    Console.WriteLine("clippedButtons=" + clippedButtons);
                    Console.WriteLine("languageCheck=" + languageCheck.ToString().ToLowerInvariant());
                    Console.WriteLine("screenshot=" + outputPath);
                    form.Close();
                    return overlaps == 0 && clippedButtons == 0 && languageCheck ? 0 : 1;
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(temporaryRoot) && Directory.Exists(temporaryRoot))
                {
                    Directory.Delete(temporaryRoot, true);
                }
            }
        }

        private static void CollectTextControls(Control parent, ICollection<Control> result)
        {
            foreach (Control child in parent.Controls)
            {
                if (!child.Visible)
                {
                    continue;
                }

                if ((child is Label || child is Button) && !string.IsNullOrWhiteSpace(child.Text))
                {
                    result.Add(child);
                }

                CollectTextControls(child, result);
            }
        }

        private static Button FindButton(Control parent, string text)
        {
            foreach (Control child in parent.Controls)
            {
                var button = child as Button;
                if (button != null && string.Equals(button.Text, text, StringComparison.Ordinal))
                {
                    return button;
                }

                var nested = FindButton(child, text);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void SetLabelField(MainForm form, string fieldName, string value)
        {
            var field = typeof(MainForm).GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var label = field == null ? null : field.GetValue(form) as Label;
            if (label == null)
            {
                throw new InvalidOperationException("The GUI field was not found: " + fieldName);
            }

            label.Text = value;
        }

        private static int CountOverlaps(Form form, IList<Control> controls)
        {
            var rectangles = controls
                .Select(control => new
                {
                    Control = control,
                    Bounds = new Rectangle(form.PointToClient(control.PointToScreen(Point.Empty)), control.Size)
                })
                .ToList();
            var overlaps = 0;
            for (var left = 0; left < rectangles.Count; left++)
            {
                for (var right = left + 1; right < rectangles.Count; right++)
                {
                    if (rectangles[left].Bounds.IntersectsWith(rectangles[right].Bounds))
                    {
                        overlaps++;
                    }
                }
            }

            return overlaps;
        }
    }
}
