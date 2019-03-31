﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interlude.Interface.Widgets;

namespace Interlude.Interface.Dialogs
{
    public class TopScoreDialog : FadeDialog
    {
        bool Technical;
        TopScoreDisplay Display;

        public TopScoreDialog(bool tech) : base((s) => { })
        {
            Technical = tech;
            AddChild(Display = new TopScoreDisplay(Technical));
            AddChild(new TextPicker("Keymode", new[] { "3K", "4K", "5K", "6K", "7K", "8K", "9K", "10K" }, (int)Game.Options.Profile.DefaultKeymode, (i) => { Display.Refresh(i); }).PositionTopLeft(210,60,AnchorType.MAX,AnchorType.MAX).PositionBottomRight(10, 10, AnchorType.MAX, AnchorType.MAX));
            PositionTopLeft(100, 100, AnchorType.MIN, AnchorType.MIN).PositionBottomRight(100, 100, AnchorType.MAX, AnchorType.MAX);
            Display.Refresh((int)Game.Options.Profile.DefaultKeymode);
        }
    }
}