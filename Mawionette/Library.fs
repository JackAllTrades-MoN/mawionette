namespace Mawionette

open System
open System.Threading
open System.Collections.Generic
open ResultUtil

[<AutoOpen>]
module internal Prelude =
    let dummy () = ()
    let rand  = System.Random ()

module RPAError =
    type T = 
        | WindowsNativeAPIError of string
        | UncategorizedError of string

    let msgOf (msg : T) = sprintf "%A" msg

    let windowsNative msg = Error <| WindowsNativeAPIError msg
    let uncategorized msg = Error <| UncategorizedError msg

type RPAError = RPAError.T

module KeyKind =
    type T =
        | ARROW_LEFT
        | ARROW_RIGHT
        | ARROW_UP
        | ARROW_DOWN
        | CHAR of char

    let nameOf = function
        | ARROW_LEFT -> "←"
        | ARROW_RIGHT -> "→"
        | ARROW_UP -> "↑"
        | ARROW_DOWN -> "↓"
        | x -> sprintf "%A" x

type KeyKind = KeyKind.T

module internal KeySet1 =
    open System.Runtime.InteropServices

    let appSignature = UIntPtr(0xA8969u)
    let vkOf = function
        | KeyKind.ARROW_LEFT  -> 0x25us
        | KeyKind.ARROW_RIGHT -> 0x27us
        | KeyKind.ARROW_UP    -> 0x26us
        | KeyKind.ARROW_DOWN  -> 0x28us
        | _ -> failwith "currently unavailable vkeycode"
    let makeOf = function
        | KeyKind.ARROW_LEFT  -> 0x4Bus
        | KeyKind.ARROW_RIGHT -> 0x4Dus
        | KeyKind.ARROW_UP    -> 0x48us
        | KeyKind.ARROW_DOWN  -> 0x50us
        | _ -> failwith "currently unavailable vkeycode"
    let breakOf keyCode : uint16 = makeOf keyCode + 0x80us

    let makeKeybdInput virtualKeyCode scanCode dwFlags =
        let mutable input = WinNative.LPINPUT ()
        input.``type`` <- WinNative.INPUT_KEYBOARD
        input.u.ki <- WinNative.KEYBDINPUT(
            virtualKeyCode, scanCode, dwFlags, uint32 0, appSignature)
        input

    let pressKey keyKind =
        let mutable input = makeKeybdInput (vkOf keyKind) (makeOf keyKind) 0u
        let r = WinNative.SendInput(uint32 1, &&input, Marshal.SizeOf(input))
        if r = 1u then Ok ()
        else RPAError.windowsNative "at pressKey"

    let releaseKey keyKind =
        let mutable input = makeKeybdInput (vkOf keyKind) (breakOf keyKind) WinNative.KEYEVENTF_KEYUP
        let r = WinNative.SendInput(uint32 1, &&input, Marshal.SizeOf(input))
        if r = 1u then Ok ()
        else RPAError.windowsNative "at releaseKey"

    let _typeKey (wait: int) keyKind =
        result {
            do! pressKey keyKind
            Thread.Sleep wait
            do! releaseKey keyKind
        }

    let typeKey = _typeKey 130

module internal MouseEvents =
    let mouseClickL (wait: int) () =
        WinNative.mouse_event(WinNative.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
        Thread.Sleep wait
        WinNative.mouse_event(WinNative.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)

    let mouseClickR (wait: int) () =
        WinNative.mouse_event(WinNative.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0)
        Thread.Sleep wait
        WinNative.mouse_event(WinNative.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0)

module RPAState =
    type LogMode = NoLog | ToFile of string | ToStdout
    type T = {
        logmode   : LogMode
        loopCnt   : Stack<int>
        loopFlag  : Stack<bool>
        mouseWait : int
    }
    let initState = {
        logmode   = NoLog
        loopCnt   = Stack()
        loopFlag  = Stack()
        mouseWait = 100
    }

type RPAState = RPAState.T

module RPA =
    type T<'a> = T of (RPAState -> Result<'a * RPAState, RPAError>)

    let runState s (T f) = f s

    let bind f m = T (fun s ->
        runState s m |> Result.bind (fun (a, s) -> runState s (f a)))

    let result a = T (fun s -> Ok(a, s))

    let fail e = T (fun s -> Error e)

    let lift m = T ((fun s -> Result.map (fun v -> v, s) m))

    let getState = T (fun s -> Ok (s, s))

    let setState a = T (fun s -> Ok ((), a))

    let (>>=) m f = bind f m

    type RPABuilder () =
        member x.Bind(comp, func) = bind func comp
        member x.Return(value) = result value
        member x.ReturnFrom(value) = value
        member x.Zero () = result ()
        member x.Combine (e1, e2) = e1 |> bind (fun () -> e2)

    let rpa = new RPABuilder ()

    let exec m =
        runState RPAState.initState m
        |> Result.mapError RPAError.msgOf
        |> ResultUtil.okOrFailwith
        |> ignore

    let typeKey keyKind = rpa {
        do! lift <| KeySet1.typeKey keyKind
    }

    let loopCount = rpa {
        let! s = getState
        return s.loopCnt.Peek()
    }

    let stopLoop = rpa {
        let! s = getState
        s.loopFlag.Pop () |> ignore
        s.loopFlag.Push false
    }

    let startLoggingToStdout = rpa {
        let! s = getState
        do! setState { s with logmode = RPAState.LogMode.ToStdout }
    }

    let info msg = rpa {
        let! s = getState
        if s.logmode = RPAState.LogMode.ToStdout
        then printfn "[info] %s" msg
        else failwith "Uninmplemented"
    }

    let enterIntoLoop = rpa {
        let! s = getState
        s.loopFlag.Push(true)
        s.loopCnt.Push(0)
    }

    let escapeFromLoop = rpa {
        let! s = getState
        s.loopFlag.Pop() |> ignore
        s.loopCnt.Pop() |> ignore
    }

    let cntUp = rpa {
        let! s = getState
        let cnt = s.loopCnt.Pop ()
        s.loopCnt.Push (cnt + 1)
    }

    let waitAndContinue waitOffset maxScale m =
        let rec inner = rpa {
            let! s = getState
            if s.loopFlag.Peek() then
                do! m
                do! cntUp
                Thread.Sleep (rand.Next(waitOffset, waitOffset * maxScale))
                do! inner
            else return ()
        }
        rpa {
            do! enterIntoLoop
            do! inner
            do! escapeFromLoop
        }

    let mouseClickR = rpa {
        let! s = getState
        MouseEvents.mouseClickR s.mouseWait ()
    }

type RPACmds<'a> = RPA.T<'a>