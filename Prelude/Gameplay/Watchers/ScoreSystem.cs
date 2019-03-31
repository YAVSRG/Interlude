﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prelude.Gameplay.Watchers.Scoring;

namespace Prelude.Gameplay.Watchers
{
    //todo: option between points out of 100% rising, max accuracy possible and current accuracy for time series
    public class ScoreSystem : IGameplayWatcher
    {
        //todo: remove and replace with dynamic score system list
        public enum ScoreType
        {
            Default,
            Osu,
            DP,
            Wife,
            SCPlus,
            Custom
        }

        //this should be a constant but its chilling here just in case it needs changing
        public readonly float MissWindow = 180f;

        //array of windows in milliseconds to score a certain judgement
        //e.g. for the best judgement your absolute deviation must be less than JudgementWindows[0]
        protected float[] JudgementWindows;

        //points awarded for each judgement, should 1 more than length of JudgementWindows (extra for missing a note)
        protected int[] PointsPerJudgement;

        //maximum points awarded for each judgement (should be same as PointsPerJudgement[0] but allows for funny use cases)
        protected int MaxPointsPerNote;

        //this judgement index OR WORSE will cause a combo break e.g. 3 awards Good, so 3,4 or 5 (good, bad, miss) all combo break if this is set to 3
        protected int ComboBreakingJudgement;

        //display name of this accuracy system
        protected string Name;

        //max points possible to have scored so far
        protected float PossiblePoints = 0;

        //points the user has scored so far
        protected float PointsScored = 0;

        //CURRENT combo the user is on
        public int Combo = 0;

        //amount of times the user has broken combo
        public int ComboBreaks = 0;

        //BEST combo the user has scored so far
        public int BestCombo = 0;

        //array storing number of each judgement the user has achieved so far
        public int[] Judgements;

        //hook used by things like UI on the gameplay screen to display judgements when you get them
        public Action<int, int, float> OnHit = (Column, Judgement, Delta) => { };

        public ScoreSystem(string Name, int JudgementCount)
        {
            this.Name = Name;
            Judgements = new int[JudgementCount];
        }

        public virtual float GetPointsForNote(float Delta)
        {
            return PointsPerJudgement[JudgeHit(Delta)];
        }

        protected virtual void ComboBreak()
        {
            if (Combo > BestCombo)
            {
                BestCombo = Combo;
            }
            Combo = 0;
            ComboBreaks += 1;
        }

        public override void HandleHit(int Column, int Index, HitData[] HitData)
        {
            float delta = Math.Abs(HitData[Index].delta[Column]);
            int Judgement = JudgeHit(delta);
            Judgements[Judgement] += 1;
            PointsScored += GetPointsForNote(delta);
            PossiblePoints += MaxPointsPerNote;
            if (Judgement >= ComboBreakingJudgement)
            {
                ComboBreak();
            }
            else
            {
                Combo++;
            }
            OnHit(Column, Judgement, HitData[Index].delta[Column]);
        }

        public override void ProcessScore(HitData[] HitData)
        {
            while (Counter < HitData.Length)
            {
                for (int i = 0; i < HitData[Counter].hit.Length; i++)
                {
                    if (HitData[Counter].hit[i] == 1)
                    {
                        HitData[Counter].delta[i] = MissWindow;
                        HandleHit(i, Counter, HitData);
                    }
                    else if (HitData[Counter].hit[i] == 2)
                    {
                        HandleHit(i, Counter, HitData);
                    }
                }
                Counter++;
            }
            BestCombo = Math.Max(Combo, BestCombo);
        }

        public override void Update(float Now, HitData[] HitData)
        {
            Now -= MissWindow;
            while (Counter < HitData.Length && HitData[Counter].Offset <= Now)
            {
                for (int i = 0; i < HitData[Counter].hit.Length; i++)
                {
                    if (HitData[Counter].hit[i] == 1)
                    {
                        HitData[Counter].delta[i] = MissWindow;
                        HandleHit(i, Counter, HitData);
                    }
                }
                Counter++;
            }
            if (Counter == HitData.Length) BestCombo = Math.Max(Combo, BestCombo);
        }

        public virtual float Accuracy()
        {
            return 100f * GetValue();
        }

        public override float GetValue()
        {
            if (PossiblePoints == 0) return 1;
            return PointsScored / PossiblePoints;
        }

        public virtual int JudgeHit(float Delta)
        {
            for (int i = 0; i < JudgementWindows.Length; i++)
            {
                if (Delta < JudgementWindows[i]) { return i; }
            }
            return JudgementWindows.Length;
        }

        public virtual string FormatAcc()
        {
            return string.Format("{0:0.00}", Math.Round(Accuracy(), 2)) + "% ("+Name+")";
        }
    }
}