using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.Math;
using BattleRight.Core.Models;

namespace NewPrediction.Extensions
{
    public static class PlayerExtensions
    {
        public static bool IsValidTarget(this Player player, float range, Vector2 rangeCheckPos)
        {
            return player.IsValid && Vector2.Distance(rangeCheckPos, player.WorldPosition) < range;
        }
    }
}
