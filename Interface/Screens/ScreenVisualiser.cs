﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using YAVSRG.Charts;
using YAVSRG.Interface.Widgets;
using YAVSRG.Interface.Animations;

namespace YAVSRG.Interface.Screens
{
    class ScreenVisualiser : Screen
    {
        AnimationCounter rotation;
        AnimationSlider hideUI;
        bool hideLogo;
        bool parallax;

        public ScreenVisualiser()
        {
            AddChild(
                new Button("buttonplay", "", () => { Game.Audio.Play(); })
                .PositionTopLeft(250, 10, AnchorType.MAX, AnchorType.MIN)
                .PositionBottomRight(170, 90, AnchorType.MAX, AnchorType.MIN)
                );
            AddChild(
                new Button("buttonpause", "", () => { Game.Audio.Pause(); })
                .PositionTopLeft(170, 10, AnchorType.MAX, AnchorType.MIN)
                .PositionBottomRight(90, 90, AnchorType.MAX, AnchorType.MIN)
                );
            AddChild(
                new Button("buttonstop", "", () => { Game.Audio.Stop(); })
                .PositionTopLeft(90, 10, AnchorType.MAX, AnchorType.MIN)
                .PositionBottomRight(10, 90, AnchorType.MAX, AnchorType.MIN)
                );
            Animation.Add(rotation = new AnimationCounter(31415926, true));
            Animation.Add(hideUI = new AnimationSlider(1f));
        }

        public override void OnEnter(Screen prev)
        {
            base.OnEnter(prev);
            Game.Screens.Logo.Move(new Rect(-400, -400, 400, 400), GetBounds());
            Game.Screens.BackgroundDim.Target = 1;
            Game.Screens.Toolbar.SetState(WidgetState.NORMAL);
            Game.Screens.Parallax.Target *= 4;
            Game.Audio.OnPlaybackFinish = () => { NextTrack(); };
        }

        public override void OnExit(Screen next)
        {
            base.OnExit(next);
            Game.Screens.Logo.Move(new Rect(-ScreenUtils.ScreenWidth -400, -200, -ScreenUtils.ScreenWidth, 200));
            Game.Screens.BackgroundDim.Target = 0.3f;
            Game.Screens.SetParallaxOverride(null);
            Game.Screens.Toolbar.SetState(WidgetState.ACTIVE);
            Game.Screens.Parallax.Target *= 0.25f;
            Game.Screens.Logo.alpha = 1;
        }

        public override void Draw(Rect bounds)
        {
            int alpha = (int)(hideUI * 255);
            base.Draw(bounds);
            float l = 1f + Game.Audio.Level - (0.05f) * Utils.GetBeat(2);
            float r1, r2;
            double a1, a2;
            float rotate = rotation.value * 0.002f;
            for (int i = 0; i < 32; i++) //draws the waveform
            {
                float level = 0;
                for (int t = 0; t < 8; t++)
                {
                    level += Game.Audio.WaveForm[i * 8 + t];
                }
                level *= 0.15f;
                level += 10f;
                r1 = 0; // used in calculations for waveform inwards -- it does nothing rn because i tore it out ungracefully
                r2 = (200 + level) * l; // used in calculations for waveform away diverging from logo
                a1 = rotate + Math.PI / 192 * i;
                for (int p = 0; p < 6; p++)
                {
                    a2 = a1 + Math.PI / 192;
                    a1 = rotate + rotate - a1;
                    a2 = rotate + rotate - a2;
                    SpriteBatch.Draw("", color: Color.FromArgb(100, Game.Screens.HighlightColor), coords: new Vector2[] { new Vector2(r1 * (float)Math.Sin(a1), r1 * (float)Math.Cos(a1)), new Vector2(r2 * (float)Math.Sin(a1), r2 * (float)Math.Cos(a1)), new Vector2(r2 * (float)Math.Sin(a2), r2 * (float)Math.Cos(a2)), new Vector2(r1 * (float)Math.Sin(a2), r1 * (float)Math.Cos(a2)) });
                    a1 = rotate + rotate - a1;
                    a2 = rotate + rotate - a2;
                    SpriteBatch.Draw("", color: Color.FromArgb(100, Game.Screens.HighlightColor), coords: new Vector2[] { new Vector2(r1 * (float)Math.Sin(a1), r1 * (float)Math.Cos(a1)), new Vector2(r2 * (float)Math.Sin(a1), r2 * (float)Math.Cos(a1)), new Vector2(r2 * (float)Math.Sin(a2), r2 * (float)Math.Cos(a2)), new Vector2(r1 * (float)Math.Sin(a2), r1 * (float)Math.Cos(a2)) });
                    a1 += Math.PI / 3;
                }
            }

            SpriteBatch.DrawRect(new Rect(bounds.Left, bounds.Bottom - 30, bounds.Right, bounds.Bottom), Game.Screens.DarkColor);
            SpriteBatch.DrawRect(new Rect(bounds.Left + 5, bounds.Bottom - 25, bounds.Left + 5 + (bounds.Right - 10 - bounds.Left) * Game.Audio.NowPercentage(), bounds.Bottom - 5),  Game.Screens.BaseColor);
            SpriteBatch.Font1.DrawCentredTextToFill(Game.CurrentChart.Data.Artist + " - " + Game.CurrentChart.Data.Title, new Rect(bounds.Left + 500, bounds.Top, bounds.Right - 500, -330), Color.FromArgb(alpha, Game.Options.Theme.MenuFont), true, Game.Screens.DarkColor);
            SpriteBatch.Font1.DrawCentredTextToFill(Utils.FormatTime((float)Game.Audio.Now()), new Rect(bounds.Left, bounds.Bottom - 150, bounds.Left + 200, bounds.Bottom - 75), Color.FromArgb(alpha, Game.Options.Theme.MenuFont), true, Game.Screens.DarkColor);
            SpriteBatch.Font1.DrawCentredTextToFill(Utils.FormatTime((float)Game.Audio.Duration), new Rect(bounds.Right-200, bounds.Bottom - 150, bounds.Right, bounds.Bottom - 75), Color.FromArgb(alpha, Game.Options.Theme.MenuFont), true, Game.Screens.DarkColor);
        }

