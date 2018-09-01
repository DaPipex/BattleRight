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

using BattleRight.Sandbox;

using PipLibrary.Extensions;
using PipLibrary.Utils;

using TestPrediction2NS;

using AH = PipAshka.AbilityHandler;

namespace PipAshka.Utils
{
    internal static class Utilities
    {
        internal static Character Hero => LocalPlayer.Instance;
        internal static string HeroName => "Ashka";

        internal static float TrueERange => !BattleriteManager.HasMachPunch 
            ? AH.E.Range 
            : AH.E.Range + (AH.E.Range * 50f / 100f);

        internal static IEnumerable<Character> EnemiesBase => EntitiesManager
            .EnemyTeam
            .Where(x => x.IsValid && !x.Living.IsDead && !x.Buffs.Any(y => y.IsImmaterial()));

        internal const bool IsDebug = true;

        internal static AbilitySlot? CastingIndexToSlot(int index)
        {
            switch (index)
            {
                case 0:
                case 1:
                case 5:
                case 7:
                case 8:
                case 15:
                case 16:
                    return AbilitySlot.Ability1;
                case 2:
                case 17:
                case 18:
                case 19:
                    return AbilitySlot.Ability2;
                case 3:
                    return AbilitySlot.Ability3;
                case 9:
                case 20:
                    return AbilitySlot.Ability4;
                case 11:
                case 12:
                    return AbilitySlot.Ability5;
                case 13:
                    return AbilitySlot.Ability6;
                case 6:
                    return AbilitySlot.EXAbility1;
                case 10:
                    return AbilitySlot.EXAbility2;
                case 14:
                    return AbilitySlot.Ability7;
                case 21:
                case 22:
                    return AbilitySlot.Mount;
                default:
                    return null;
            }
        }
    }
}
