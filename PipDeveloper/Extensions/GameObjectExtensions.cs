using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core.GameObjects;

namespace PipDeveloper.Extensions
{
    public static class GameObjectExtensions
    {
        public static bool IsSame(this InGameObject inGameObj, InGameObject other)
        {
            return inGameObj != null && other != null && inGameObj.Generation == other.Generation && inGameObj.Index == other.Index;
        }
    }
}
