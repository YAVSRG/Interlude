﻿namespace Interlude.UI.Screens.LevelSelect

open System
open Prelude.Common
open Prelude.Data.Charts.Caching
open Prelude.Gameplay.Mods
open Prelude.Gameplay.Difficulty
open Prelude.ChartFormats.Interlude
open Interlude.UI
open Interlude.Utils
open Interlude.Gameplay
open Interlude.UI.Components

type ChartInfo() as this =
    inherit Widget()

    let scores = Scoreboard()
    let mutable length = ""
    let mutable bpm = ""
    let mutable notecount = ""

    do
        scores
        |> positionWidgetA(0.0f, 0.0f, 0.0f, -200.0f)
        |> this.Add

        new TextBox(
            (fun () -> sprintf "%.2f%s" (match Chart.rating with None -> 0.0 | Some d -> d.Physical) Interlude.Icons.star),
            (fun () -> Color.White, match Chart.rating with None -> Color.Black | Some d -> physicalColor d.Physical), 0.0f)
        |> positionWidget(10.0f, 0.0f, -190.0f, 1.0f, 0.0f, 0.5f, -120.0f, 1.0f)
        |> this.Add

        new TextBox(
            (fun () -> sprintf "%.2f%s" (match Chart.rating with None -> 0.0 | Some d -> d.Technical) Interlude.Icons.star),
            (fun () -> Color.White, match Chart.rating with None -> Color.Black | Some d -> technicalColor d.Technical), 0.0f)
        |> positionWidget(10.0f, 0.0f, -120.0f, 1.0f, 0.0f, 0.5f, -50.0f, 1.0f)
        |> this.Add

        new TextBox((fun () -> bpm), K (Color.White, Color.Black), 1.0f)
        |> positionWidget(0.0f, 0.5f, -190.0f, 1.0f, -10.0f, 1.0f, -120.0f, 1.0f)
        |> this.Add

        new TextBox((fun () -> length), K (Color.White, Color.Black), 1.0f)
        |> positionWidget(0.0f, 0.5f, -120.0f, 1.0f, -10.0f, 1.0f, -50.0f, 1.0f)
        |> this.Add

        new TextBox((fun () -> notecount), K (Color.White, Color.Black), 1.0f)
        |> positionWidget(10.0f, 0.0f, -50.0f, 1.0f, -17.0f, 1.0f, -10.0f, 1.0f)
        |> this.Add

        new TextBox((fun () -> getModString(rate.Value, selectedMods.Value, autoplay)), K (Color.White, Color.Black), 0.0f)
        |> positionWidget(17.0f, 0.0f, -50.0f, 1.0f, -10.0f, 1.0f, -10.0f, 1.0f)
        |> this.Add

    member this.Refresh() =
        length <-
            match Chart.cacheInfo with
            | Some cc -> cc.Length
            | None -> 0.0f<ms>
            |> fun x -> x / rate.Value
            |> fun x -> (x / 1000.0f / 60.0f |> int, (x / 1000f |> int) % 60)
            |> fun (x, y) -> sprintf "%s %i:%02i" Interlude.Icons.time x y
        bpm <-
            match Chart.cacheInfo with
            | Some cc -> cc.BPM
            | None -> (500.0f<ms/beat>, 500.0f<ms/beat>)
            |> fun (b, a) -> (60000.0f<ms> / a * rate.Value |> int, 60000.0f<ms> / b * rate.Value |> int)
            |> fun (a, b) ->
                if a > 9000 || b < 0 then sprintf "%s ∞" Interlude.Icons.bpm
                elif Math.Abs(a - b) < 5 || b > 9000 then sprintf "%s %i" Interlude.Icons.bpm a
                else sprintf "%s %i-%i" Interlude.Icons.bpm a b
        notecount <-
            match Chart.current with
            | Some c ->
                let mutable notes = 0
                let mutable lnotes = 0
                for (_, nr) in c.Notes.Data do
                    for n in nr do
                        if n = NoteType.NORMAL then notes <- notes + 1
                        elif n = NoteType.HOLDHEAD then notes <- notes + 1; lnotes <- lnotes + 1
                sprintf "%iK | %i Notes | %.0f%% Holds" c.Keys notes (100.0f * float32 lnotes / float32 notes)
            | None -> ""
        scores.Refresh()