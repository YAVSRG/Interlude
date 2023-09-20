﻿namespace Interlude.Features.Import

open System.IO
open Percyqaz.Common
open Percyqaz.Flux.Input
open Percyqaz.Flux.UI
open Prelude.Common
open Prelude.Charts.Formats.Conversions
open Prelude.Data.Charts.Library.Imports
open Interlude.Options
open Interlude.Utils
open Interlude.UI
open Interlude.UI.Menu

module Mounts =
    
    let mutable dropFunc : (string -> unit) option = None

    type Game =
        | Osu = 0
        | Stepmania = 1
        | Etterna = 2

    type EditorPage(setting: Setting<MountedChartSource option>) as this =
        inherit Page()

        let mount = setting.Value.Value
        let importOnStartup = Setting.simple mount.ImportOnStartup
        let mutable import = false

        do
            this.Content(
                column()
                |+ PageSetting("mount.importatstartup", Selector<_>.FromBool importOnStartup)
                    .Pos(200.0f)
                    .Tooltip(Tooltip.Info("mount.importatstartup"))
                |+ PageButton.Once(
                        "mount.import",
                        fun () -> 
                            import <- true
                            Notifications.action_feedback(Icons.add_to_collection, L"notification.import_queued", "") )
                    .Pos(300.0f)
                    .Tooltip(Tooltip.Info("mount.import"))
                |+ PageButton.Once(
                        "mount.importall",
                        fun () -> 
                            import <- true
                            mount.LastImported <- System.DateTime.UnixEpoch
                            Notifications.action_feedback(Icons.add_to_collection, L"notification.import_queued", "") )
                    .Pos(370.0f)
                    .Tooltip(Tooltip.Info("mount.importall"))
            )

        override this.Title = L"mount.name"
        override this.OnClose() =
            setting.Value <- Some { mount with ImportOnStartup = importOnStartup.Value }
            if import then import_mounted_source.Request(setting.Value.Value, ignore)

    let handleStartupImports() =
        // todo: ingame notification if mount has moved
        match options.OsuMount.Value with
        | Some mount ->
            Logging.Info("Checking for new osu! songs to import..")
            if Directory.Exists mount.SourceFolder then
                if mount.ImportOnStartup then import_mounted_source.Request(mount, ignore)
            else Logging.Warn("osu! Songs folder has moved or can no longer be found.\n This may break any mounted songs, if so you will need to set up the link again.")
        | None -> ()
        match options.StepmaniaMount.Value with
        | Some mount -> 
            Logging.Info("Checking for new Stepmania songs to import..")
            if Directory.Exists mount.SourceFolder then
                if mount.ImportOnStartup then import_mounted_source.Request(mount, ignore)
            else Logging.Warn("Stepmania Songs folder has moved or can no longer be found.\n This may break any mounted songs, if so you will need to set up the link again.")
        | None -> ()
        match options.EtternaMount.Value with
        | Some mount ->
            Logging.Info("Checking for new Etterna songs to import..")
            if Directory.Exists mount.SourceFolder then
                if mount.ImportOnStartup then import_mounted_source.Request(mount, ignore)
            else Logging.Warn("Etterna Songs folder has moved or can no longer be found.\n This may break any mounted songs, if so you will need to set up the link again.")
        | None -> ()

    type CreateDialog(mountType: Game, setting: Setting<MountedChartSource option>) as this =
        inherit Dialog()

        let text = 
            Text(
                match mountType with
                | Game.Osu -> L"imports.mount.create.osu.hint"
                | Game.Stepmania -> L"imports.mount.create.stepmania.hint"
                | Game.Etterna -> L"imports.mount.create.etterna.hint"
                | _ -> failwith "impossible"
                ,
                Align = Alignment.CENTER,
                Position = { Left = 0.0f %+ 100.0f; Top = 0.5f %- 200.0f; Right = 1.0f %- 100.0f; Bottom = 0.5f %+ 200.0f })

        let button =
            Button(
                L"imports.mount.create.auto",
                (fun () ->
                    match mountType with
                    | Game.Osu -> osuSongFolder
                    | Game.Stepmania -> stepmaniaPackFolder
                    | Game.Etterna -> etternaPackFolder
                    | _ -> failwith "impossible"
                    |> dropFunc.Value),
                Position = { Left = 0.5f %- 150.0f; Top = 0.5f %+ 200.0f; Right = 0.5f %+ 150.0f; Bottom = 0.5f %+ 260.0f })

        do
            dropFunc <-
                fun path ->
                    match mountType, path with
                    | Game.Osu, PackFolder -> setting.Value <- MountedChartSource.Pack ("osu!", path) |> Some
                    | Game.Osu, _ -> Notifications.error (L"imports.mount.create.osu.error", "")
                    | Game.Stepmania, FolderOfPacks
                    | Game.Etterna, FolderOfPacks -> setting.Value <- MountedChartSource.Library path |> Some
                    | Game.Stepmania, _ -> Notifications.error (L"imports.mount.create.stepmania.error", "")
                    | Game.Etterna, _ -> Notifications.error (L"imports.mount.create.etterna.error", "")
                    | _ -> failwith "impossible"
                    if setting.Value.IsSome then 
                        import_mounted_source.Request(setting.Value.Value, ignore)
                        Notifications.action_feedback(Icons.add_to_collection, L"notification.import_queued", "")
                        this.Close()
                |> Some

        override this.Update(elapsedTime, bounds) =
            base.Update(elapsedTime, bounds)
            text.Update(elapsedTime, bounds)
            button.Update(elapsedTime, bounds)
            if (!|"exit").Tapped() then this.Close()

        override this.Draw() =
            text.Draw()
            button.Draw()

        override this.Close() = 
            dropFunc <- None
            base.Close()

        override this.Init(parent: Widget) =
            base.Init parent
            text.Init this
            button.Init this

    type Control(mountType: Game, setting: Setting<MountedChartSource option>) as this =
        inherit Frame(NodeType.Switch(fun _ -> this.WhoShouldFocus))

        let createButton = 
            Button(sprintf "%s %s" Icons.import_local (L"imports.mount.create"),
                (fun () -> CreateDialog(mountType, setting).Show()),
                Position = Position.SliceBottom(60.0f).Margin 5.0f)
        let editButtons =
            SwitchContainer.Row<Button>(Position = Position.SliceBottom(60.0f).Margin 5.0f)
            |+ Button(sprintf "%s %s" Icons.edit (L"imports.mount.edit"),
                (fun () -> EditorPage(setting).Show()),
                Position = { Position.Default with Right = 0.5f %+ 0.0f })
            |+ Button(sprintf "%s %s" Icons.delete (L"imports.mount.delete"),
                (fun () -> setting.Value <- None),
                Position = { Position.Default with Left = 0.5f %+ 0.0f })

        override this.Init(parent: Widget) =
            this
            |+ Text(
                match mountType with
                | Game.Osu -> "osu!mania"
                | Game.Stepmania -> "Stepmania"
                | Game.Etterna -> "Etterna"
                | _ -> failwith "impossible"
                ,
                Position = Position.SliceTop 80.0f,
                Align = Alignment.CENTER)
            |* Text(
                fun () -> 
                    match setting.Value with
                    | Some s -> Localisation.localiseWith [if s.LastImported = System.DateTime.UnixEpoch then "--" else s.LastImported.ToString()] "imports.mount.lastimported"
                    | None -> L"imports.mount.notlinked"
                ,
                Color = K Colors.text_subheading,
                Position = Position.SliceTop(145.0f).TrimTop(85.0f),
                Align = Alignment.CENTER)
            base.Init parent
            createButton.Init this
            editButtons.Init this

        member private this.WhoShouldFocus : Widget =
            if setting.Value.IsSome then editButtons else createButton

        override this.Update(elapsedTime, moved) =
            base.Update(elapsedTime, moved)
            this.WhoShouldFocus.Update(elapsedTime, moved)

        override this.Draw() =
            base.Draw()
            this.WhoShouldFocus.Draw()

    let tab =
        SwitchContainer.Column<Widget>()
        |+ Control(Game.Osu, options.OsuMount,
            Position = Position.Row(100.0f, 200.0f) )
        |+ Control(Game.Stepmania, options.StepmaniaMount,
            Position = Position.Row(320.0f, 200.0f) )
        |+ Control(Game.Etterna, options.EtternaMount,
            Position = Position.Row(540.0f, 200.0f) )
        |+ Text(L"imports.mount",
            Align = Alignment.CENTER,
            Position = Position.Row(0.0f, 80.0f) )
        |+ Text(L"imports.drag_and_drop_hint",
            Align = Alignment.CENTER,
            Position = Position.Row(740.0f, 80.0f) )

    open System
    open System.Text
    open Prelude.Charts.Formats.``osu!``
    open Prelude.Charts.Formats.Interlude
    open Prelude.Charts.Formats.Conversions
    open Prelude.Gameplay
    open Prelude.Data.``osu!``
    open Prelude.Data.Scores
    open Prelude.Data.Charts
    open Prelude.Data.Charts.Caching

    let import_osu_scores() =
        match options.OsuMount.Value with
        | None -> Logging.Warn "Requires osu! Songs folder to be mounted"
        | Some m ->

        Logging.Info "Reading osu! database ..."
        let scores =
            use file = Path.Combine(m.SourceFolder, "..", "scores.db") |> File.OpenRead
            use reader = new BinaryReader(file, Encoding.UTF8)
            ScoreDatabase.Read(reader)

        let main_db =
            use file = Path.Combine(m.SourceFolder, "..", "osu!.db") |> File.OpenRead
            use reader = new BinaryReader(file, Encoding.UTF8)
            OsuDatabase.Read(reader)

        Logging.Info (sprintf "Read %s's osu! database containing %i maps" main_db.PlayerName main_db.Beatmaps.Length)

        let mutable chart_count = 0
        let mutable score_count = 0

        let find_matching_chart (beatmap_data: OsuDatabase_Beatmap) (chart: Chart) =
            let chart_hash = Chart.hash chart
            match Cache.by_hash chart_hash Library.cache with
            | None -> 
                match ``osu!``.detect_rate_mod beatmap_data.Difficulty with
                | Some rate ->
                    let chart = Chart.scale rate chart
                    let chart_hash = Chart.hash chart
                    match Cache.by_hash chart_hash Library.cache with
                    | None -> 
                        Logging.Warn(sprintf "%s [%s] is %.2fx, couldn't find a matching 1.0x so skipping" beatmap_data.TitleUnicode beatmap_data.Difficulty rate)
                        None
                    | Some _ ->
                        Some (chart, chart_hash, rate)
                | None -> 
                    Logging.Warn(sprintf "%s [%s] skipped" beatmap_data.TitleUnicode beatmap_data.Difficulty)
                    None
            | Some _ -> Some (chart, chart_hash, 1.0f)

        for beatmap_score_data in scores.Beatmaps |> Seq.where (fun b -> b.Scores.Length > 0 && b.Scores.[0].Mode = 3uy) do
            match main_db.Beatmaps |> Seq.tryFind (fun b -> b.Hash = beatmap_score_data.Hash && b.Mode = 3uy) with
            | None -> ()
            | Some beatmap_data ->

            let osu_file = Path.Combine(m.SourceFolder, beatmap_data.FolderName, beatmap_data.Filename)
            match
                try
                    loadBeatmapFile osu_file
                    |> fun b -> ``osu!``.toInterlude b { Config = ConversionOptions.Default; Source = osu_file }
                    |> Some
                with _ -> None
            with
            | None -> ()
            | Some chart ->

            match find_matching_chart beatmap_data chart with
            | None -> ()
            | Some (chart, _, rate) ->

            chart_count <- chart_count + 1

            for score in beatmap_score_data.Scores do
                let replay_file = Path.Combine(m.SourceFolder, "..", "Data", "r", sprintf "%s-%i.osr" score.BeatmapHash score.Timestamp)
                match
                    try
                        use file = File.OpenRead replay_file
                        use br = new BinaryReader(file)
                        Some (ScoreDatabase_Score.Read br)
                    with err -> Logging.Error(sprintf "Error loading replay file %s" replay_file, err); None
                with
                | None -> ()
                | Some replay_info ->

                match Mods.to_interlude_rate_and_mods replay_info.ModsUsed with
                | None -> () // score is invalid for import in some way, skip
                | Some (rate2, mods) ->

                let combined_rate = rate2 * rate
                if MathF.Round(combined_rate, 3) <> MathF.Round(combined_rate, 2) || combined_rate > 2.0f || combined_rate < 0.5f then
                    Logging.Info(sprintf "Skipping score with rate %.3f because this isn't supported in Interlude" combined_rate)
                else

                let replay_data = decode_replay (replay_info, chart, rate)

                let score : Score = 
                    { 
                        time = DateTime.FromFileTimeUtc(replay_info.Timestamp).ToLocalTime()
                        replay = Replay.compress replay_data
                        rate = MathF.Round(combined_rate, 2)
                        selectedMods = mods
                        layout = Layout.Layout.LeftTwo
                        keycount = chart.Keys
                    }

                let data = Scores.getOrCreateData chart
                if data.Scores.RemoveAll(fun s -> s.time.ToUniversalTime().Ticks = score.time.ToUniversalTime().Ticks) = 0 then score_count <- score_count + 1
                data.Scores.Add(score) 

        Logging.Info(sprintf "Finished importing osu! scores (%i scores from %i maps)" score_count chart_count)