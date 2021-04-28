module internal WinNative

open System
open System.Runtime.InteropServices

[<Literal>]
let INPUT_MOUSE : uint32 = 0u
[<Literal>]
let INPUT_KEYBOARD : uint32 = 1u
[<Literal>]
let INPUT_HARDWARE : uint32 = 2u

[<Literal>]
let KEYEVENTF_EXTENDEDKEY : uint32 = 0x0001u
[<Literal>]
let KEYEVENTF_KEYUP : uint32 = 0x0002u
[<Literal>]
let KEYEVENTF_SCANCODE : uint32 = 0x0008u
[<Literal>]
let KEYEVENTF_UNICODE : uint32 = 0x0004u


[<StructLayout(LayoutKind.Sequential)>]
type MOUSEINPUT = struct
    val dx: int32
    val dy:int32
    val mouseData:uint32
    val dwFlags: uint32
    val time: uint32
    val dwExtraInfo: UIntPtr
    new(_dx, _dy, _mouseData, _dwFlags, _time, _dwExtraInfo) = {dx=_dx; dy=_dy; mouseData=_mouseData; dwFlags=_dwFlags; time=_time; dwExtraInfo=_dwExtraInfo}
end

[<StructLayout(LayoutKind.Sequential)>]
type KEYBDINPUT = struct
    val wVk: uint16
    val wScan: uint16
    val dwFlags: uint32
    val time: uint32
    val dwExtraInfo: UIntPtr
    new(_wVk, _wScan, _dwFlags, _time, _dwExtraInfo) = {wVk =_wVk; wScan = _wScan; dwFlags = _dwFlags; time = _time; dwExtraInfo = _dwExtraInfo}
end

[<StructLayout(LayoutKind.Sequential)>]
type HARDWAREINPUT = struct
    val uMsg: uint32
    val wParamL: uint16
    val wParamH: uint16
    new(_uMsg, _wParamL, _wParamH) = {uMsg = _uMsg; wParamL = _wParamL; wParamH = _wParamH}
end


[<StructLayout(LayoutKind.Explicit)>]
type InputUnion = struct
    [<FieldOffset(0)>]
    val mutable mi : MOUSEINPUT

    [<FieldOffset(0)>]
    val mutable ki : KEYBDINPUT

    [<FieldOffset(0)>]
    val mutable hi : HARDWAREINPUT 
end

[<StructLayout(LayoutKind.Sequential)>]
type LPINPUT  = struct
    val mutable ``type``: uint32 // 1 is keyboard
    val mutable u: InputUnion
end


[<DllImport("user32.dll", SetLastError=true)>]
extern uint32 SendInput(uint32 nInputs, LPINPUT* pInputs, int cbSize)

[<Literal>]
let MOUSEEVENTF_LEFTDOWN = 0x2

[<Literal>]
let MOUSEEVENTF_LEFTUP = 0x4

[<Literal>]
let MOUSEEVENTF_RIGHTDOWN = 0x8

[<Literal>]
let MOUSEEVENTF_RIGHTUP = 0x10

// Should be deprecated
[<DllImport("user32.dll")>]
extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo)