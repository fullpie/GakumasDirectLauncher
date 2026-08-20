using System;
using GakumasSmartLauncher;

internal static class CloseDmmHarness
{
    private static int Main()
    {
        var result = LauncherEnvironment.CreateDefault().CloseDmm();
        Console.WriteLine("found=" + result.Found);
        Console.WriteLine("closed=" + result.Closed);
        Console.WriteLine("remaining=" + result.Remaining);
        return result.Found > 0 && result.Remaining == 0 ? 0 : 1;
    }
}
