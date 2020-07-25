﻿namespace Interlude

open System
open System.Collections.Generic
open Prelude.Common
open Prelude.Charts.Interlude
open Prelude.Editor
open Prelude.Gameplay.Mods
open Prelude.Gameplay.Score
open Prelude.Gameplay.Difficulty
open Prelude.Gameplay.NoteColors
open Prelude.Data.ChartManager
open Prelude.Data.ScoreManager
open Interlude
open Interlude.Utils
open Interlude.Options

module Gameplay =
    
    let mutable internal currentChart: Chart option = None
    let mutable internal currentCachedChart: CachedChart option = None
    let mutable internal chartSaveData = None
    let mutable modifiedChart: Lazy<ModChart> = lazy ( failwith "tried to access modified chart when none is selected" )
    let mutable coloredChart: Lazy<ColorizedChart> = lazy ( failwith "tried to access colored chart when none is selected" )
    let mutable private replayData: Lazy<ScoreData> = lazy ( null )
    let mutable difficultyRating = None

    let mutable rate = 1.0f
    let selectedMods = ModState()
    let scores = ScoresDB()
    let cache = Cache()

    let mutable onChartUpdate = fun () -> ()
    let mutable onChartChange = fun () -> ()

    let updateChart() =
        match currentChart with
        | None -> ()
        | Some c ->
            let (m, r) = getModChart selectedMods c
            modifiedChart <- m
            coloredChart <- getColoredChart(Options.profile.ColorStyle.Get())(modifiedChart)
            replayData <- r
            difficultyRating <-
                let (keys, notes, _, _, _) = m.Force() in
                Some <| RatingReport(notes, rate, Options.profile.Playstyles.[keys - 3], keys)
            onChartUpdate()

    let changeRate(amount) =
        rate <- Math.Round(float (rate + amount), 2) |> float32
        Audio.changeRate(rate)
        updateChart()

    let changeChart(cachedChart, chart) =
        currentCachedChart <- Some cachedChart
        currentChart <- Some chart
        chartSaveData <- Some <| scores.GetScoreData(chart)
        Themes.loadBackground(chart.BGPath)
        let localOffset = if chart.Notes.IsEmpty() then 0.0f<ms> else chartSaveData.Value.Offset.Get() - (offsetOf <| chart.Notes.First())
        Audio.changeTrack(chart.AudioPath, localOffset, rate)
        Audio.playFrom(chart.Header.PreviewTime)
        Options.options.CurrentChart.Set(cachedChart.FilePath)
        updateChart()
        onChartChange()

    let getModString() = 
        String.Join(", ", sprintf "%.2fx" rate :: (selectedMods.Enumerate() |> List.map (ModState.GetModName)))

    let createScoreData() = 
        let r = replayData.Force()
        replayData <- getScoreData(selectedMods)(modifiedChart.Force())
        r

    let makeScore(scoreData, keys) : Score =
        {   
            time = DateTime.Now
            hitdata = compressScoreData scoreData
            player = Options.profile.Name.Get()
            playerUUID = Options.profile.UUID
            rate = rate
            selectedMods = selectedMods.Data
            layout = Options.profile.Playstyles.[keys - 2]
            keycount = keys } : Score
        

    let setScore(data: ScoreInfoProvider) =
        let d = chartSaveData.Value
        if
            match Options.profile.ScoreSaveCondition.Get() with
            | _ -> true
            //todo: fill in this stub (pb condition will be complicated)
        then
            //add to score db
            d.Scores.Add(data.Score)
            scores.Save()
            //update top scores
            Options.profile.Stats.TopPhysical.Set(TopScore.add(currentCachedChart.Value.Hash, data.Score.time, data.Physical)(Options.profile.Stats.TopPhysical.Get()))
            Options.profile.Stats.TopTechnical.Set(TopScore.add(currentCachedChart.Value.Hash, data.Score.time, data.Technical)(Options.profile.Stats.TopTechnical.Get()))
            //update pbs
            let f name (target: Dictionary<string, PersonalBests<'T>>) (value: 'T) = 
                if target.ContainsKey(name) then
                    let n, pb = updatePB(target.[name])(value, data.Score.rate)
                    target.[name] <- n
                    pb
                else
                    target.Add(name, ((value, data.Score.rate), (value, data.Score.rate)))
                    PersonalBestType.Faster
            f data.Accuracy.Name d.Lamp data.Lamp,
            f data.Accuracy.Name d.Accuracy data.Accuracy.Value,
            f (data.Accuracy.Name + "|" + data.HP.Name) d.Clear (not data.HP.Failed)
        else
            (PersonalBestType.None, PersonalBestType.None, PersonalBestType.None)

    let save() =
        scores.Save()
        cache.Save()
        
    let init() =
        //cache.RebuildCache (fun x -> Logging.Info(x) "") |> ignore
        //cache.ConvertPackFolder(osuSongFolder) "osu!" (fun x -> Logging.Info(x) "") |> ignore
        //Options.profile.ColorStyle.Set({ Options.profile.ColorStyle.Get() with Style = ColorScheme.DDR })
       
        let c, ch = 
            match cache.LookupChart(Options.options.CurrentChart.Get()) with
            | Some cc ->
                match cache.LoadChart(cc) with
                | Some c -> cc, c
                | None ->
                    Logging.Error("Could not load chart file: " + cc.FilePath) ""
                    cache.GetGroups(K "All") (Comparison(fun _ _ -> 0)) ""
                    |> fun d -> d.["All"].[0]
                    |> fun c -> c, cache.LoadChart(c).Value
            | None ->
                Logging.Info("Could not find cached chart: " + Options.options.CurrentChart.Get()) ""
                cache.GetGroups(K "All") (Comparison(fun _ _ -> 0)) ""
                |> fun d -> d.["All"].[0]
                |> fun c -> c, cache.LoadChart(c).Value
        changeChart(c, ch)
        //temp while audio code isnt finished (this will automatically happen in future)
        Audio.playFrom(ch.Header.PreviewTime)