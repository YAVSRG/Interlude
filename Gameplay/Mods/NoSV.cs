﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAVSRG.Charts.YAVSRG;

namespace YAVSRG.Gameplay.Mods
{
    public class NoSV : Mod
    {
        public override bool IsApplicable(ChartWithModifiers c, string data)
        {
            return c.Timing.ContainsSV();
        }

        public override void Apply(ChartWithModifiers c, string data)
        {
            base.Apply(c, data);
            c.Timing.SetBlankSVData();
        }

        public override string GetName(string data)
        {
            return "NoSV";
        }

        public override string GetDescription(string data) { return "Removes all slider velocity (scroll speed) changes from a chart."; }
    }
}
