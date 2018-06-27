using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
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

            return abilityHudData != null && abilityHudData.CooldownLeft <= 0 && abilityHudData.EnergyCost <= EntitiesManager.LocalPlayer.Energy;
        }

        public static int EnemiesAround(this ActiveGameObject gameObj, float distance)
        {
            return EntitiesManager.EnemyTeam.Count(x => !x.IsDead && x.Distance(gameObj) <= distance);
        }

        public static bool HasBuff(this Player player, string buffName, out Buff buff)
        {
            if (player.Buffs.Any(x => x.ObjectName.Equals(buffName)))
            {
                buff = player.GetBuffOfName(buffName);
                return true;
            }

            buff = null;
            return false;
        }

        public static bool HasBuff(this Player player, string buffName)
        {
            return player.Buffs.Any(x => x.ObjectName.Equals(buffName));
        }

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
