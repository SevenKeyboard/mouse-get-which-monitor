# MouseGetWhichMonitor

C# port, version **1.0.1**, namespace `SevenKeyboard`, targeting `net10.0-windows`.

```csharp
using SevenKeyboard;

int currentMonitor = MouseGetWhichMonitor.Get();
int pointMonitor = MouseGetWhichMonitor.Get(-100.5, 200);
int currentAgain = MouseGetWhichMonitor.Get(x: 100); // Missing y: read both cursor coordinates.
```

`public static int Get(double? x = null, double? y = null)` returns the first
matching monitor's **one-based snapshot index**, or **0** when no monitor
contains the point or `GetCursorPos` fails. If either coordinate is null, both
are read from the current cursor. Explicit zero, negative, and fractional
coordinates are compared directly, without integer truncation.

Containment is `left <= x < right` and `top <= y < bottom`, using monitor bounds
rather than work areas. Overlaps use snapshot enumeration order. Each lookup
uses the shared `MonitorExGetUtils.GetInfoList()` snapshot without forcing a
refresh. The port does not change the caller's DPI context or use
`MonitorFromPoint`. Coordinates must use the snapshot's coordinate space.

There is no application UI dependency. The external monitor manager maintains
its own hidden message window and cache. No mouse input or settings are changed.
Version 1.0.1 is preserved: nullable typed inputs retain the source contract.

## Dependencies

The project references the external sibling `monitor-ex-get-utils/main-cs`
project and imports `library-build/main-cs/CheckLibraryVersions.targets`.
The build requires MonitorExGetUtils **2.x**, minimum **2.0.0**.
No dependency source is included.

For another checkout layout, set MSBuild properties
`MonitorExGetUtilsProjectPath` and `LibraryBuildTargetsPath`; the dependency
also accepts `DisplayMonitorInfoManagerProjectPath`.

See [build and test instructions](tests/README.md).

Source: `mouseGetWhichMonitor.ahk` on the original AHK1.1 branch.
Author: SevenKeyboard Ltd. (2025). License: The Unlicense; see `LICENSE`.