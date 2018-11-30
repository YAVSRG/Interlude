﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAVSRG.Net.Web;

namespace YAVSRG.Interface.Widgets
{
    class DownloadCard : Widget
    {
        public string name;
        string size;
        string difficulty;

        public DownloadCard(EtternaPackData.EtternaPack data)
        {
            name = data.attributes.name;
            difficulty = Utils.RoundNumber(data.attributes.average / 2.5) + "*";
            size = Utils.RoundNumber(data.attributes.size / 1000000) + "MB";
            if (data.attributes.download != "")
            {
                AddChild(new SimpleButton("Download",
                    () => Game.Tasks.AddTask(Charts.ChartLoader.DownloadAndImportPack(data.attributes.download, data.attributes.name, ".zip"), (b) => { }, "Downloading pack: " + data.attributes.name, true), () => false, 20f)
                    .PositionTopLeft(100, 0, AnchorType.MAX, AnchorType.MIN).PositionBottomRight(10, 0, AnchorType.MAX, AnchorType.MAX));
            }
        }

        public DownloadCard(BloodcatChartData data)
        {
            name = data.title;
            difficulty = data.creator;
            size = "";
            AddChild(new SimpleButton("Download",
                () => Game.Tasks.AddTask(Charts.ChartLoader.DownloadAndImportPack("https://osu.ppy.sh/beatmapsets/"+data.id.ToString()+"/download?noVideo=1", data.id.ToString(), ".osz"), (b) => { }, "Downloading beatmap: " + data.title, true), () => false, 20f)
                .PositionTopLeft(100, 0, AnchorType.MAX, AnchorType.MIN).PositionBottomRight(10, 0, AnchorType.MAX, AnchorType.MAX));
        }

        public override void Draw(Rect bounds)
        {
            base.Draw(bounds);
            bounds = GetBounds(bounds);
            float w = bounds.Width - 100;
            SpriteBatch.DrawRect(bounds, System.Drawing.Color.FromArgb(127, 0, 0, 0));
            //todo: move to textbox widgets
            SpriteBatch.Font1.DrawTextToFill(name, new Rect(bounds.Left, bounds.Top, bounds.Left + w * 0.7f, bounds.Bottom), Game.Options.Theme.MenuFont);
            SpriteBatch.Font1.DrawJustifiedTextToFill(size, new Rect(bounds.Left + w * 0.75f, bounds.Top, bounds.Left + w * 0.9f, bounds.Bottom), Game.Options.Theme.MenuFont);
            SpriteBatch.Font1.DrawJustifiedTextToFill(difficulty, new Rect(bounds.Left + w * 0.9f, bounds.Top, bounds.Left + w, bounds.Bottom), Game.Options.Theme.MenuFont);
            ScreenUtils.DrawFrame(bounds, 30f, System.Drawing.Color.White);
        }
    }
}