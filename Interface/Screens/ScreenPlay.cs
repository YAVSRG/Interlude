﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using static YAVSRG.Interface.ScreenUtils;
using YAVSRG.Charts.YAVSRG;
using YAVSRG.Gameplay;
using YAVSRG.Interface.Widgets.Gameplay;
using YAVSRG.Interface.Animations;
using OpenTK.Input;

namespace YAVSRG.Interface.Screens
{
    class ScreenPlay : Screen //i cleaned up this file a little but it's a bit of a mess. sorry!
    {
        
        int lasti; int lastt; //the number of snaps and number of timing points respectively. used to be an optimisation and now they're just a convenience
        ChartWithModifiers Chart;
        ScoreTracker scoreTracker;
        Widget playfield;
        float missWindow;
        Key[] binds;
        HitLighting[] lighting;
        AnimationFade bannerIn, bannerOut;

        public ScreenPlay()
        {
            Chart = Game.Gameplay.ModifiedChart;
            scoreTracker = new ScoreTracker(Game.Gameplay.ModifiedChart);

            int columnwidth = Game.Options.Theme.ColumnWidth;
            int hitposition = Game.Options.Profile.HitPosition;

            //i make all this stuff ahead of time as a small optimisation
            lasti = Chart.Notes.Count;
            lastt = Chart.Timing.Count;
            missWindow = scoreTracker.Scoring.MissWindow * (float)Game.Options.Profile.Rate;
            binds = Game.Options.Profile.Bindings[Chart.Keys];

            var widgetData = Game.Options.Theme.Gameplay;

            //this stuff is ok to stay here
            AddChild(playfield = new Playfield(scoreTracker).PositionTopLeft(-columnwidth * Chart.Keys * 0.5f, 0, AnchorType.CENTER, AnchorType.MIN).PositionBottomRight(columnwidth * Chart.Keys * 0.5f, 0, AnchorType.CENTER, AnchorType.MAX));
            AddChild(new HitMeter(scoreTracker).PositionTopLeft(-columnwidth * Chart.Keys / 2, 0, AnchorType.CENTER, AnchorType.CENTER).PositionBottomRight(columnwidth * Chart.Keys / 2, 0, AnchorType.CENTER, AnchorType.MAX));
            if (widgetData.IsEnabled("progressBar"))
            {
                AddChild(new ProgressBar(scoreTracker).PositionTopLeft(-500, 10, AnchorType.CENTER, AnchorType.MIN).PositionBottomRight(500, 50, AnchorType.CENTER, AnchorType.MIN));
            }
            if (widgetData.IsEnabled("combo"))
            {
                AddChild(new ComboDisplay(scoreTracker).Position(widgetData.GetPosition("combo")));
            }
            if (widgetData.IsEnabled("accuracy"))
            {
                AddChild(new AccMeter(scoreTracker).Position(widgetData.GetPosition("accuracy")));
            }
            if (widgetData.IsEnabled("time"))
            {
                AddChild(new MiscInfoDisplay(scoreTracker, () => { return DateTime.Now.ToLongTimeString(); }).Position(widgetData.GetPosition("time")));
            }
            if (widgetData.IsEnabled("timeLeft"))
            {
                AddChild(new MiscInfoDisplay(scoreTracker, () => { return Utils.FormatTime((Chart.Notes.Points[Chart.Notes.Points.Count - 1].Offset - (float)Game.Audio.Now())/(float)Game.Options.Profile.Rate) + " left"; }).Position(widgetData.GetPosition("timeLeft")));
            }
            if (widgetData.IsEnabled("fps"))
            {
                AddChild(new MiscInfoDisplay(scoreTracker, () => { return ((int)Game.Instance.FPS).ToString() + "fps"; }).Position(widgetData.GetPosition("fps")));
            }
            if (widgetData.IsEnabled("judgements"))
            {
                AddChild(new JudgementDisplay(scoreTracker).Position(widgetData.GetPosition("judgements")));
            }
            //all this stuff needs to be moved to Playfield under a method that adds gameplay elements (not used when in editor)
            //playfield.InitGameplay();
            lighting = new HitLighting[Chart.Keys];
            float x = Chart.Keys * 0.5f;
            //this places a hitlight on every column
            for (int i = 0; i < Chart.Keys; i++)
            {
                lighting[i] = new HitLighting();
                lighting[i].PositionTopLeft(columnwidth * i, hitposition + columnwidth * 2, AnchorType.MIN, AnchorType.CENTER)
                    .PositionBottomRight(columnwidth * (i + 1), hitposition, AnchorType.MIN, AnchorType.CENTER);
                playfield.AddChild(lighting[i]);
            }
            //this places the screencovers
            if (Game.Options.Profile.ScreenCoverUp > 0)
            playfield.AddChild(new Screencover(scoreTracker, false)
                .PositionTopLeft(0, 0, AnchorType.MIN, AnchorType.CENTER).PositionBottomRight(0, ScreenHeight * 2 * Game.Options.Profile.ScreenCoverUp, AnchorType.MAX, AnchorType.CENTER));
            if (Game.Options.Profile.ScreenCoverDown > 0)
                playfield.AddChild(new Screencover(scoreTracker, true)
                .PositionTopLeft(0, ScreenHeight * 2 * (1 - Game.Options.Profile.ScreenCoverDown), AnchorType.MIN, AnchorType.CENTER).PositionBottomRight(0, ScreenHeight * 2, AnchorType.MAX, AnchorType.CENTER));
        }

