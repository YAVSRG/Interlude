﻿namespace Interlude.UI.Screens.Score

open System.Drawing
open Prelude.Scoring
open Prelude.Data.ScoreManager
open Interlude
open Interlude.Graphics
open Interlude.UI

type ScoreGraph(data: ScoreInfoProvider) =
    inherit Widget()

    let fbo = FBO.create()
    let mutable refresh = true

    do fbo.Unbind()

    member this.Refresh() = refresh <- true

    member private this.Redraw() =
        refresh <- false
        let width = Rect.width this.Bounds
        let h = 0.5f * Rect.height this.Bounds
        let struct (left, top, right, bottom) = this.Bounds
        fbo.Bind true

        Draw.rect this.Bounds (Color.FromArgb(160, 0, 0, 0)) Sprite.Default
        Draw.rect (Rect.create left (top + h - 2.5f) right (top + h + 2.5f)) (Color.FromArgb(127, 255, 255, 255)) Sprite.Default
        
        // todo: graph stuff like hp/accuracy over time
        // todo: let you filter to just release timing

        let events = data.Scoring.HitEvents
        assert (events.Count > 0)

        let hscale = (width - 10.0f) / events.[events.Count - 1].Time
        for ev in events do
            let y, col =
                match ev.Guts with
                | HitEventGuts.Hit (judgement, delta, _) ->
                    h - delta / data.Scoring.MissWindow * h, Themes.themeConfig.JudgementColors.[int judgement]
                | HitEventGuts.Release (judgement, delta, _, _) ->
                    h - 0.5f * delta / data.Scoring.MissWindow * h, Color.FromArgb(127, Themes.themeConfig.JudgementColors.[int judgement])
                | HitEventGuts.Mine false -> 0.0f, Themes.themeConfig.JudgementColors.[int JudgementType.NG]
                | _ -> 0.0f, Color.Transparent
            if col.A > 0uy then
                let x = left + 5.0f + ev.Time * hscale
                Draw.rect(Rect.create (x - 2.5f) (top + y - 2.5f) (x + 2.5f) (top + y + 2.5f)) col Sprite.Default
        Text.draw(Themes.font(), "Early", 18.0f, left + 5.0f, bottom - 35.0f, Color.FromArgb(127, Color.White))
        Text.draw(Themes.font(), "Late", 18.0f, left + 5.0f, top + 5.0f, Color.FromArgb(127, Color.White))

        fbo.Unbind()

    override this.Draw() =
        if refresh then this.Redraw()
        Draw.rect Render.bounds Color.White fbo.sprite

    override this.Dispose() =
        base.Dispose()
        fbo.Dispose()