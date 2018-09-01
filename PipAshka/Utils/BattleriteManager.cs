using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Models;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.GameObjects.Models;
using BattleRight.Core.Math;

using BattleRight.SDK;
using BattleRight.SDK.Enumeration;
using BattleRight.SDK.Events;
using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Models;
using BattleRight.SDK.UI.Values;

namespace PipAshka.Utils
{
    internal static class BattleriteManager
    {
        private static List<Battlerite> Battlerites = new List<Battlerite>();

        internal static bool HasEruption { get; private set; } = false;
        internal static bool HasKnockout { get; private set; } = false;
        internal static bool HasMachPunch { get; private set; } = false;
        internal static bool HasConflagration { get; private set; } = false;

        internal static void Clear()
        {
            Battlerites.Clear();
        }

        internal static void Update()
        {
            if (Utilities.Hero == null)
            {
                return;
            }

            Clear();

            for (var i = 0; i < 5; i++)
            {
                var br = Utilities.Hero.BattleriteSystem.GetEquippedBattlerite(i);
                if (br != null)
                {
                    Battlerites.Add(br);
                }
            }

            HasEruption = Battlerites.Any(x => x.Name.Equals("EruptionNewUpgrade"));
            HasKnockout = Battlerites.Any(x => x.Name.Equals("KnockoutNewUpgrade"));
            HasMachPunch = Battlerites.Any(x => x.Name.Equals("MachPunchUpgrade"));
            HasConflagration = Battlerites.Any(x => x.Name.Equals("ConflagrationUpgrade"));
        }
    }
}
