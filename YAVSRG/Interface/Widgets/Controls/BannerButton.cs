﻿using System;
using Interlude.Graphics;

namespace Interlude.Interface.Widgets
{
    class BannerButton : SpriteButton
    {
        public BannerButton(string label, Action onClick, AnchorType text = AnchorType.CENTER) : base("banner", label, onClick)
        {
        }

        public override void Draw(Rect bounds)
        {
            bounds = GetBounds(bounds);
            ScreenUtils.DrawParallelogramWithBG(bounds, 0.5f, color, color);
            SpriteBatch.Font1.DrawJustifiedText(text, 40f, bounds.Right - bounds.Height, bounds.Top + 15, Game.Options.Theme.MenuFont, true, Game.Screens.DarkColor);
        }
    }
}