        public override void OnEnter(Screen prev)
        {
            base.OnEnter(prev);
            //if returning from score screen, go straight to menu
            if (prev is ScreenScore)
            {
                Game.Screens.PopScreen(); return;
            }
            //some misc stuff
            Game.Screens.BackgroundDim.Target = 1 - Game.Options.Profile.BackgroundDim;
            Utils.SetDiscordData("Playing", Game.CurrentChart.Data.Artist + " - " + Game.CurrentChart.Data.Title + " [" + Game.CurrentChart.Data.DiffName + "]");
            Game.Options.Profile.Stats.TimesPlayed++;
            Game.Screens.Toolbar.SetHidden(true);
            AnimationSeries s = new AnimationSeries(false);
            s.Add(bannerIn = new AnimationFade(0, 2.001f, 2f));
            s.Add(new AnimationCounter(60, false));
            s.Add(new AnimationAction(() => { scoreTracker.WidgetColor.Target = 1; }));
            s.Add(bannerOut = new AnimationFade(0, 255, 254));
            Animation.Add(s);

            //prep audio and play from beginning
            Game.Audio.LocalOffset = Game.Gameplay.GetChartOffset();
            Game.Audio.OnPlaybackFinish = null;
            Game.Audio.Stop();
            Game.Audio.SetRate(Game.Options.Profile.Rate);
            Game.Audio.PlayLeadIn();
        }

        public override void OnExit(Screen next)
        {
            //some misc stuff
            Game.Screens.BackgroundDim.Target = 0.3f;
            Utils.SetDiscordData("Looking for something to play", "");
            Game.Screens.Toolbar.SetHidden(false);
            base.OnExit(next);
        }

        public override void Update(float left, float top, float right, float bottom) //update loop
        {
            float now = (float)Game.Audio.Now(); //get where we are now
            if (Input.KeyTap(Key.Escape)) //escape quits
            {
                Game.Screens.PopScreen();
                Widgets.Clear();
                Game.Options.Profile.Stats.TimesQuit++;
            }
            else if (Game.Audio.LeadingIn && Input.KeyTap(Game.Options.General.Binds.ChangeOffset)) //if map hasn't started you can sync it with + key
            {
                Game.Audio.Stop();
                Game.Screens.AddDialog(new Dialogs.TextDialog("Change sync by... (ms)", (x) => {
                    float f = 0; float.TryParse(x, out f); Game.Gameplay.ChartSaveData.Offset += f;
                    Game.Audio.LocalOffset = Game.Gameplay.GetChartOffset();
                    Game.Audio.PlayLeadIn();
                }));
            }
            else if (Input.KeyTap(Game.Options.General.Binds.Skip) && (Chart.Notes.Points[0].Offset - Game.Audio.Now() > 5000))
            {
                Game.Audio.Stop();
                Game.Audio.Seek(Chart.Notes.Points[0].Offset - 5000);
                Game.Audio.Play(); //in case of leading in
            }
            //actual input stuff
            for (byte k = 0; k < Chart.Keys; k++)
            {
                if (Input.KeyTap(binds[k])) //if you press a key
                {
                    OnKeyDown(k, now); //handle it in the context of the map and where we are (now)
                }
                else if (Input.KeyRelease(binds[k])) //if you release a key
                {
                    OnKeyUp(k, now); //handle it
                }
            }
            scoreTracker.Update(now - missWindow); //check for notes you've missed and handle them
            if (scoreTracker.EndOfChart()) //if the chart is over, go to score screen
            {
                Game.Screens.PopScreen();
                Game.Screens.AddScreen(new ScreenScore(scoreTracker));
                Widgets.Clear();
            }
            base.Update(left, top, right, bottom);
        }

        public void OnKeyDown(byte k, float now) //handle but also do the hit lighting stuff
        {
            HandleHit(k, now, false);
            lighting[k].ReceptorLight.Target = 1;
            lighting[k].ReceptorLight.Val = 1;
        }

