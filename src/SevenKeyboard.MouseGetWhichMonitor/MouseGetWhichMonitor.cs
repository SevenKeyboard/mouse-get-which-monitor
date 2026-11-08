//==============================================================
// mouseGetWhichMonitor — Determine the monitor index containing the mouse cursor.
//
// GitHub: https://github.com/SevenKeyboard/mouse-get-which-monitor
// Author: SevenKeyboard Ltd. (2025)
// License: The Unlicense
//==============================================================
using System.Runtime.InteropServices;

namespace SevenKeyboard;

/// <summary>Finds the first monitor containing a point in the shared snapshot.</summary>
public static partial class MouseGetWhichMonitor
{
    private static readonly MouseMonitorQuery Query = new(
        ReadCursor, () => MonitorExGetUtils.GetInfoList());

    /// <summary>
    /// Returns a one-based monitor index, or 0 when no monitor contains the point
    /// or the cursor cannot be read. If either coordinate is null, both come from
    /// the current cursor. Left/top edges are included; right/bottom are excluded.
    /// Supplied coordinates are compared without truncation.
    /// </summary>
    public static int Get(double? x = null, double? y = null) => Query.Get(x, y);

    private static (int X, int Y)? ReadCursor() =>
        GetCursorPos(out Point point) != 0 ? (point.X, point.Y) : null;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        internal int X;
        internal int Y;
    }

    [LibraryImport("user32.dll")]
    private static partial int GetCursorPos(out Point point);
}

// Use the same query with native reads and deterministic test snapshots.
internal sealed class MouseMonitorQuery(
    Func<(int X, int Y)?> readCursor,
    Func<IReadOnlyList<DisplayMonitorInfo>> getSnapshot)
{
    internal int Get(double? x, double? y)
    {
        (double X, double Y) point;
        if (x is double suppliedX && y is double suppliedY)
            point = (suppliedX, suppliedY);
        else if (readCursor() is { } cursor)
            point = cursor;
        else
            return 0;

        IReadOnlyList<DisplayMonitorInfo> snapshot = getSnapshot();
        for (int i = 0; i < snapshot.Count; i++)
        {
            MonitorRectangle rectangle = snapshot[i].Monitor;
            if (rectangle.Left <= point.X && point.X < rectangle.Right &&
                rectangle.Top <= point.Y && point.Y < rectangle.Bottom)
                return i + 1;
        }
        return 0;
    }
}
