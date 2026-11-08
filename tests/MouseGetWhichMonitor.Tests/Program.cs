using System.Runtime.InteropServices;
using SevenKeyboard;

internal static partial class Program
{
    private static int _checks;

    private static void Main()
    {
        BoundariesAndSnapshotOrder();
        MissingCoordinatesAndReadFailure();
        NativeReadOnly();
        Console.WriteLine($"PASS: {_checks} checks ({RuntimeInformation.ProcessArchitecture}; deterministic and read-only native).");
    }

    private static DisplayMonitorInfo Info(int handle, MonitorRectangle monitor, uint flags = 0) =>
        new(handle, flags, $"monitor-{handle}", monitor, new(0, 0, 0, 0));

    private static void BoundariesAndSnapshotOrder()
    {
        IReadOnlyList<DisplayMonitorInfo> snapshot = new[]
        {
            Info(70, new(-200, -100, 0, 100)),
            Info(20, new(-50, -50, 100, 100), flags: 1),
            Info(90, new(100, -100, 200, 0))
        };
        int snapshotReads = 0;
        var query = new MouseMonitorQuery(
            () => throw new InvalidOperationException("Explicit coordinates must not read the cursor."),
            () => { snapshotReads++; return snapshot; });

        var cases = new (double X, double Y, int Expected)[]
        {
            (-200, -100, 1), // Inclusive left/top, outside the work area.
            (-1, 99, 1),
            (-201, 0, 0),
            (-100, -101, 0),
            (-100, 100, 0), // Exclusive bottom.
            (0, -100, 0), // Exclusive right, in a gap.
            (-25, 0, 1), // Overlap: enumeration wins over primary flag or handle.
            (0, 0, 2), // Zero is an explicit coordinate; adjacent left edge wins.
            (99, 99, 2),
            (100, 0, 0),
            (0, 100, 0),
            (100, -100, 3),
            (199, -1, 3),
            (200, -1, 0),
            (100, -101, 0),
            (-200.25, 0, 0), // Truncation would incorrectly include the negative left edge.
            (-100, -100.25, 0), // Fractional negative top stays outside.
            (-0.25, 50, 1) // Fractional negative right neighbor stays on the first monitor.
        };
        foreach (var test in cases)
            Check(query.Get(test.X, test.Y) == test.Expected, $"boundary ({test.X}, {test.Y}) -> {test.Expected}");
        Check(snapshotReads == cases.Length, "each lookup obtains one snapshot");

        snapshot = new[] { snapshot[1], snapshot[0] };
        Check(query.Get(-100, 0) == 2, "a later call uses the current snapshot and its one-based order");
        Check(query.Get(-25, 0) == 1, "overlap follows changed enumeration order");

        snapshot = Array.Empty<DisplayMonitorInfo>();
        Check(query.Get(0, 0) == 0, "empty snapshot");

        snapshot = new[] { Info(1, new(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue)) };
        Check(query.Get(int.MinValue, int.MinValue) == 1, "signed minimum coordinates are included");
        Check(query.Get(int.MaxValue - 1, int.MaxValue - 1) == 1, "large coordinates do not overflow");
        Check(query.Get(int.MaxValue, 0) == 0 && query.Get(0, int.MaxValue) == 0, "maximum right/bottom edges are excluded");
    }

    private static void MissingCoordinatesAndReadFailure()
    {
        var calls = new List<string>();
        (int X, int Y)? cursor = (-100, 20);
        IReadOnlyList<DisplayMonitorInfo> snapshot = new[] { Info(1, new(-200, -100, 0, 100)) };
        var query = new MouseMonitorQuery(
            () => { calls.Add("cursor"); return cursor; },
            () => { calls.Add("snapshot"); return snapshot; });

        var inputs = new (double? X, double? Y)[] { (null, null), (100.5, null), (null, -500.5) };
        foreach (var input in inputs)
        {
            calls.Clear();
            Check(query.Get(input.X, input.Y) == 1, "either missing coordinate replaces both with the cursor");
            Check(calls.SequenceEqual(new[] { "cursor", "snapshot" }), "cursor is read once before the snapshot");
        }

        cursor = null;
        foreach (var input in inputs)
        {
            calls.Clear();
            Check(query.Get(input.X, input.Y) == 0, "failed cursor read returns zero");
            Check(calls.SequenceEqual(new[] { "cursor" }), "failed cursor read skips the snapshot");
        }
        calls.Clear();
        Check(query.Get(-100, 20) == 1 && calls.SequenceEqual(new[] { "snapshot" }),
            "explicit coordinates still work when the cursor cannot be read");

        cursor = (500, 500);
        Check(query.Get(null, null) == 0, "cursor outside all monitors");
    }

    private static void NativeReadOnly()
    {
        nint dpiBefore = GetThreadDpiAwarenessContext();
        IReadOnlyList<DisplayMonitorInfo> snapshot = MonitorExGetUtils.GetInfoList();
        if (snapshot.Count == 0)
            Check(MouseGetWhichMonitor.Get(0, 0) == 0, "public explicit lookup with an empty native snapshot");
        else
        {
            MonitorRectangle first = snapshot[0].Monitor;
            Check(first.Left < first.Right && first.Top < first.Bottom, "first native monitor has a nonempty rectangle");
            Check(MouseGetWhichMonitor.Get(first.Left, first.Top) == 1, "public native first-monitor left/top");
            Check(MouseGetWhichMonitor.Get(first.Right - 1, first.Bottom - 1) == 1, "public native first-monitor last pixel");
        }

        int beforeResult = GetCursorPos(out Point before);
        int current = MouseGetWhichMonitor.Get();
        int missingY = MouseGetWhichMonitor.Get(int.MaxValue, null);
        int missingX = MouseGetWhichMonitor.Get(null, int.MaxValue);
        int afterResult = GetCursorPos(out Point after);
        bool stable = beforeResult != 0 && afterResult != 0 && before.X == after.X && before.Y == after.Y &&
            ReferenceEquals(snapshot, MonitorExGetUtils.GetInfoList());
        if (stable)
        {
            int explicitPoint = MouseGetWhichMonitor.Get(after.X, after.Y);
            Check(current == explicitPoint && missingY == explicitPoint && missingX == explicitPoint,
                "public native cursor paths agree with the sampled explicit point");
            Check(current >= 0 && current <= snapshot.Count, "native result is zero or a snapshot index");
        }
        else
            Console.WriteLine("SKIP: live cursor parity sample moved, was unreadable, or the snapshot changed.");

        Check(GetThreadDpiAwarenessContext() == dpiBefore, "caller DPI context is unchanged");
        Console.WriteLine($"Read-only native: {snapshot.Count} monitor(s), cursor parity sampled={stable}, DPI context=0x{dpiBefore:X}.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        internal int X;
        internal int Y;
    }

    [LibraryImport("user32.dll")]
    private static partial int GetCursorPos(out Point point);

    [LibraryImport("user32.dll")]
    private static partial nint GetThreadDpiAwarenessContext();

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}
