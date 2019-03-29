﻿using System;
using Interlude.Graphics;

namespace Interlude.Interface.Widgets
{
    public class SimpleButton : Widget //planned new button to replace FramedButton in most things in future
    {
        protected Func<string> text;
        protected Action action;
        protected Animations.AnimationColorMixer color;
        protected Func<bool> highlight;
        protected float fontsize;

        public SimpleButton(string label, Action onClick, Func<bool> highlight, float size) : this(() => (label), onClick, highlight, size)
        {
        }

        public SimpleButton(Func<string> label, Action onClick, Func<bool> highlight, float size) : base()
        {
            fontsize = size;
            text = label;
            action = onClick;
            this.highlight = highlight;
            Animation.Add(color = new Animations.AnimationColorMixer(Game.Screens.BaseColor));
        }

        public override void Draw(Rect bounds)
        {
            base.Draw(bounds);
            bounds = GetBounds(bounds);
            SpriteBatch.Font1.DrawCentredText(text(), fontsize, bounds.CenterX, bounds.Top, color, true, Utils.ColorInterp(color,System.Drawing.Color.Black, 0.7f));
            SpriteBatch.DrawRect(new Rect(bounds.Left, bounds.Bottom - 10, bounds.Right, bounds.Bottom), color); //slice
        }

        public override void Update(Rect bounds)
        {
            base.Update(bounds);
            bounds = GetBounds(bounds);
            color.Target(highlight() ? System.Drawing.Color.White : ScreenUtils.MouseOver(bounds) ? Game.Screens.HighlightColor : Game.Screens.BaseColor);
            if (ScreenUtils.CheckButtonClick(bounds))
            {
                Game.Audio.PlaySFX("click");
                action();
            }
        }
    }
}
