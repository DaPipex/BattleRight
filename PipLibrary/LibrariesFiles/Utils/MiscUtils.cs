using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.GameObjects.Models;
using BattleRight.Core.Math;
using BattleRight.Core.Models;

using BattleRight.SDK;
using BattleRight.SDK.Enumeration;
using BattleRight.SDK.Events;
using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Models;
using BattleRight.SDK.UI.Values;

namespace PipLibrary.Utils
{
    public static class MiscUtils
    {
        public static bool CanCast(AbilitySlot slot)
        {
            var abilityHudData = LocalPlayer.GetAbilityHudData(slot);

            if (abilityHudData != null && abilityHudData.EnergyCost <= EntitiesManager.LocalPlayer.Energized.Energy)
            {
                if (abilityHudData.UsesCharges)
                {
                    return abilityHudData.ChargeCount > 0;
                }
                else
                {
                    return abilityHudData.CooldownLeft <= 0;
                }
            }

            return false;
        }

        public static int EnemiesAroundAlive(this InGameObject gameObj, float distance)
        {
            return EntitiesManager.EnemyTeam.Count(x => !x.Living.IsDead && x.Distance(gameObj.Get<MapGameObject>().Position) <= distance);
        }


        public static int EnemiesAroundAlive(this Vector2 position, float distance)
        {
            return EntitiesManager.EnemyTeam.Count(x => !x.Living.IsDead && x.Distance(position) <= distance);
        }

        public static bool HasBuff(this Character player, string buffName, out Buff buff)
        {
            if (player.Buffs.Any(x => x.ObjectName.Equals(buffName)))
            {
                buff = player.GetBuffOfName(buffName);
                return true;
            }

            buff = null;
            return false;
        }

        public static bool HasBuff(this Character player, string buffName)
        {
            return player.Buffs.Any(x => x.ObjectName.Equals(buffName));
        }

        public static bool HasReflectiveShield(this Character player)
        {
            //return player.HasBuff("BulwarkBuff") || player.HasBuff("DivineShieldBuff");
            return player.Buffs.Any(x => x.ObjectName.Equals("BulwarkBuff") || x.ObjectName.Equals("DivineShieldBuff"));
        }

        public static bool IsReflectiveShield(this Buff buff)
        {
            return buff.ObjectName.Equals("BulwarkBuff") || buff.ObjectName.Equals("DivineShieldBuff");
        }

        public static bool HasParry(this Character player)
        {
            //return player.HasBuff("GustBuff") || player.HasBuff("TimeBenderBuff");
            return player.Buffs.Any(x => x.ObjectName.Equals("GustBuff") || x.ObjectName.Equals("TimeBenderBuff"));
        }

        public static bool IsParry(this Buff buff)
        {
            return buff.ObjectName.Equals("GustBuff") || buff.ObjectName.Equals("TimeBenderBuff");
        }

        public static bool IsImmaterial(this Buff buff)
        {
            return buff.ObjectName.Equals("OtherSideBuff") || buff.ObjectName.Equals("Fleetfoot")
                || buff.ObjectName.Equals("TempestRushBuff");
        }

        public static bool HasProjectileBlocker(this Character player)
        {
            return player.Buffs.Any(x => x.IsImmaterial() 
            || x.IsCounter || x.IsConsume || x.IsReflectiveShield() || x.IsParry());
        }

        public static bool HasHardCC(this Character player)
        {
            return player.HasCCOfType(CCType.Stun) || player.HasCCOfType(CCType.Snared) || player.HasCCOfType(CCType.Root);
        }

        [Obsolete]
        public static Vector2 TempScreenToWorld(this Vector2 screenPos)
        {
            var cam = UnityEngine.Camera.main;
            var ray = cam.ScreenPointToRay(new UnityEngine.Vector3(screenPos.X, screenPos.Y));
            var plane = new UnityEngine.Plane(UnityEngine.Vector3.up, UnityEngine.Vector3.zero);

            float d;
            if (plane.Raycast(ray, out d))
            {
                return new Vector2(ray.GetPoint(d).x, ray.GetPoint(d).z);
            }

            return Vector2.Zero;
        }
    }
}
