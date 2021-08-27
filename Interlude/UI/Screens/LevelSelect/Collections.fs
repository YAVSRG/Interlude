﻿namespace Interlude.UI.Screens.LevelSelect

open System.Drawing
open Prelude.Common
open Prelude.Data.ChartManager
open Interlude.Utils
open Interlude.UI
open Interlude.UI.Components
open Interlude.UI.Selection
open Interlude.Gameplay
open Interlude.Options
open Globals

module Collections =

    let mutable selected =
        //todo: load from settings
        let favourites = Localisation.localise "collections.Favourites"
        let c = 
            match cache.GetCollection favourites with
            | Some c -> c
            | None ->
                let n = Collection.Blank
                cache.UpdateCollection (favourites, n)
                n
        favourites, c

    let private editCollection (setting: Setting<string * Collection>) =
        let name =
            setting.Value
            |> fst
            |> Setting.simple
        {
            Content = fun add ->
                column [
                    PrettySetting("CollectionName", TextField name).Position(200.0f)
                ] :> Selectable
            Callback = fun () -> setting.Value <- (name.Value, snd setting.Value)
        }

    let page() =
        {
            Content = fun add ->
                let setting =
                    Setting.make
                        ignore
                        (fun () -> cache.GetCollections() |> Seq.map (fun n -> (n, cache.GetCollection(n).Value)) |> List.ofSeq)
                column
                    [
                        PrettySetting("Collections",
                            CardSelect.Selector(
                                setting,
                                { 
                                    NameFunc = fst
                                    MarkFunc = (fun (x, _) -> colorVersionGlobal <- colorVersionGlobal + 1; selected <- x)
                                    EditFunc = Some editCollection
                                    CreateFunc = Some (fun () -> "New collection", Collection (ResizeArray<string>()))

                                    CanReorder = false
                                    CanDuplicate = false
                                    CanDelete = true
                                    CanMultiSelect = false
                                }, add)).Position(200.0f, 1200.0f, 600.0f)
                        TextBox(K <| Localisation.localiseWith [options.Hotkeys.AddToCollection.Value.ToString()] "collections.AddHint", K (Color.White, Color.Black), 0.5f)
                        |> positionWidget(0.0f, 0.0f, -190.0f, 1.0f, 0.0f, 1.0f, -120.0f, 1.0f)
                        TextBox(K <| Localisation.localiseWith [options.Hotkeys.RemoveFromCollection.Value.ToString()] "collections.RemoveHint", K (Color.White, Color.Black), 0.5f)
                        |> positionWidget(0.0f, 0.0f, -120.0f, 1.0f, 0.0f, 1.0f, -50.0f, 1.0f)
                    ] :> Selectable
            Callback = ignore
        }
    
type CollectionManager() =
    inherit Widget()
    
    override this.Update(elapsedTime, bounds) =
        base.Update(elapsedTime, bounds)
        if options.Hotkeys.Collections.Value.Tapped() then
            Globals.addDialog <| SelectionMenu(Collections.page())
        if currentCachedChart.IsSome then
            if options.Hotkeys.AddToCollection.Value.Tapped() then
                if
                    match snd Collections.selected with
                    | Collection ccs -> if ccs.Contains selectedChart then false else ccs.Add selectedChart; true
                    | Playlist ps -> ps.Add (selectedChart, selectedMods, rate); true
                    | Goals gs -> false //gs.Add ((selectedChart, selectedMods, rate), Goal.NoGoal); true
                then
                    colorVersionGlobal <- colorVersionGlobal + 1
                    Globals.addNotification(Localisation.localiseWith [currentCachedChart.Value.Title; fst Collections.selected] "collections.Added", NotificationType.Info)
            elif options.Hotkeys.RemoveFromCollection.Value.Tapped() then
                if
                    match snd Collections.selected with
                    | Collection ccs -> ccs.Remove selectedChart
                    | Playlist ps -> ps.RemoveAll(fun (id, _, _) -> id = selectedChart) > 0
                    | Goals gs -> gs.RemoveAll(fun ((id, _, _), _) -> id = selectedChart) > 0
                then
                    colorVersionGlobal <- colorVersionGlobal + 1
                    Globals.addNotification(Localisation.localiseWith [currentCachedChart.Value.Title; fst Collections.selected] "collections.Removed", NotificationType.Info)