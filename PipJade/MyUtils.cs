using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.Math;
using BattleRight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PipJade
{
    public static class MyUtils
    {
        public static int EnemiesAround(this Player player, float range)
        {
            int enemiesAround = 0;

            foreach (var enemy in EntitiesManager.EnemyTeam)
            {
                if (!enemy.IsDead && Vector2.Distance(player.WorldPosition, enemy.WorldPosition) <= range)
                {
                    enemiesAround++;
                }
            }

            return enemiesAround;
        }

        public static bool CanCastAbility(AbilitySlot slot)
        {
            //var abilityHudData = LocalPlayer.GetAbilityHudData(slot);
            //return abilityHudData.CooldownTime == 0f && abilityHudData.EnergyCost <= LocalPlayer.Instance.Energy;

            var abilityData = LocalPlayer.GetAbilityData(AbilitySlotDataToIndex(slot));
            return abilityData.CanCast;
        }

        public static int AbilitySlotDataToIndex(AbilitySlot slot)
        {
            switch (slot)
            {
                case AbilitySlot.Ability1:
                    return 0;

                case AbilitySlot.Ability2:
                    return 1;

                case AbilitySlot.Ability3:
                    return 2;

                case AbilitySlot.Ability4:
                    return 3;

                case AbilitySlot.Ability5:
                    return 4;

                case AbilitySlot.EnergyAbility:
                    return 5;

                case AbilitySlot.UltimateAbility:
                    return 6;

                case AbilitySlot.Mount:
                    return 7;

                default:
                    return 8;
            }
        }
    }
}
