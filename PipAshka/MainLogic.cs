using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

using PipLibrary.Extensions;
using PipLibrary.Utils;

using PipAshka.Modes;
using PipAshka.Utils;

namespace PipAshka
{
    internal static class MainLogic
    {
        private static Menu KeysMenu => AshkaMenu.KeysMenu;

        internal static AbilitySlot? LastAbilityFired = null;
        internal static bool IsCastingOrChanneling = false;

        internal static void OnUpdate(EventArgs args)
        {
            var hero = Utilities.Hero;

            if (hero.Living.IsDead)
            {
                return;
            }

            IsCastingOrChanneling =
                hero.AbilitySystem.IsCasting || hero.IsChanneling;

            if (IsCastingOrChanneling && LastAbilityFired == null)
            {
                LastAbilityFired = Utilities.CastingIndexToSlot(hero.AbilitySystem.CastingAbilityIndex);
            }

            if (KeysMenu.GetKeybind("combo"))
            {
                Combo.Update();
            }
            else
            {
                LocalPlayer.EditAimPosition = false;
                LastAbilityFired = null;
            }
        }

        internal static void OnDraw(EventArgs args)
        {
            var hero = Utilities.Hero;

            if (hero.Living.IsDead)
            {
                return;
            }

            Drawings.Draw();
        }

        internal static void OnMatchStateUpdate(MatchStateUpdate args)
        {
            if (Utilities.Hero == null)
            {
                return;
            }

            if (args.OldMatchState == MatchState.BattleritePicking || args.NewMatchState == MatchState.PreRound)
            {
                BattleriteManager.Update();
            }
        }
    }
}
