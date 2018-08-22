﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAVSRG.Gameplay
{
    public class Score
    {
        public DateTime time;
        public string hitdata;
        public string player;
        public Dictionary<string,string> mods;
        public float rate;
        public string playstyle;
        public int keycount;
    }

    public class TopScore
    {
        public float rating;
        public float accuracy;
        public string mods;
        public string hash;
        public string abspath;
    }
}