        public void OnKeyUp(byte k, float now) //handle but also do the hit lighting stuff
        {
            HandleHit(k, now, true);
            lighting[k].ReceptorLight.Target = 0;
        }

        public void HandleHit(byte k, float now, bool release)
        {
            //basically, this whole algorithm finds the closest snap to the receptors (above or below) that is relevant (has a note in the column you're pressing)
            //- missWindow and + missWindow are used because all snaps found in that time slice are considered. the closest note to now in that column is the one you hit
            //this mechanic is different to other rhythm games where it finds the earliest unhit note in this window
            //you will miss more, but get fucked by column lockouts in jacks and dense streams less
            int i = Chart.Notes.GetNextIndex(now - missWindow);
            if (i >= lasti) { return; } //if there are no more notes, stop
            float delta = missWindow; //default value for the final "found" delta"
            float d; //temp delta for the note we're looking at
            int hitAt = -1; //when we find the note with the smallest d, its index is put in here
            int needsToHold = 1023;
            while (Chart.Notes.Points[i].Offset < now + missWindow) //search loop
            {
                Snap s = Chart.Notes.Points[i];
                needsToHold &= ~(s.ends.value + s.holds.value); //if there are any starts of hold or releases within range, don't worry about finger independence penalty
                BinarySwitcher b = release ? new BinarySwitcher(s.ends.value + s.taps.value + s.holds.value) : new BinarySwitcher(s.taps.value + s.holds.value);
                if (b.GetColumn(k)) //if there's a note here
                {
                    d = (now - s.Offset);
                    if (Math.Abs(d) < Math.Abs(delta))
                    {
                        delta = d;
                        hitAt = i;
                    }
                }
                i++;
                if (i == lasti) { break; }
            } //delta is misswindow if nothing found

            //LOOK AT THIS LINE \/ \/ \/ IT IS IMPORTANT I DO SOMETHING ABOUT IT I.E REMOVE IT LATER
            if (release) delta *= 0.5f; //releasing long notes is more lenient (hit windows twice as big). needs a setting to turn on and off or to be removed


            if (hitAt >= 0 && (!release || Chart.Notes.Points[hitAt].ends.GetColumn(k))) //if we found a note to hit (it's -1 if nothing found)
            //this first && is added because releasing looks for anything the the column and if a release was the closest it hits it. fixes some unhittable ln behaviours.
            //the second && checks that you are holding all the columns you're supposed to. it will just miss otherwise to prevent LN cheesing
            {
                foreach (byte c in new BinarySwitcher(needsToHold & Chart.Notes.Points[hitAt].middles.value).GetColumns())
                {
                    if (!Input.KeyPress(binds[c]))
                    {
                        return;
                    }
                }
                delta /= (float)Game.Options.Profile.Rate; //convert back to time relative to what player observes instead of map data
                //i.e on 2x rate if you hit 80ms away in the map data, you hit 40ms away in reality
                scoreTracker.RegisterHit(hitAt, k, delta); //handle the hit
                lighting[k].NoteLight.Val = 1;
            } //put else statement here for cb on unecessary keypress if i ever want to do that
        }
        
        public override void Draw(float left, float top, float right, float bottom)
        {
            base.Draw(left, top, right, bottom);
            if (Chart.Notes.Points[0].Offset - Game.Audio.Now() > 5000)
            {
                SpriteBatch.Font1.DrawCentredText("Press "+Game.Options.General.Binds.Skip.ToString().ToUpper()+" to Skip", 50f, 0, 100, Game.Options.Theme.MenuFont);
            }
            if (Animation.Running)
            {
                int a = 255 - (int)bannerOut;
                SpriteBatch.DrawRect(left, -55, left + ScreenWidth * bannerIn, -50, Color.FromArgb(a, Game.Screens.DarkColor));
                SpriteBatch.DrawRect(left, 50, left + ScreenWidth * bannerIn, 55, Color.FromArgb(a, Game.Screens.DarkColor));
                Game.Screens.DrawChartBackground(right - ScreenWidth * bannerIn, -50, right, 50, Color.FromArgb(a, Game.Screens.BaseColor));
                SpriteBatch.Font1.DrawCentredTextToFill(Game.CurrentChart.Data.Artist + " - " + Game.CurrentChart.Data.Title, right - ScreenWidth * bannerIn, -50, right, 30, Color.FromArgb(a, Game.Options.Theme.MenuFont));
                SpriteBatch.Font2.DrawCentredTextToFill(Game.CurrentChart.Data.DiffName + " // " + Game.CurrentChart.Data.Creator, left, 10, left + ScreenWidth * bannerIn, 50, Color.FromArgb(a, Game.Options.Theme.MenuFont));
            }
        }
    }
}
