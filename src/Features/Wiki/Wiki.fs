﻿namespace Interlude.Features.Wiki

open Percyqaz.Common
open Percyqaz.Json
open Percyqaz.Flux.Input
open Percyqaz.Flux.UI
open Percyqaz.Flux.Graphics
open Prelude.Data
open Interlude.Utils
open Interlude.UI
open Interlude.UI.Components
open FSharp.Formatting.Markdown

module Wiki =

    [<Json.AutoCodec>]
    type WikiPage =
        {
            Folder: string
            Title: string
            Filename: string
        }

    type Resource =
        | WikiIndex
        | WikiPage of WikiPage
        | Changelog

    module Cache =

        let mutable changelog_content: MarkdownDocument array option = None
        let mutable index_content: MarkdownDocument array option = None
        let mutable index_table_of_contents: Map<string, WikiPage list> = Map.empty

        let mutable page_cache_by_filename: Map<string, MarkdownDocument array option> =
            Map.empty

        let mutable pages_by_filename: Map<string, WikiPage> = Map.empty

    let mutable private loading = false
    let mutable private page_history = [ WikiIndex ]
    let mutable private current_page = WikiIndex
    let mutable private content: MarkdownDocument array option = None
    let mutable private page_changed: unit -> unit = ignore

    let private page_loader =
        { new Async.Service<Resource, MarkdownDocument array>() with
            override this.Handle(req) =
                async {
                    match req with
                    | WikiIndex ->
                        if Cache.index_content.IsNone then
                            match!
                                WebServices.download_string.RequestAsync(
                                    "https://raw.githubusercontent.com/YAVSRG/Interlude/main/docs/wiki/index.md"
                                )
                            with
                            | Some md ->
                                match!
                                    WebServices.download_json_async (
                                        "https://raw.githubusercontent.com/YAVSRG/Interlude/main/docs/wiki/index.json"
                                    )
                                with
                                | Some toc ->
                                    Cache.index_table_of_contents <- toc

                                    Cache.page_cache_by_filename <-
                                        toc
                                        |> Map.toSeq
                                        |> Seq.map snd
                                        |> Seq.concat
                                        |> Seq.map (fun p -> p.Filename, None)
                                        |> Map.ofSeq

                                    Cache.pages_by_filename <-
                                        toc
                                        |> Map.toSeq
                                        |> Seq.map snd
                                        |> Seq.concat
                                        |> Seq.map (fun p -> p.Filename, p)
                                        |> Map.ofSeq

                                    Cache.index_content <- Some [| Markdown.Parse md |]
                                | None -> ()
                            | None -> ()

                        return Cache.index_content |> Option.defaultValue [||]

                    | WikiPage p ->
                        if Cache.page_cache_by_filename.[p.Filename].IsNone then
                            match!
                                WebServices.download_string.RequestAsync(
                                    "https://raw.githubusercontent.com/YAVSRG/Interlude/main/docs/wiki/"
                                    + p.Filename
                                    + ".md"
                                )
                            with
                            | Some md ->
                                let sections =
                                    md
                                        .Split("---", 3, System.StringSplitOptions.TrimEntries)
                                        .[2].Split("::::", System.StringSplitOptions.TrimEntries)

                                Cache.page_cache_by_filename <-
                                    Cache.page_cache_by_filename.Add(
                                        p.Filename,
                                        Some(sections |> Array.map Markdown.Parse)
                                    )
                            | None -> ()

                        return Cache.page_cache_by_filename.[p.Filename] |> Option.defaultValue [||]

                    | Changelog ->
                        if Cache.changelog_content.IsNone then
                            match!
                                WebServices.download_string.RequestAsync(
                                    "https://raw.githubusercontent.com/YAVSRG/Interlude/main/docs/changelog.md"
                                )
                            with
                            | Some md -> Cache.changelog_content <- Some [| Markdown.Parse md |]
                            | None -> ()

                        return Cache.changelog_content |> Option.defaultValue [||]
                }

        }

    // todo: reimplement links to specific headers
    //if page.Contains('#') then
    //Heading.scrollTo <- page.Substring(page.LastIndexOf '#' + 1).Replace('-', ' ')
    //page.Substring(0, page.LastIndexOf '#')

    let load_resource (res) =
        if not loading then
            loading <- true
            page_history <- res :: (List.except [ res ] page_history)
            current_page <- res

            page_loader.Request(
                res,
                fun md ->
                    content <- Some md
                    loading <- false
                    sync page_changed
            )

    do
        LinkHandler.add
            {
                Condition =
                    fun url ->
                        Cache.pages_by_filename.ContainsKey(
                            url.Replace(".html", "", System.StringComparison.InvariantCultureIgnoreCase)
                        )
                Action =
                    fun url ->
                        Cache.pages_by_filename.[url.Replace(
                            ".html",
                            "",
                            System.StringComparison.InvariantCultureIgnoreCase
                        )]
                        |> WikiPage
                        |> load_resource
            }

    type Browser() as this =
        inherit Dialog()

        let mutable flow = Unchecked.defaultof<ScrollContainer>

        let buttons =
            NavigationContainer.Row<Widget>(
                Position = Position.SliceTop(70.0f).Margin((Viewport.vwidth - 1400.0f) * 0.5f, 10.0f)
            )
            |+ IconButton(
                %"menu.back"
                , Icons.ARROW_LEFT_CIRCLE
                , 50.0f
                , fun () ->
                    match page_history with
                    | x :: y :: xs ->
                        load_resource y
                        page_history <- y :: xs
                    | _ -> this.Close()
                , Disabled = (fun () -> page_history.Length < 2)
                , Position = Position.SliceLeft(150.0f)
            )
            |+ Text(
                (fun () ->
                    match current_page with
                    | Changelog -> Icons.EDIT_2 + " Changelog"
                    | WikiPage p -> sprintf "%s %s  >  %s" Icons.BOOK_OPEN p.Folder p.Title
                    | WikiIndex -> sprintf "%s Home" Icons.BOOK
                ),
                Align = Alignment.LEFT,
                Position = Position.Column(200.0f, 900.0f)
            )
            |+ IconButton(
                %"wiki.openinbrowser"
                , Icons.EXTERNAL_LINK
                , 50.0f
                , fun () ->
                    match current_page with
                    | WikiIndex -> open_url ("https://yavsrg.net/interlude/wiki/index.html")
                    | WikiPage p -> open_url ("https://yavsrg.net/interlude/wiki/" + p.Filename + ".html")
                    | Changelog -> open_url ("https://yavsrg.net/interlude/changelog.html")
                , Position = Position.SliceRight(300.0f)
            )

        do Heading.scroll_handler <- fun w -> flow.Scroll(w.Bounds.Top - flow.Bounds.Top)

        member private this.UpdateContent() =
            let con = StaticContainer(NodeType.None)
            let mutable y = 0.0f
            let max_width = 1400.0f
            let spacing = 35.0f

            match content with
            | Some paragraphs ->
                for paragraph in paragraphs do
                    let markdown = MarkdownUI.build max_width paragraph
                    markdown.Position <- Position.Box(0.0f, 0.0f, 0.0f, y, max_width, markdown.Height)
                    con.Add(markdown)
                    y <- y + markdown.Height + spacing

                if current_page = WikiIndex then
                    let fcount = float32 Cache.index_table_of_contents.Keys.Count

                    let folders =
                        FlowContainer.LeftToRight<_>(
                            (max_width - 10.0f - (fcount - 1.0f) * 20.0f) / fcount,
                            Spacing = 20.0f,
                            Position = Position.TrimTop(80.0f).Margin(5.0f, 0.0f)
                        )

                    for key in Cache.index_table_of_contents.Keys do
                        let pages = Cache.index_table_of_contents.[key]

                        let links = FlowContainer.Vertical(50.0f, Position = Position.TrimTop(60.0f))

                        for p in pages do
                            links.Add(Button(p.Title, (fun () -> load_resource (WikiPage p))))

                        folders.Add(
                            Frame(
                                NodeType.Switch(fun () -> links),
                                Fill = K Colors.cyan.O2,
                                Border = K Colors.cyan_accent
                            )
                            |+ Text(key, Position = Position.SliceTop(60.0f), Color = K Colors.text_subheading)
                            |+ links
                        )

                    con.Add(
                        StaticContainer(
                            NodeType.Switch(fun () -> folders),
                            Position = Position.Box(0.0f, 0.0f, 0.0f, y, max_width, 400.0f)
                        )
                        |+ Text(
                            Icons.BOOK_OPEN + " Table of contents",
                            Position = Position.SliceTop(70.0f).Margin(20.0f, 0.0f),
                            Align = Alignment.LEFT
                        )
                        |+ folders
                    )

                    y <- y + 400.0f + spacing
            | None -> con.Add(LoadingState())

            flow <-
                ScrollContainer(con, y - spacing, Position = Position.Margin((Viewport.vwidth - 1400.0f) * 0.5f, 80.0f))

            flow.Init this

        override this.Init(parent: Widget) =
            base.Init parent
            buttons.Init this
            this.UpdateContent()

            if content.IsNone && current_page = WikiIndex then
                load_resource WikiIndex

            page_changed <- this.UpdateContent

        override this.Update(elapsed_ms, moved) =
            base.Update(elapsed_ms, moved)
            buttons.Update(elapsed_ms, moved)
            flow.Update(elapsed_ms, moved)

            if Mouse.left_click () || (%%"exit").Tapped() then
                this.Close()

        override this.Draw() =
            buttons.Draw()
            flow.Draw()

    let show () = (Browser()).Show()

    let show_changelog () =
        if current_page <> Changelog then
            load_resource Changelog

        (Browser()).Show()
