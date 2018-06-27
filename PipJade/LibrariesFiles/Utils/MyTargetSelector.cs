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
    public static class MyTargetSelector
    {
        public static TargetingMode TargetingMode = TargetingMode.NearLocalPlayer;

        public static Player GetTarget(ActiveGameObject from = null)
        {
            return GetTarget(EntitiesManager.EnemyTeam.Where(e => !e.IsImmaterial && !e.IsCountering), TargetingMode, int.MaxValue, from);
        }

        public static Player GetTarget(TargetingMode mode, ActiveGameObject from = null)
        {
            return GetTarget(EntitiesManager.EnemyTeam.Where(e => !e.IsImmaterial && !e.IsCountering), mode, int.MaxValue, from);
        }

        public static Player GetTarget(float worldDistance, ActiveGameObject from = null)
        {
            return GetTarget(EntitiesManager.EnemyTeam.Where(e => !e.IsImmaterial && !e.IsCountering), TargetingMode, worldDistance, from);
        }

        public static Player GetTarget(TargetingMode mode, float worldDistance, ActiveGameObject from = null)
        {
            return GetTarget(EntitiesManager.EnemyTeam.Where(e => !e.IsImmaterial && !e.IsCountering), mode, worldDistance, from);
        }

        public static Player GetAlly(ActiveGameObject from = null)
        {
            return GetTarget(EntitiesManager.LocalTeam, TargetingMode, int.MaxValue, from);
        }

        public static Player GetAlly(TargetingMode mode, ActiveGameObject from = null)
        {
            return GetTarget(EntitiesManager.LocalTeam.Where(a => !a.IsLocalPlayer), mode, int.MaxValue, from);
        }

        public static Player GetAlly(float worldDistance, ActiveGameObject from = null)
        {
            return GetTarget(EntitiesManager.LocalTeam.Where(a => !a.IsLocalPlayer), TargetingMode, worldDistance, from);
        }

        public static Player GetAlly(TargetingMode mode, float worldDistance, ActiveGameObject from = null)
        {
            return GetTarget(EntitiesManager.LocalTeam.Where(a => !a.IsLocalPlayer), mode, worldDistance, from);
        }

        public static Player GetTarget(IEnumerable<Player> playerInfos, TargetingMode mode, float worldDistance, ActiveGameObject from = null)
        {
            if (playerInfos == null)
                return null;

            from = from ?? EntitiesManager.LocalPlayer;

            var aliveTargets = playerInfos.Where(e => !e.IsDead && e.Distance(from) <= worldDistance);
            switch (mode)
            {
                case TargetingMode.NearMouse:
                    return aliveTargets.OrderBy(o => o.ScreenPosition.Distance(InputManager.MousePosition)).FirstOrDefault();
                case TargetingMode.NearLocalPlayer:
                    return aliveTargets.OrderBy(o => o.Distance(from)).FirstOrDefault();
                case TargetingMode.LowestHealth:
                    return aliveTargets.OrderBy(o => o.Health).FirstOrDefault();
                default:
                    return null;
            }
        }
    }
}
