﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static YAVSRG.Interface.ScreenUtils;
using YAVSRG.Charts;
using System.Drawing;
using YAVSRG.Interface.Widgets;

namespace YAVSRG.Interface.Screens
{
    public class ScreenLevelSelect : Screen
    {
        private ChartInfoPanel diffDisplay;
        private LevelSelector selector;

        public ScreenLevelSelect()
        {
            selector = new LevelSelector(this);
            AddChild(selector.PositionTopLeft(0, 120, AnchorType.CENTER, AnchorType.MIN).PositionBottomRight(0, 0, AnchorType.MAX, AnchorType.MAX));
            AddChild(new ChartSortingControls().PositionTopLeft(0, 0, AnchorType.MIN, AnchorType.MIN).PositionBottomRight(0, 120, AnchorType.MAX, AnchorType.MIN));
            diffDisplay = new ChartInfoPanel();
            AddChild(diffDisplay.PositionTopLeft(0, 0, AnchorType.MIN, AnchorType.MIN).PositionBottomRight(700, 0, AnchorType.MAX, AnchorType.MAX));

            ModMenu modMenu = new ModMenu();
            AddChild(modMenu.PositionTopLeft(0, 150, AnchorType.MIN, AnchorType.MIN).PositionBottomRight(720, 100, AnchorType.MAX, AnchorType.MAX));

            AddChild(new FramedButton("buttonbase", "Play", () => { Game.Screens.AddScreen(new ScreenPlay()); })
                .PositionTopLeft(-350, 75, AnchorType.CENTER, AnchorType.MAX)
                .PositionBottomRight(750, 25, AnchorType.MAX, AnchorType.MAX));
            AddChild(new FramedButton("buttonbase", "Mods", () => { modMenu.Toggle(); })
                .PositionTopLeft(50, 75, AnchorType.MIN, AnchorType.MAX)
                .PositionBottomRight(-400, 25, AnchorType.CENTER, AnchorType.MAX));

            PositionTopLeft(-ScreenWidth, 0, AnchorType.MIN, AnchorType.MIN);
            PositionBottomRight(-ScreenWidth, 0, AnchorType.MAX, AnchorType.MAX);
            Animation.Add(new Animation()); //dummy animation ensures "expansion" effect happens during screen transitions
        }

        private void OnUpdateGroups()
        {
            selector.Refresh();
            selector.ScrollToSelected();
        }

        private void OnUpdateChart()
        {
            diffDisplay.ChangeChart();
            Game.Audio.SetRate(Game.Options.Profile.Rate);
        }

        public override void OnEnter(Screen prev)
        {
            base.OnEnter(prev);
            A.Target(0, 0);
            B.Target(0, 0);
            Game.Gameplay.OnUpdateChart += OnUpdateChart;
            Game.Audio.OnPlaybackFinish = () => { Game.Audio.Stop(); Game.Audio.Play((long)Game.CurrentChart.Data.PreviewTime); };
            OnUpdateChart();
            selector.ScrollToSelected();
            ChartLoader.OnRefreshGroups += OnUpdateGroups;
        }

        public override void OnExit(Screen next)
        {
            base.OnExit(next);
            Input.ChangeIM(null);
            Game.Gameplay.OnUpdateChart -= OnUpdateChart;
            ChartLoader.OnRefreshGroups -= OnUpdateGroups;
            A.Target(-ScreenWidth, 0);
            B.Target(-ScreenWidth, 0);
        }

        public override void Update(float left, float top, float right, float bottom)
        {
            base.Update(left, top, right, bottom);

            double ratestep = Input.KeyPress(OpenTK.Input.Key.ControlLeft) ? 0.2d : Input.KeyPress(OpenTK.Input.Key.ShiftLeft) ? 0.01d : 0.05d;
            if (Input.KeyTap(Game.Options.General.Binds.UpRate))
            {
                ChangeRate(ratestep);
            }
            else if (Input.KeyTap(Game.Options.General.Binds.DownRate))
            {
                ChangeRate(-ratestep);
            }
            else if (Input.KeyPress(OpenTK.Input.Key.ControlLeft) && Input.KeyTap(OpenTK.Input.Key.R)) //temp
            {
                ChartLoader.RecacheThreaded();
                Game.Screens.AddDialog(new Dialogs.LoadingDialog((s) => { ChartLoader.Refresh(); }));
            }
            else if (Input.KeyTap(OpenTK.Input.Key.F)) //also temp
            {
                Game.Gameplay.Collections.GetCollection("Favourites").AddItem(Game.Gameplay.CurrentCachedChart);
            }
        }

        public void ChangeRate(double change)
        {
            Game.Options.Profile.Rate += change;
            Game.Options.Profile.Rate = Math.Round(Game.Options.Profile.Rate, 2, MidpointRounding.AwayFromZero);
            Game.Options.Profile.Rate = Math.Max(0.5, Math.Min(Game.Options.Profile.Rate,3.0));
            Game.Gameplay.UpdateDifficulty();
            OnUpdateChart();
        }
    }
}
