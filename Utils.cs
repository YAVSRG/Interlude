﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAVSRG
{
    class Utils
    {
        public static int Modulus(int a, int b)
        {
            while (a < 0)
            {
                a += b;
            }
            return a % b;
        }

        public static string RoundNumber(float x)
        {
            return Math.Round(x, 2).ToString();
        }

        public static string FormatTime(float ms)
        {
            int seconds = (int)(ms / 1000) % 60;
            int minutes = (int)Math.Floor(ms / 60000);
            return minutes.ToString() + ":" + seconds.ToString().PadLeft(2,'0');
        }

        public static T LoadObject<T>(string path)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(System.IO.File.ReadAllText(path));
        }

        public static void SaveObject<T>(T obj, string path)
        {
            System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
