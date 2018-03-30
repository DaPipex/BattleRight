using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.Math;
using BattleRight.Core.Models;

using NewPrediction.Enumerations;

namespace NewPrediction
{
    public class PredictionOutput
    {
        #region Fields

        internal PredictionInput Input;

        public Hitchance Hitchance = Hitchance.VeryLow;

        public Vector2 MousePosition;

        public Vector2 TargetPosition;

        public Vector2 CollisionPoint;

        #endregion
    }
}
