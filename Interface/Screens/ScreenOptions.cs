﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAVSRG.Gameplay;
using System.Drawing;
using YAVSRG.Interface.Widgets;
using YAVSRG.Options.Panels;

namespace YAVSRG.Interface.Screens
{
    class ScreenOptions : Screen
    {
        private LayoutPanel lp;


        public ScreenOptions()
        {
            OnResize();
        }

        public override void OnResize()
        {
            Widgets.Clear();
            var ib = new InfoBox();
            Widget tabs = new ScrollContainer(5f, 5f, false, true);
            lp = new LayoutPanel(ib);
            tabs.AddChild(new GeneralPanel(ib, lp).PositionBottomRight(ScreenUtils.ScreenWidth * 2 - 600, 800, AnchorType.MIN, AnchorType.MIN));
            tabs.AddChild(new GameplayPanel(ib, lp).PositionBottomRight(ScreenUtils.ScreenWidth * 2 - 600, 800, AnchorType.MIN, AnchorType.MIN));
            tabs.AddChild(lp.PositionBottomRight(ScreenUtils.ScreenWidth * 2 - 600, 800, AnchorType.MIN, AnchorType.MIN));
            lp.Refresh();

            AddChild(tabs.PositionTopLeft(0, 0, AnchorType.MIN, AnchorType.MIN).PositionBottomRight(600, 0, AnchorType.MAX, AnchorType.MAX));
            AddChild(ib.PositionTopLeft(550, 50, AnchorType.MAX, AnchorType.MIN).PositionBottomRight(50, 50, AnchorType.MAX, AnchorType.MAX));
        }

        public override void OnExit(Screen next)
        {
            base.OnExit(next);
            Game.Gameplay.UpdateChart(); //recolor notes based on settings if they've changed
        }
    }
}
