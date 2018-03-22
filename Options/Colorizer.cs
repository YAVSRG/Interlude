﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using YAVSRG.Gameplay;
using YAVSRG.Beatmap;

namespace YAVSRG.Options
{
    class Colorizer
    {
        public static readonly int[] DDRValues = { 1, 2, 4, 8, 3, 6, 12 };

        public enum ColorStyle
        {
            DDR,
            Column,
            Chord,
            //LongNote,
            //Jackhammer,
            //Manipulate
        }

        public static void Colorize(ChartWithModifiers c, ColorScheme s)
        {
            switch (s.Style)
            {
                case (ColorStyle.DDR):
                    DDR(c, s);
                    return;
                case (ColorStyle.Column):
                    Column(c, s);
                    return;
                case (ColorStyle.Chord):
                    Chord(c, s);
                    return;
            }
        }

        private static void Jackhammer(ChartWithModifiers c, ColorScheme cs)
        {
            //DO NOT USE UNTIL FINISHED/FIXED
            int last = c.Notes.Points[0].taps.value;
            int current;
            for (int i = 1; i < c.Notes.Count; i++)
            {
                current = c.Notes.Points[i].taps.value;
                foreach (int k in new BinarySwitcher(current & last).GetColumns())
                {
                    c.Notes.Points[i - 1].colors[k] = 3;
                    c.Notes.Points[i].colors[k] += 1;
                }
                last = current;
            }
        }
        private static void DDR(ChartWithModifiers c, ColorScheme cs)
        {
            //sorry, i'll comment it soon, but the code is likely to change around a bit
            float v;
            float x;
            BPMPoint p;
            int color;
            foreach (Snap s in c.Notes.Points)
            {
                p = c.Timing.GetPointAt(c.Timing.GetPointAt(s.Offset, false).InheritsFrom, false);
                v = p.MSPerBeat;
                x = (s.Offset - p.Offset) % v;

                color = 7;
                for (int i = 0; i < DDRValues.Length; i++)
                {
                    if (RoughlyDivisibleBy(x, v / DDRValues[i]))
                    {
                        color = i;
                        break;
                    }
                }
                for (int k = 0; k < c.Keys; k++)
                {
                    s.colors[k] = cs.GetColorIndex(color,c.Keys);
                }
            }
        }

        private static void Column(ChartWithModifiers c, ColorScheme cs)
        {
            foreach (Snap s in c.Notes.Points)
            {
                s.colors = new int[c.Keys];
                for (int i = 0; i < c.Keys; i++)
                {
                    s.colors[i] = cs.GetColorIndex(i,c.Keys); //color notes based on column.
                }
            }
        }

        private static void Chord(ChartWithModifiers c, ColorScheme cs)
        {
            int count;
            foreach (Snap s in c.Notes.Points)
            {
                s.colors = new int[c.Keys];
                count = new BinarySwitcher(s.taps.value | s.holds.value).Count; //count number of lns/notes
                for (int i = 0; i < c.Keys; i++)
                {
                    s.colors[i] = cs.GetColorIndex(count,c.Keys); //color notes in row based on number of notes. chord coloring.
                }
            }
        }

        private static bool RoughlyDivisibleBy(float a, float b)
        {
            var x = a % b;

            return Math.Min(Math.Abs(x),Math.Abs(b-x)) < 2;
        }
    }
}
