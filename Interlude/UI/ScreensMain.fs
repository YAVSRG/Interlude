﻿namespace Interlude.UI

open OpenTK
open Interlude
open Interlude.Render
open Interlude.UI.Animation
open Interlude.UI.Components
open Interlude.Utils
open Interlude.Input
open Interlude.Options

// Menu screen

type ScreenMenu() =
    inherit Screen()

    override this.OnEnter(prev: Screen) =
        Screens.logo.Move(-Render.vwidth * 0.5f, -400.0f, 800.0f - Render.vwidth * 0.5f, 400.0f)
        Screens.backgroundDim.SetTarget(0.0f)
        Screens.setToolbarCollapsed(false)

    override this.OnExit(next: Screen) =
        Screens.logo.Move(-Render.vwidth * 0.5f - 600.0f, -300.0f, -Render.vwidth * 0.5f, 300.0f)
        Screens.backgroundDim.SetTarget(0.7f)

    override this.Draw() =
        let (x, y) = Rect.center this.Bounds
        base.Draw()
        //Text.drawJust(Themes.font(), Audio.timeWithOffset().ToString(), 50.f, x, y, Color.White, 0.5f)

    override this.Update(time, bounds) =
        base.Update(time, bounds)
        if (Options.options.Hotkeys.Select.Get().Tapped(false)) then
            Screens.addScreen (new ScreenLevelSelect())

// Loading screen

type ScreenLoading() =
    inherit Screen()

    let mutable closing = false

    override this.OnEnter(prev: Screen) =
        Screens.logo.Move(-400.0f, -400.0f, 400.0f, 400.0f)
        Screens.setToolbarCollapsed(true)
        match prev with
        | :? ScreenMenu ->
            closing <- true
            let s = AnimationSequence()
            s.Add(AnimationTimer(1500.0))
            s.Add(AnimationAction(fun () -> Screens.popScreen()))
            this.Animation.Add(s)
        | _ -> 
            let s = AnimationSequence()
            s.Add(AnimationTimer(1500.0))
            s.Add(AnimationAction(fun () -> Screens.addScreen (new ScreenMenu())))
            this.Animation.Add(s)
        
    override this.Draw() =
        let (x, y) = Rect.center this.Bounds
        Text.drawJust(Themes.font(), (if closing then "Bye o/" else "Loading :)"), 80.f, x, y - 500.0f, Color.White, 0.5f)

// Toolbar

type Toolbar() as this =
    inherit Widget()

    static let height = 70.f

    let barSlider = new AnimationFade(0.0f)
    let notifSlider = new AnimationFade(0.0f)

    let mutable userCollapse = false
    let mutable forceCollapse = true
    
    do
        this.Animation.Add(barSlider)
        this.Animation.Add(notifSlider)
        this.Add(new TextBox(K version, K Color.White, 1.0f) |> positionWidget(-200.f, 1.f, 0.f, 1.f, 0.f, 1.f, height * 0.5f, 1.f))
        this.Add(new TextBox((fun () -> System.DateTime.Now.ToString()), K Color.White, 1.0f) |> positionWidget(-200.f, 1.f, height * 0.5f, 1.f, 0.f, 1.f, height, 1.f))
        this.Add(new Button((fun () -> Screens.popScreen()), "Back", Options.options.Hotkeys.Exit, Sprite.Default) |> positionWidget(0.0f, 0.0f, 0.0f, 1.0f, 200.f, 0.0f, height, 1.0f))
        this.Add(new Button((fun () -> new ScreenOptions() |> Screens.addScreen), "Options", Options.options.Hotkeys.Options, Sprite.Default) |> positionWidget(0.0f, 0.0f, -height, 0.0f, 200.f, 0.0f, 0.0f, 0.0f))
        this.Add(new Button(ignore, "Import", Options.options.Hotkeys.Import, Sprite.Default) |> positionWidget(200.0f, 0.0f, -height, 0.0f, 400.f, 0.0f, 0.0f, 0.0f))
        this.Add(new Button(ignore, "Help", Options.options.Hotkeys.Help, Sprite.Default) |> positionWidget(400.0f, 0.0f, -height, 0.0f, 600.f, 0.0f, 0.0f, 0.0f))

        Screens.setToolbarCollapsed <- (fun b -> forceCollapse <- b; barSlider.SetTarget(if userCollapse || forceCollapse then 0.0f else 1.0f))

    override this.Draw() = 
        let struct (l, t, r, b) = this.Bounds
        Draw.rect(Rect.create l (t - height) r t) (Screens.accentShade(127, 1.0f, 0.0f)) Sprite.Default
        Draw.rect(Rect.create l b r (b + height)) (Screens.accentShade(127, 1.0f, 0.0f)) Sprite.Default
        base.Draw()

    override this.Update(elapsed, bounds) =
        if (not forceCollapse) && Options.options.Hotkeys.Toolbar.Get().Tapped(false) then
            userCollapse <- not userCollapse
            barSlider.SetTarget(if userCollapse || forceCollapse then 0.0f else 1.0f)
        base.Update(elapsed, Rect.expand (0.f, -height * barSlider.Value) bounds)

//Screen manager

type ScreenContainer() as this =
    inherit Widget()

    let mutable dialogs = []
    let mutable previous = None
    let mutable current = new ScreenLoading() :> Screen
    let mutable screens = [current]
    let mutable exit = false

    let toolbar = new Toolbar()

    let parallaxX = new AnimationFade(0.0f)
    let parallaxY = new AnimationFade(0.0f)

    do
        Screens.addScreen <- this.AddScreen
        Screens.popScreen <- this.RemoveScreen
        this.Add(toolbar)
        this.Add(Screens.logo |> Components.positionWidget(-300.0f, 0.5f, 1000.0f, 0.5f, 300.0f, 0.5f, 1600.0f, 0.5f))
        this.Animation.Add(Screens.accentColor)
        this.Animation.Add(Screens.parallaxZ)
        this.Animation.Add(Screens.parallaxX)
        this.Animation.Add(Screens.parallaxY)
        this.Animation.Add(Screens.backgroundDim)
        current.OnEnter(current)

    member this.Exit = exit

    member this.AddScreen(s: Screen) =
        screens <- s :: screens
        current.OnExit(s)
        s.OnEnter(current)
        previous <- Some current
        current <- s

    member this.RemoveScreen() =
        previous <- Some current
        screens <- List.tail screens
        match List.tryHead screens with
        | None -> exit <- true
        | Some s ->
            current.OnExit(s)
            s.OnEnter(current)
            current <- s

    override this.Update(elapsedTime, bounds) =
        Screens.parallaxX.SetTarget(Mouse.X())
        Screens.parallaxY.SetTarget(Mouse.Y())
        Screens.accentColor.SetColor(Themes.accentColor)
        base.Update(elapsedTime, bounds)
        current.Update(elapsedTime, toolbar.Bounds)

    override this.Draw() =
        Screens.drawBackground(this.Bounds, Color.White, 1.0f)
        Draw.rect this.Bounds (Color.FromArgb(Screens.backgroundDim.Value * 255.0f |> int, 0, 0, 0)) Sprite.Default
        current.Draw()
        base.Draw()