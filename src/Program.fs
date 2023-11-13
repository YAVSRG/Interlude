﻿open System
open System.IO
open System.Threading
open System.Diagnostics
open System.Reflection
open Percyqaz.Common
open Percyqaz.Shell
open Percyqaz.Flux
open Percyqaz.Flux.Windowing
open Interlude
open Interlude.UI
open Interlude.Features
open Interlude.Features.Import
open Interlude.Features.Printerlude
open Interlude.Features.Online

let launch (instance: int) =
    Logging.Verbosity <-
        if Prelude.Common.DEV_MODE then
            LoggingLevel.DEBUG
        else
            LoggingLevel.INFO

    Logging.LogFile <- Some(Path.Combine("Logs", sprintf "log-%s.txt" (DateTime.Today.ToString("yyyyMMdd"))))

    if OperatingSystem.IsWindows() then
        Process.GetCurrentProcess().PriorityClass <- ProcessPriorityClass.High

    let crash_splash =
        Utils.splash_message_picker ("CrashSplashes.txt")
        >> (fun s -> Logging.Critical s)

    try
        Options.load (instance)
    with err ->
        Logging.Critical("Fatal error loading game config", err)
        crash_splash ()
        Console.ReadLine() |> ignore

    let mutable has_shutdown = false

    let shutdown (unexpected) =
        if has_shutdown then
            ()
        else
            has_shutdown <- true
            Gameplay.save ()
            Options.save ()
            Network.shutdown ()
            //DiscordRPC.shutdown()
            Printerlude.shutdown ()

            if unexpected then
                crash_splash ()
                Logging.Critical("The game crashed or quit abnormally, but was able to shut down correctly")
            else
                Logging.Info("Thank you for playing")

            Logging.Shutdown()

    Window.after_init.Add(fun () ->
        Content.init Options.options.Theme.Value Options.options.Noteskin.Value
        Options.Hotkeys.init Options.options.Hotkeys
        Printerlude.init (instance)
        //DiscordRPC.init()

        AppDomain.CurrentDomain.ProcessExit.Add(fun args -> shutdown (true))
    )

    Window.on_file_drop.Add(Import.handle_file_drop)

    use icon_stream = Utils.get_resource_stream ("icon.png")
    use icon = Utils.Bitmap.load icon_stream

    Launch.entryPoint (Options.config, "Interlude", Startup.ui_entry_point (), Some icon)

    shutdown (false)

[<EntryPoint>]
let main argv =
    let executable_location = Assembly.GetExecutingAssembly().Location
    Directory.SetCurrentDirectory(Path.GetDirectoryName(executable_location))

    if
        not (File.Exists("bass.dll"))
        && not (File.Exists("libbass.iso"))
        && not (File.Exists("libbass.dylib"))
    then
        printfn "Interlude is missing the appropriate audio library dll/iso/dylib for your platform.\n If you are a developer, info on how to fix this is at https://github.com/YAVSRG/YAVSRG#readme\n If you are not a developer, looks like you deleted a file you shouldn't have!\n Redownloading the game and extracting the zip over this folder to replace what is missing should fix it."
        -1
    else

    let m = new Mutex(true, "Interlude")

    if argv.Length > 0 then

        if m.WaitOne(TimeSpan.Zero, true) then
            printfn "Error: Interlude is not running!"
            m.ReleaseMutex()

        else if argv.Length > 0 then
            match Shell.IPC.send "Interlude" (String.concat " " argv) with
            | Some success -> printfn "%s" success
            | None -> printfn "Error: Connection timed out!"

    else if

        m.WaitOne(TimeSpan.Zero, true)
    then
        launch (0)
        m.ReleaseMutex()

        if Utils.AutoUpdate.restart_on_exit then
            m.Dispose()

            if OperatingSystem.IsWindows() then
                let executable = executable_location.Replace(".dll", ".exe")
                let launch_dir = Path.GetDirectoryName executable

                try
                    let _ =
                        Process.Start(
                            ProcessStartInfo(executable, WorkingDirectory = launch_dir, UseShellExecute = true)
                        )

                    printfn "Restarting"
                with err ->
                    printfn "Automatic restart failed :("
                    printfn "%O" err

    elif Prelude.Common.DEV_MODE then
        let instances = Process.GetProcessesByName "Interlude" |> Array.length
        launch (instances - 1)
    else
        // todo: command to maximise/show Interlude window when already running
        printfn "Interlude is already running!"

    0
