﻿namespace Interlude.Features.Play

open Percyqaz.Common
open Percyqaz.Flux.Audio
open Percyqaz.Flux.Input
open Percyqaz.Flux.UI
open Percyqaz.Flux.Graphics
open Prelude.Common
open Prelude.Charts.Formats.Interlude
open Prelude.Charts.Tools.Patterns
open Prelude.Scoring
open Prelude.Scoring.Metrics
open Interlude.Options
open Interlude.Content
open Interlude.UI
open Interlude.Utils
open Interlude.Features
open Interlude.Features.Play.HUD

module PracticeScreen =

    module Sync =

        [<RequireQualifiedAccess>]
        type Mode =
            | HIT_POSITION
            | SCROLL_SPEED
            | VISUAL_OFFSET
            | AUDIO_OFFSET
            member this.Audio =
                match this with
                | HIT_POSITION
                | SCROLL_SPEED
                | VISUAL_OFFSET -> false
                | AUDIO_OFFSET -> true

        let mutable suggested = false
        let mutable mode = Mode.AUDIO_OFFSET
        let mutable mean = 0.0f<ms>

        let update_mean(scoring: ScoreMetric) =
            let mutable sum = 0.0f<ms>
            let mutable count = 1.0f
            for ev in scoring.HitEvents do
                match ev.Guts with
                | Hit x when not x.Missed ->
                    sum <- sum + x.Delta
                    count <- count + 1.0f
                | _ -> ()
            mean <- sum / count * Gameplay.rate.Value
            suggested <- true

        type Panel(content: Widget) =
            inherit StaticContainer(NodeType.Switch(fun () -> content))

            override this.Init(parent) =
                this.Add content
                base.Init parent

            override this.Draw() =
                Draw.rect this.Bounds Colors.shadow_1.O2
                Draw.rect (this.Bounds.Expand(0.0f, Style.padding).SliceBottom(Style.padding)) Colors.shadow_2.O3
                base.Draw()
        
        type ModeButton(label: string, m: Mode, apply_suggestion: unit -> unit) =
            inherit StaticContainer(NodeType.Button(fun () -> if mode = m then apply_suggestion() else mode <- m))

            let requires_audio = m.Audio

            override this.Init(parent) =
                this 
                |+ Text(label, Color = fun () -> 
                        if requires_audio <> (options.AudioVolume.Value > 0.0) then 
                            Colors.text_greyout
                        elif this.Focused then Colors.text_yellow_2
                        else Colors.text
                    )
                |* Clickable.Focus this
                base.Init parent

            override this.Draw() =
                if this.Focused then Draw.rect this.Bounds Colors.yellow_accent.O2
                elif mode = m then Draw.rect this.Bounds Colors.pink_accent.O2
                base.Draw()

            override this.Update(elapsedTime, moved) =
                base.Update(elapsedTime, moved)
                if this.Focused && not this.Selected && (!|"right").Tapped() then this.Select()
                elif this.Selected && (!|"left").Tapped() then this.Focus()

        type UI() =
            inherit StaticContainer(NodeType.None)

            let firstNote = Gameplay.Chart.current.Value.FirstNote

            let local_audio_offset = 
                Setting.make
                    (fun v -> Gameplay.Chart.saveData.Value.Offset <- v + firstNote; Song.changeLocalOffset(v))
                    (fun () -> (Gameplay.Chart.saveData.Value.Offset - firstNote))
                |> Setting.bound -200.0f<ms> 200.0f<ms>
            let mutable local_audio_offset_suggestion = local_audio_offset.Value

            let visual_offset = options.VisualOffset |> Setting.roundf 0
            let mutable visual_offset_suggestion = visual_offset.Value

            let scroll_speed = options.ScrollSpeed |> Setting.roundf 2
            let mutable scroll_speed_suggestion = scroll_speed.Value

            let hit_position = options.HitPosition |> Setting.roundf 0
            let mutable hit_position_suggestion = hit_position.Value

            let accept_suggestion_hint = Localisation.localiseWith [(!|"accept_suggestion").ToString()] "practice.suggestions.accepthint"
            let about_right_hint = Icons.check + " " + L"practice.suggestions.aboutright"
            let suggestion_text() = Text(fun () -> 
                if not suggested then L"practice.suggestions.hint" 
                elif mean < 5.0f<ms> && mean > -5.0f<ms> then about_right_hint 
                else accept_suggestion_hint)

            member this.UpdateSuggestions() =
                hit_position_suggestion <- hit_position.Value - mean * options.ScrollSpeed.Value * 1.0f</ms>

                let expected_pixels = (1080.0f - options.HitPosition.Value) * 0.6f
                let current_lead_time = expected_pixels / (options.ScrollSpeed.Value * 1.0f</ms>)
                let desired_lead_time = current_lead_time - mean
                scroll_speed_suggestion <- expected_pixels / float32 desired_lead_time

                visual_offset_suggestion <- visual_offset.Value + mean / 1.0f<ms>
                local_audio_offset_suggestion <- local_audio_offset.Value - mean

            member this.AcceptSuggestion() =
                match mode with
                | Mode.HIT_POSITION -> hit_position.Set hit_position_suggestion
                | Mode.SCROLL_SPEED -> scroll_speed.Set scroll_speed_suggestion
                | Mode.VISUAL_OFFSET -> visual_offset.Set visual_offset_suggestion
                | Mode.AUDIO_OFFSET -> local_audio_offset.Set local_audio_offset_suggestion
                hit_position_suggestion <- hit_position.Value
                scroll_speed_suggestion <- scroll_speed.Value
                visual_offset_suggestion <- visual_offset.Value
                local_audio_offset_suggestion <- local_audio_offset.Value

            override this.Init(parent) =
                this
                |+
                    Panel(
                        FlowContainer.Vertical<ModeButton>(50.0f, Spacing = 10.0f)
                        |+ ModeButton(L"gameplay.hitposition.name", Mode.HIT_POSITION, this.AcceptSuggestion)
                        |+ ModeButton(L"gameplay.scrollspeed.name", Mode.SCROLL_SPEED, this.AcceptSuggestion)
                        |+ ModeButton(L"system.visualoffset.name", Mode.VISUAL_OFFSET, this.AcceptSuggestion)
                        |+ ModeButton(L"quick.localoffset.name", Mode.AUDIO_OFFSET, this.AcceptSuggestion)
                        ,
                        Position = Position.Box(0.0f, 0.0f, 20.0f, 350.0f, 300.0f, 230.0f)
                    )
                |+ Conditional((fun () -> not mode.Audio && options.AudioVolume.Value > 0.0),
                        Callout.frame
                            (Callout.Small.Icon(Icons.audio_mute).Title(L"practice.mute_hint"))
                            (fun (w, h) -> Position.Box(0.0f, 0.0f, 20.0f, 650.0f, w, h + 40.0f))
                    )
                |+ Conditional((fun () -> mode.Audio && options.AudioVolume.Value = 0.0),
                        Callout.frame
                            (Callout.Small.Icon(Icons.audio_on).Title(L"practice.unmute_hint"))
                            (fun (w, h) -> Position.Box(0.0f, 0.0f, 20.0f, 650.0f, w, h + 40.0f))
                    )
                |+ Conditional((fun () -> mode = Mode.HIT_POSITION && options.AudioVolume.Value = 0.0),
                        Panel(
                            FlowContainer.Vertical<Widget>(50.0f, Spacing = 10.0f)
                            |+ Text(fun () -> sprintf "Current: %.0f" hit_position.Value)
                            |+ Text(fun () -> sprintf "Suggested: %.0f" hit_position_suggestion)
                            |+ suggestion_text() 
                            ,
                            Position = Position.Box(0.0f, 0.0f, 20.0f, 650.0f, 500.0f, 170.0f)
                        )
                    )
                |+ Conditional((fun () -> mode = Mode.SCROLL_SPEED && options.AudioVolume.Value = 0.0),
                        Panel(
                            FlowContainer.Vertical<Widget>(50.0f, Spacing = 10.0f)
                            |+ Text(fun () -> sprintf "Current: %.0f%%" (100.0f * scroll_speed.Value))
                            |+ Text(fun () -> sprintf "Suggested: %.0f%%" (100.0f * scroll_speed_suggestion))
                            |+ suggestion_text() 
                            ,
                            Position = Position.Box(0.0f, 0.0f, 20.0f, 650.0f, 500.0f, 170.0f)
                        )
                    )
                |+ Conditional((fun () -> mode = Mode.VISUAL_OFFSET && options.AudioVolume.Value = 0.0),
                        Panel(
                            FlowContainer.Vertical<Widget>(50.0f, Spacing = 10.0f)
                            |+ Text(fun () -> sprintf "Current: %.0f" visual_offset.Value)
                            |+ Text(fun () -> sprintf "Suggested: %.0f" visual_offset_suggestion)
                            |+ suggestion_text() 
                            ,
                            Position = Position.Box(0.0f, 0.0f, 20.0f, 650.0f, 500.0f, 170.0f)
                        )
                    )
                |* Conditional((fun () -> mode = Mode.AUDIO_OFFSET && options.AudioVolume.Value > 0.0),
                        Panel(
                            FlowContainer.Vertical<Widget>(50.0f, Spacing = 10.0f)
                            |+ Text(fun () -> sprintf "Current: %.0f" local_audio_offset.Value)
                            |+ Text(fun () -> sprintf "Suggested: %.0f" local_audio_offset_suggestion)
                            |+ suggestion_text() 
                            ,
                            Position = Position.Box(0.0f, 0.0f, 20.0f, 650.0f, 500.0f, 170.0f)
                        )
                    )
                base.Init parent

    type Timeline(chart: Chart, on_seek: Time -> unit) =
        inherit StaticWidget(NodeType.None)

        let density_graph_1, density_graph_2 = Analysis.density 100 chart
        let density_graph_1, density_graph_2 = Array.map float32 density_graph_1, Array.map float32 density_graph_2
        let max_note_density = Array.max density_graph_1

        override this.Draw() =
            let b = this.Bounds.Shrink(10.0f, 20.0f)
            let start = chart.FirstNote - Song.LEADIN_TIME
        
            let w = b.Width / float32 density_graph_1.Length
            for i = 0 to density_graph_1.Length - 1 do
                let h = 80.0f * density_graph_1.[i] / max_note_density
                let h2 = 80.0f * density_graph_2.[i] / max_note_density
                Draw.rect (Rect.Box(b.Left + float32 i * w, b.Bottom - h, w, h - 5.0f)) (Color.FromArgb(120, Color.White))
                Draw.rect (Rect.Box(b.Left + float32 i * w, b.Bottom - h2, w, h2 - 5.0f)) (Style.color(80, 1.0f, 0.0f))
        
            let percent = (Song.time() - start) / (chart.LastNote - start) 
            Draw.rect (b.SliceBottom(5.0f)) (Color.FromArgb(160, Color.White))
            let x = b.Width * percent
            Draw.rect (b.SliceBottom(5.0f).SliceLeft x) (Style.color(255, 1.0f, 0.0f))

        override this.Update(elapsedTime, moved) =
            base.Update(elapsedTime, moved)
            if this.Bounds.Bottom - Mouse.y() < 200.0f && Mouse.leftClick() then
                let percent = (Mouse.x() - 10.0f) / (Viewport.vwidth - 20.0f) |> min 1.0f |> max 0.0f
                let start = chart.FirstNote - Song.LEADIN_TIME
                let newTime = start + (chart.LastNote - start) * percent
                on_seek newTime

    let info_callout = 
        Callout.Small
            .Icon(Icons.practice)
            .Title(L"practice.info.title")
            .Body(L"practice.info.desc")
            .Hotkey(L"practice.info.play", "skip")
            .Hotkey(L"practice.info.restart", "retry")
            .Hotkey(L"practice.info.options", "exit")
            .Body(L"practice.info.sync")
            .Hotkey(L"practice.info.accept_suggestion", "accept_suggestion")
    
    let rec practice_screen (practice_point: Time) =

        let chart = Gameplay.Chart.withMods.Value
        let lastNote = offsetOf chart.Notes.Last.Value - 5.0f<ms> - Song.LEADIN_TIME * Gameplay.rate.Value
        let mutable practice_point = min lastNote practice_point

        let mutable playable_notes = TimeData(chart.Notes.EnumerateBetween (practice_point + Song.LEADIN_TIME * Gameplay.rate.Value) Time.infinity)
        let mutable firstNote = offsetOf playable_notes.First.Value
        let mutable liveplay = LiveReplayProvider firstNote
        let mutable scoring = createScoreMetric Rulesets.current chart.Keys liveplay playable_notes Gameplay.rate.Value

        let binds = options.GameplayBinds.[chart.Keys - 3]
        let mutable inputKeyState = 0us

        // options state
        let mutable options_mode = true

        let restart(screen: IPlayScreen) =
            liveplay <- LiveReplayProvider firstNote
            scoring <- createScoreMetric Rulesets.current chart.Keys liveplay playable_notes Gameplay.rate.Value
            screen.State.ChangeScoring scoring
            Song.playFrom(practice_point)

        let play(screen: IPlayScreen) =
            playable_notes <- TimeData(chart.Notes.EnumerateBetween (practice_point + Song.LEADIN_TIME * Gameplay.rate.Value) Time.infinity)
            firstNote <- offsetOf playable_notes.First.Value
            screen.FirstNote <- firstNote
            restart(screen)
            options_mode <- false

        let sync_ui = Sync.UI()

        let pause(screen: IPlayScreen) =
            Song.pause()
            Sync.update_mean scoring
            sync_ui.UpdateSuggestions()
            options_mode <- true

        let options_ui =
            StaticContainer(NodeType.None)
            |+ Timeline(Gameplay.Chart.current.Value, fun t -> practice_point <- min lastNote t; Song.seek t)
            |+ Callout.frame (info_callout) ( fun (w, h) -> Position.Box(0.0f, 0.0f, 20.0f, 20.0f, w, h + 40.0f) )
            |+ sync_ui

        { new IPlayScreen(chart, PacemakerInfo.None, Rulesets.current, scoring, FirstNote = firstNote) with
            override this.AddWidgets() =
                let inline add_widget x = add_widget (this, this.Playfield, this.State) x

                add_widget AccuracyMeter
                add_widget HitMeter
                add_widget LifeMeter
                add_widget ComboMeter
                add_widget ProgressMeter
                add_widget JudgementCounts

                this.Add(Conditional((fun () -> options_mode), options_ui))

            override this.OnEnter(p) =
                base.OnEnter(p)
                Song.seek(practice_point)
                Song.pause()

            override this.OnBack() =
                if not options_mode then
                    pause(this)
                    inputKeyState <- 0us
                    None
                else 
                    Song.resume()
                    base.OnBack()

            override this.Update(elapsedTime, bounds) =
                base.Update(elapsedTime, bounds)
                let now = Song.timeWithOffset()
                let chartTime = now - firstNote

                if not (liveplay :> IReplayProvider).Finished then
                    Input.consumeGameplay(binds, fun column time isRelease ->
                        if isRelease then inputKeyState <- Bitmap.unsetBit column inputKeyState
                        else inputKeyState <- Bitmap.setBit column inputKeyState
                        liveplay.Add(time, inputKeyState) )
                    this.State.Scoring.Update chartTime

                if (!|"retry").Tapped() then
                    if options_mode then play(this) else restart(this)

                elif (!|"accept_suggestion").Tapped() then
                    if options_mode then sync_ui.AcceptSuggestion()
                    else pause(this); sync_ui.AcceptSuggestion(); play(this)

                elif options_mode && (!|"skip").Tapped() then
                    play(this)

                if this.State.Scoring.Finished && not options_mode then
                    pause(this)
        }
    