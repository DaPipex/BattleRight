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

using PipAshka.Utils;

namespace PipAshka
{
    internal static class AbilityHandler
    {
        internal static MySkillBase M1 { get; private set; }
        internal static MySkillBase M2 { get; private set; }
        internal static MySkillBase Space { get; private set; }
        internal static MySkillBase Q { get; private set; }
        internal static MySkillBase E { get; private set; }
        internal static MySkillBase R { get; private set; }
        internal static MySkillBase EX1 { get; private set; }
        internal static MySkillBase EX2 { get; private set; }
        internal static MySkillBase F { get; private set; }
        internal static MySkillBase Interrupt { get; private set; }

        internal static void Initialize()
        {
            M1 = MySkillBase.NewProjectile(AbilitySlot.Ability1, 7.2f, 15.5f, 0.3f);
            M2 = MySkillBase.NewProjectile(AbilitySlot.Ability2, 9f, 20.5f, 0.35f);
            Space = MySkillBase.NewCircle(AbilitySlot.Ability3, 8f, 0.75f, 1.5f);
            Q = MySkillBase.NewCircle(AbilitySlot.Ability4, 9f, 1.1f, 2f);
            E = MySkillBase.NewProjectile(AbilitySlot.Ability5, 4.5f, 15f, 0.5f);
            R = MySkillBase.NewCircle(AbilitySlot.Ability6, 9f, 0.1f, 2.8f); //w: 2.8f - h: 0.8f
            EX1 = MySkillBase.NewCircle(AbilitySlot.EXAbility1, 8f, 0.75f, 1.5f);
            EX2 = MySkillBase.NewCircle(AbilitySlot.EXAbility2, 2.5f, 0.5f, 2.5f);
            F = MySkillBase.NewProjectile(AbilitySlot.Ability7, 10f, 2.5f, 1f);
            Interrupt = MySkillBase.NewProjectile(AbilitySlot.Interrupt, 0f, 0f, 0f);
        }
    }
}
