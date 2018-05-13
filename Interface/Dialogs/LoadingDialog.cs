﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAVSRG.Interface.Animations;

namespace YAVSRG.Interface.Dialogs
{
    public class LoadingDialog : Dialog
    {
        AnimationColorMixer c;
        AnimationCounter anim;
        public LoadingDialog(Action<string> action) : base(action)
        {
            Animation.Add(c = new AnimationColorMixer(System.Drawing.Color.FromArgb(0, 0, 0, 0)));
            Animation.Add(anim = new AnimationCounter(100000000, true));
            c.Target(System.Drawing.Color.FromArgb(200, 0, 0, 0));
            SpriteBatch.ParallelogramTransform(0, 0);
        }

        public override void Update(float left, float top, float right, float bottom)
        {
            base.Update(left, top, right, bottom);
            if (ChartLoader.Loaded)
            {
                SpriteBatch.DisableTransform();
                Close("");
            }
        }

        public override void Draw(float left, float top, float right, float bottom)
        {
            SpriteBatch.DisableTransform();
            base.Draw(left, top, right, bottom);
            ConvertCoordinates(ref left, ref top, ref right, ref bottom);
            SpriteBatch.DrawRect(left, top, right, bottom, c);
            SpriteBatch.Font1.DrawCentredTextToFill("Loading...", -300, -100, 300, 100, Game.Options.Theme.MenuFont);
            SpriteBatch.ParallelogramTransform((float)Math.Sin(anim.value * 0.01f), ScreenUtils.ScreenHeight * (float)Math.Cos(anim.value * 0.01f));
        }
    }
}