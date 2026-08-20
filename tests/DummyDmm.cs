using System;
using System.Drawing;
using System.Windows.Forms;

internal static class DummyDmm
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form
        {
            Text = "DMM close-flow test",
            ClientSize = new Size(320, 120),
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-30000, -30000)
        });
    }
}