        public override void Update(Rect bounds)
        {
            base.Update(bounds);
            float f = Utils.GetBeat(1);
            float r = 400 + Utils.GetBeat(1) * 20;
            Game.Screens.Logo.Move(new Rect(-r, -r, r, r));

            if (hideUI.Val < 0.99f)
            {
                if (hideLogo) { Game.Screens.Logo.alpha = hideUI; }
            }

            if (parallax && hideUI.Val >= 0.99f && hideUI.Target == 1)
            {
                parallax = false;
                hideLogo = false;
            }

            if (Input.MouseClick(OpenTK.Input.MouseButton.Left))
            {
                hideUI.Target = 1 - hideUI.Target;
                if (hideUI.Target == 0)
                {
                    if (Input.KeyPress(OpenTK.Input.Key.ShiftLeft))
                    {
                        hideLogo = true;
                    }
                    Game.Screens.SetParallaxOverride(() => new Point((int)(ScreenUtils.ScreenWidth * Math.Sin(rotation.value * 0.002f)), (int)(ScreenUtils.ScreenHeight * Math.Sin(rotation.value * 0.004f))));
                    parallax = true;
                }
                else
                {
                    Game.Screens.SetParallaxOverride(null);
                }
            }

            if (Input.KeyTap(OpenTK.Input.Key.Right))
            {
                NextTrack();
            }
            else if (Input.KeyTap(OpenTK.Input.Key.Left))
            {
                PrevTrack();
            }
        }

        public void NextTrack()
        {
            bool flag = false;
            foreach (ChartLoader.ChartGroup g in ChartLoader.Groups)
            {
                foreach (CachedChart c in g.charts)
                {
                    if (c.title == Game.CurrentChart.Data.Title && c.artist == Game.CurrentChart.Data.Artist && c.diffname == Game.CurrentChart.Data.DiffName)
                    {
                        flag = true;
                    }
                    else if (flag)
                    {
                        ChartLoader.SwitchToChart(c, false);
                        return;
                    }
                }
            }
        }

        public void PrevTrack()
        {
            CachedChart prev = null;
            foreach (ChartLoader.ChartGroup g in ChartLoader.Groups)
            {
                foreach (CachedChart c in g.charts)
                {
                    if (c.title == Game.CurrentChart.Data.Title && c.artist == Game.CurrentChart.Data.Artist && c.diffname == Game.CurrentChart.Data.DiffName)
                    {
                        ChartLoader.SwitchToChart(prev, false);
                        return;
                    }
                    else
                    {
                        prev = c;
                    }
                }
            }
        }
    }
}
