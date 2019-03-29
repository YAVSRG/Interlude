﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interlude.Gameplay.Watchers
{
    public abstract class ILifeMeter : IGameplayWatcher
    {
        protected float MaximumHP = 100;
        protected float CurrentHP = 100;
        protected float[] PointsPerJudgement;
        protected bool Failed;

        public virtual bool HasFailed()
        {
            return Failed;
        }

        public override float GetValue()
        {
            return CurrentHP / MaximumHP;
        }
    }
}
