using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAVSRG.Interface.Widgets;
using System.Diagnostics;
using ManagedBass;
using YAVSRG.Charts;
using YAVSRG.Interface.Screens;
using YAVSRG.Interface;

namespace YAVSRG.Interface.Screens
{
    class ScreenGoals : Screen
    {
        public ScreenGoals() : base()
        {
            // as of now this does absolutely nothing but exist -- i'm going to familiarise myself with the widgets and then attempt to create a calenderised goal system.
        }

        public override void Draw(Rect bounds)
        {
            base.Draw(bounds);
            bounds = GetBounds(bounds);
            SpriteBatch.Font1.DrawCentredTextToFill("Goals (will) go here", new Rect(bounds.Left, bounds.Top + 100, bounds.Right, bounds.Top + 200), Game.Options.Theme.MenuFont);
        }
    }
}