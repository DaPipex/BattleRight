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

using TestPrediction2NS;

namespace PipAshka.Utils
{
    public class MySkillBase
    {
        public AbilitySlot Slot { get; set; }
        public float Range { get; set; }
        public float Speed { get; set; }
        public float Radius { get; set; }
        public float FixedDelay { get; set; }
        public AbilityHudData HudData => LocalPlayer.GetAbilityHudData(Slot);
        public bool IsReady
        {
            get
            {
                var hudData = HudData;
                if (LocalPlayer.Instance == null || hudData == null || HudData.EnergyCost > LocalPlayer.Instance.Energized.Energy)
                {
                    return false;
                }

                if (hudData.UsesCharges)
                {
                    return hudData.ChargeCount > 0;
                }
                else
                {
                    return hudData.CooldownLeft <= 0;
                }
            }
        }

        private MySkillBase(AbilitySlot slot, float range, float speed, float radius, float fixedDelay)
        {
            Slot = slot;
            Range = range;
            Speed = speed;
            Radius = radius;
            FixedDelay = fixedDelay;
        }

        public static MySkillBase NewProjectile(AbilitySlot slot, float range, float speed, float radius)
        {
            return new MySkillBase(slot, range, speed, radius, 0f);
        }

        public static MySkillBase NewCircle(AbilitySlot slot, float range, float fixedDelay, float radius)
        {
            return new MySkillBase(slot, range, 0f, radius, fixedDelay);
        }

        public TestOutput GetPrediction(InGameObject target, bool checkCollision, Vector2 fromPos, float overrideRange = 0f)
        {
            if (Speed >= float.Epsilon)
            {
                return TestPrediction.GetNormalLinePrediction(fromPos, target, overrideRange >= float.Epsilon ? overrideRange : Range, Speed, Radius, checkCollision);
            }
            else
            {
                //I should seriously wrap this into a GetFixedDelayPrediction. So TODO for later.
                return TestPrediction.GetPrediction(fromPos, target, overrideRange >= float.Epsilon ? overrideRange : Range, 0f, Radius, FixedDelay, 1.75f, checkCollision);
            }
        }

        public bool Cast()
        {
            return LocalPlayer.PressAbility(Slot, true);
        }
    }
}
