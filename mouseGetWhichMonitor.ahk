#Requires AutoHotkey v1.1.36+
#Include %A_ScriptDir%
#Include .\lib\MonitorExGetUtils.ahk
;==============================================================
; mouseGetWhichMonitor — Determine the monitor index containing the mouse cursor.
;
; GitHub: https://github.com/SevenKeyboard/mouse-get-which-monitor
; Author: SevenKeyboard Ltd. (2025)
; License: The Unlicense
;==============================================================
class VersionManager_mouseGetWhichMonitor
{
    static _ := VersionManager_mouseGetWhichMonitor._init()
    _init()    {
        global
        MOUSEGETWHICHMONITOR_VERSION := "1.0.0"
        if (!this._verCheck(MONITOREXGETUTILS_VERSION , "1.0.0"))
            throw exception("MonitorExGetUtils version 1.x is required (minimum 1.0.0).")
        return true
    }
    _verCheck(byRef actual, required)    {
        if !isSet(actual)
            return false
        actualMajor     := strSplit(actual, ".",, 2)[1]
        requiredMajor   := strSplit(required, ".",, 2)[1]
        if (actualMajor !== requiredMajor)
            return false
        return verCompare(actual, ">=" required)
    }
}
mouseGetWhichMonitor(x:="", y:="")    {
    if (x=="" || y=="")    {
        varSetCapacity(POINT, 8, 0)
        if (!dllCall("User32.dll\GetCursorPos", "Ptr",&POINT))
            return 0
        x:=numGet(POINT,0,"Int"), y:=numGet(POINT,4,"Int")
    }
    for N,info in monitorExGetInfoList()    {
        if (info.rcMonitor.left<=x && x<=info.rcMonitor.right && info.rcMonitor.top<=y && y<=info.rcMonitor.bottom)
            return N
    }
    return 0
}