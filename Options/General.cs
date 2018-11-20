﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAVSRG.Options
{
    public class General
    {
        public enum WindowType
        {
            Window,
            Borderless,
            Fullscreen
        }

        public static readonly List<Tuple<int, int>> RESOLUTIONS = new List<Tuple<int, int>>
        {
            new Tuple<int, int>(800, 600),
            new Tuple<int, int>(1024, 768),
            new Tuple<int, int>(1280, 800),
            new Tuple<int, int>(1280, 1024),
            new Tuple<int, int>(1366, 768),
            new Tuple<int, int>(1600, 900),
            new Tuple<int, int>(1600, 1024),
            new Tuple<int, int>(1680, 1050),
            new Tuple<int, int>(1768, 992),
            new Tuple<int, int>(1920, 1080),
            new Tuple<int, int>(2715, 1527),
        };

        public float UniversalAudioOffset = 0f;
        public float AudioVolume = 0.1f;
        public int FrameLimiter = 0;
        public int Resolution = 4;
        public WindowType WindowMode = WindowType.Borderless;
        public string CurrentProfile = "Default.json";
        public string WorkingDirectory = "";
        public Keybinds Binds = new Keybinds();
    }
}
