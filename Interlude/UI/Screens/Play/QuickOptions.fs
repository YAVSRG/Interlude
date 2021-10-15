﻿namespace Interlude.UI.Screens.Play

open Prelude.Common
open Interlude
open Interlude.Options
open Interlude.UI.Components.Selection
open Interlude.UI.Components.Selection.Controls
open Interlude.UI.Components.Selection.Menu

module QuickOptions =

    let page() : SelectionPage =
        {
            Content = fun add ->
                let firstNote = Gameplay.currentChart.Value.Notes.First |> Option.map Prelude.ChartFormats.Interlude.offsetOf |> Option.defaultValue 0.0f<ms>
                let offset = Gameplay.chartSaveData.Value.Offset
                column [
                    PrettySetting("SongAudioOffset",
                        Slider(
                            Setting.make
                                (fun v -> Gameplay.chartSaveData.Value.Offset <- toTime v + firstNote; Audio.localOffset <- toTime v)
                                (fun () -> float (Gameplay.chartSaveData.Value.Offset - firstNote))
                            |> Setting.bound -200.0 200.0
                            |> Setting.round 0, 0.01f)
                    ).Position(200.0f)
                    PrettySetting("ScrollSpeed", Slider(options.ScrollSpeed, 0.005f)).Position(280.0f)
                    PrettySetting("HitPosition", Slider(options.HitPosition, 0.005f)).Position(360.0f)
                    PrettySetting("Upscroll", Selector.FromBool options.Upscroll).Position(440.0f)
                    //PrettySetting("BackgroundDim", Slider(options.BackgroundDim :?> FloatSetting, 0.01f)).Position(440.0f)
                    //PrettyButton("ScoreSystems", fun () -> add("ScoreSystems", scoreSystems(add))).Position(560.0f)
                    //PrettyButton("LifeSystems", ignore).Position(640.0f)
                ] :> Selectable
            Callback = Audio.playLeadIn
        }

    let show() = { new SelectionMenu(page()) with override this.OnClose() = base.OnClose(); Audio.playLeadIn() }.Show()