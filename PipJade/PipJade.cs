using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Models;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.Math;

using BattleRight.SDK;
using BattleRight.SDK.Enumeration;
using BattleRight.SDK.Events;
using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Models;
using BattleRight.SDK.UI.Values;

namespace PipJade
{
    internal static class PipJade
    {
        private static Menu JadeMenu = null;

        private static Player JadeHero;

        private const bool JadeDebugMode = true;

        //THE SPEEDS ARE ROUGH ESTIMATES, NEED TO BE MORE ACCURATELY ACQUIRED
        private const float M1ProjSpeed = 17f;
        private const float M2ProjSpeed = 30f;
        private const float EProjSpeed = 30f;
        private const float RProjSpeed = 20f;
        private const float FProjSpeed = 22.5f;

        private const float M1ProjRange = 7f;
        private const float M2ProjRange = 11.5f;
        private const float SpaceRange = 4f;
        private const float EProjRange = 9.5f;
        private const float RProjRange = 5f;
        private const float FProjRange = 11.6f;
        //THE SPEEDS ARE ROUGH ESTIMATES, NEED TO BE MORE ACCURATELY ACQUIRED

        //HELPERS
        private static Projectile myLastProj = null;

        private static Vector2 myLastProjPreviousPos = Vector2.Zero;

        private static AbilitySlot? lastAbilityFired = null;

        private static readonly CollisionFlags normalCollisions = CollisionFlags.NPCBlocker | CollisionFlags.Bush | CollisionFlags.InvisWalls;
        //HELPERS

        public static void Init()
        {
            //LoadMenu();
            var _jadeMenu = MainMenu.AddMenu(new Menu("pipjademenu", "DaPipex's Jade"));

            _jadeMenu.AddLabel("Basic Combo");
            _jadeMenu.Add(new MenuCheckBox("combo.useM1", "Use Left-Mouse (Revolver Shot)", true));
            _jadeMenu.Add(new MenuCheckBox("combo.useM2", "Use Right-Mouse (Snipe)", true));
            _jadeMenu.Add(new MenuCheckBox("combo.useR", "Use R (Junk Shot)", true));
            _jadeMenu.Add(new MenuCheckBox("combo.useEX1", "Use EX1 (Snap Shot)", true));
            _jadeMenu.Add(new MenuCheckBox("combo.useF", "Use F (Explosive Shells)", true));
            _jadeMenu.Add(new MenuCheckBox("combo.useNewPred", "Use new prediction (EXPERIMENTAL)", false));

            _jadeMenu.AddSeparator(10f);

            _jadeMenu.AddLabel("Misc");
            _jadeMenu.Add(new MenuCheckBox("misc.useE", "Use E (Disabling Shot) to interrupt", true));

            _jadeMenu.AddSeparator(10f);

            _jadeMenu.AddLabel("Drawings");
            _jadeMenu.Add(new MenuCheckBox("draw.rangeM1", "Draw Left-Mouse range (Revolver Shot)", false)); //true
            _jadeMenu.Add(new MenuCheckBox("draw.rangeM2", "Draw Right-Mouse range (Snipe)", true));
            _jadeMenu.Add(new MenuCheckBox("draw.rangeE", "Draw E range (Disabling Shot)", true));
            _jadeMenu.Add(new MenuCheckBox("draw.rangeR", "Draw R range (Junk Shot)", false));
            _jadeMenu.Add(new MenuCheckBox("draw.rangeF", "Draw F range (Explosive Shells)", false));

            if (JadeDebugMode)
            {
                _jadeMenu.AddSeparator(10f);

                _jadeMenu.AddLabel("Debug");
                _jadeMenu.Add(new MenuCheckBox("debug.projSpeed", "My last projectile's speed", false));
                _jadeMenu.Add(new MenuCheckBox("debug.newPrediction", "Enemies Movement Prediction", false));
            }

            CustomEvents.Instance.OnMatchStart += OnMatchStart;
            CustomEvents.Instance.OnMatchEnd += OnMatchEnd;
            CustomEvents.Instance.OnDraw += OnDraw;
            CustomEvents.Instance.OnUpdate += delegate
            {
                JadeHero = EntitiesManager.LocalPlayer;
                JadeMenu = _jadeMenu;

                OnUpdate();
            };
        }

        private static void OnMatchStart(EventArgs e)
        {

        }

        private static void OnMatchEnd(EventArgs e)
        {

        }

        private static void OnUpdate()
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (JadeHero.CharName != "Gunner")
            {
                return;
            }

            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.Mouse3))
            {
                OrbStealerMode();
            }
            else if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl))
            {
                ComboMode();
            }

            if (JadeDebugMode)
            {
                DebugStuff();
            }
        }

        private static void ComboMode()
        {
            Player targetM1 = TargetSelector.GetTarget(TargetingMode.LowestHealth, M1ProjRange);
            Player targetM2_F = TargetSelector.GetTarget(TargetingMode.LowestHealth, M2ProjRange);
            Player targetR = TargetSelector.GetTarget(TargetingMode.LowestHealth, RProjRange);
            Player targetE = null;

            bool useNewPred = JadeMenu.GetBoolean("combo.useNewPred");
            bool castingSomething = JadeHero.IsCasting || JadeHero.IsChanneling;

            if ((JadeMenu.GetBoolean("misc.useE") && MyUtils.CanCastAbility(AbilitySlot.Ability5)) || (castingSomething && lastAbilityFired == AbilitySlot.Ability5))
            {
                if (targetE == null)
                {
                    foreach (var enemy in EntitiesManager.EnemyTeam)
                    {
                        if (enemy.IsValid && !enemy.IsImmaterial && !enemy.IsCountering && !enemy.IsDead && (enemy.IsCasting || enemy.IsChanneling))
                        {
                            if (Vector2.Distance(JadeHero.WorldPosition, enemy.WorldPosition) <= EProjRange)
                            {
                                targetE = enemy;
                                break;
                            }
                        }
                    }
                }

                if (targetE != null)
                {
                    if (!useNewPred)
                    {
                        var pred = Prediction.GetPrediction(JadeHero, targetE, EProjSpeed, EProjRange, 0f, SkillType.Line, 0, normalCollisions);

                        if (pred.HitChancePercent >= 25f)
                        {
                            LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                            LocalPlayer.CastAbility(AbilitySlot.Ability5);
                            lastAbilityFired = AbilitySlot.Ability5;
                        }
                    }
                    else
                    {
                        var pred = NewPrediction.Prediction.GetPrediction(
                            new NewPrediction.PredictionInput(
                                JadeHero.WorldPosition, targetE, EProjSpeed, EProjRange, 0f, 0f), true);

                        if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                        {
                            LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                            LocalPlayer.CastAbility(AbilitySlot.Ability5);
                            lastAbilityFired = AbilitySlot.Ability5;
                        }
                    }
                }
            }
            else
            {
                targetE = null;
            }

            if (castingSomething)
            {
                switch (lastAbilityFired)
                {
                    case AbilitySlot.UltimateAbility:
                        if (targetM2_F != null)
                        {
                            if (!useNewPred)
                            {
                                var pred = Prediction.GetPrediction(JadeHero, targetM2_F, FProjSpeed, FProjRange, 0f, SkillType.Line, 0, normalCollisions);

                                if (pred.HitChancePercent >= 25f)
                                {
                                    LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                                }
                            }
                            else
                            {
                                var pred = NewPrediction.Prediction.GetPrediction(
                                    new NewPrediction.PredictionInput(
                                        JadeHero.WorldPosition, targetM2_F, FProjSpeed, FProjRange, 0f, 0f), true);

                                if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                                {
                                    LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                                }
                            }
                        }
                        break;
                    case AbilitySlot.EnergyAbility:
                        if (targetR != null)
                        {
                            if (!useNewPred)
                            {
                                var pred = Prediction.GetPrediction(JadeHero, targetR, RProjSpeed, RProjRange, 0f, SkillType.Line, 0, normalCollisions);

                                if (pred.HitChancePercent >= 25f)
                                {
                                    LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                                }
                            }
                            else
                            {
                                var pred = NewPrediction.Prediction.GetPrediction(
                                    new NewPrediction.PredictionInput(
                                        JadeHero.WorldPosition, targetR, RProjSpeed, RProjRange, 0f, 0f), true);

                                if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                                {
                                    LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                                }
                            }
                        }
                        break;
                    case AbilitySlot.Ability2:
                        if (targetM2_F != null)
                        {
                            if (!useNewPred)
                            {
                                var pred = Prediction.GetPrediction(JadeHero, targetM2_F, M2ProjSpeed, M2ProjRange, 0f, SkillType.Line, 0, normalCollisions);

                                if (pred.HitChancePercent >= 25f)
                                {
                                    LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                                }
                            }
                            else
                            {
                                var pred = NewPrediction.Prediction.GetPrediction(
                                    new NewPrediction.PredictionInput(
                                        JadeHero.WorldPosition, targetM2_F, M2ProjSpeed, M2ProjRange, 0f, 0f), true);

                                if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                                {
                                    LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                                }
                            }
                        }
                        break;
                    case AbilitySlot.EXAbility1:
                        if (targetM2_F != null)
                        {
                            if (!useNewPred)
                            {
                                var pred = Prediction.GetPrediction(JadeHero, targetM2_F, M2ProjSpeed, M2ProjRange, 0f, SkillType.Line, 0, normalCollisions);

                                if (pred.HitChancePercent >= 25f)
                                {
                                    LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                                }
                            }
                            else
                            {
                                var pred = NewPrediction.Prediction.GetPrediction(
                                    new NewPrediction.PredictionInput(
                                        JadeHero.WorldPosition, targetM2_F, M2ProjSpeed, M2ProjRange, 0f, 0f), true);

                                if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                                {
                                    LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                                }
                            }
                        }
                        break;
                }
            }
            else
            {
                lastAbilityFired = null;
            }

            if (JadeMenu.GetBoolean("combo.useF") && MyUtils.CanCastAbility(AbilitySlot.UltimateAbility) && lastAbilityFired == null && targetM2_F != null)
            {
                if (!useNewPred)
                {
                    var pred = Prediction.GetPrediction(JadeHero, targetM2_F, FProjSpeed, FProjRange, 0f, SkillType.Line, 0, normalCollisions);

                    if (pred.HitChancePercent >= 25f)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.UltimateAbility);
                        lastAbilityFired = AbilitySlot.UltimateAbility;
                    }
                }
                else
                {
                    var pred = NewPrediction.Prediction.GetPrediction(
                        new NewPrediction.PredictionInput(
                            JadeHero.WorldPosition, targetM2_F, FProjSpeed, FProjRange, 0f, 0f), true);

                    if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.UltimateAbility);
                        lastAbilityFired = AbilitySlot.UltimateAbility;
                    }
                }
            }
            
            if (JadeMenu.GetBoolean("combo.useR") && MyUtils.CanCastAbility(AbilitySlot.EnergyAbility) && JadeHero.Energy >= 50f && lastAbilityFired == null && targetR != null)
            {
                if (!useNewPred)
                {
                    var pred = Prediction.GetPrediction(JadeHero, targetR, RProjSpeed, RProjRange, 0f, SkillType.Line, 0, normalCollisions);

                    if (pred.HitChancePercent >= 25f)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.EnergyAbility);
                        lastAbilityFired = AbilitySlot.EnergyAbility;
                    }
                }
                else
                {
                    var pred = NewPrediction.Prediction.GetPrediction(
                        new NewPrediction.PredictionInput(
                            JadeHero.WorldPosition, targetR, RProjSpeed, RProjRange, 0f, 0f), true);

                    if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.EnergyAbility);
                        lastAbilityFired = AbilitySlot.EnergyAbility;
                    }
                }
            }

            if (JadeMenu.GetBoolean("combo.useM2") && MyUtils.CanCastAbility(AbilitySlot.Ability2) && JadeHero.EnemiesAround(6.5f) == 0 && lastAbilityFired == null && targetM2_F != null)
            {
                if (!useNewPred)
                {
                    var pred = Prediction.GetPrediction(JadeHero, targetM2_F, M2ProjSpeed, M2ProjRange, 0f, SkillType.Line, 0, normalCollisions);

                    if (pred.HitChancePercent >= 25f)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.Ability2);
                        lastAbilityFired = AbilitySlot.Ability2;
                    }
                }
                else
                {
                    var pred = NewPrediction.Prediction.GetPrediction(
                                    new NewPrediction.PredictionInput(
                                        JadeHero.WorldPosition, targetM2_F, M2ProjSpeed, M2ProjRange, 0f, 0f), true);

                    if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.Ability2);
                        lastAbilityFired = AbilitySlot.Ability2;
                    }
                }
            }

            if (JadeMenu.GetBoolean("combo.useEX1") && MyUtils.CanCastAbility(AbilitySlot.Ability2) && JadeHero.Energy >= 50f && lastAbilityFired == null && targetM2_F != null)
            {
                if (!useNewPred)
                {
                    var pred = Prediction.GetPrediction(JadeHero, targetM2_F, M2ProjSpeed, M2ProjRange, 0f, SkillType.Line, 0, normalCollisions);

                    if (pred.HitChancePercent >= 25f)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.EXAbility1);
                        lastAbilityFired = AbilitySlot.EXAbility1;
                    }
                }
                else
                {
                    var pred = NewPrediction.Prediction.GetPrediction(
                                    new NewPrediction.PredictionInput(
                                        JadeHero.WorldPosition, targetM2_F, M2ProjSpeed, M2ProjRange, 0f, 0f), true);

                    if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.EXAbility1);
                        lastAbilityFired = AbilitySlot.EXAbility1;
                    }
                }
            }

            if (JadeMenu.GetBoolean("combo.useM1") && lastAbilityFired == null && targetM1 != null)
            {
                if (!useNewPred)
                {
                    var pred = Prediction.GetPrediction(JadeHero, targetM1, M1ProjSpeed, M1ProjRange, 0f, SkillType.Line, 0, normalCollisions);

                    if (pred.HitChancePercent >= 25f)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MoveMousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.Ability1, 10);
                    }
                }
                else
                {
                    var pred = NewPrediction.Prediction.GetPrediction(
                        new NewPrediction.PredictionInput(
                            JadeHero.WorldPosition, targetM1, M1ProjSpeed, M1ProjRange, 0f, 0f), true);

                    if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                    {
                        LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                        LocalPlayer.CastAbility(AbilitySlot.Ability1, 10);
                    }
                }
            }
        }

        private static void OrbStealerMode()
        {
            var orb = EntitiesManager.CenterOrb;

            if (orb.Health <= 0)
            {
                return;
            }

            LocalPlayer.UpdateCursorPosition(orb);

            if (Vector2.Distance(JadeHero.WorldPosition, orb.WorldPosition) <= M2ProjRange)
            {
                if (MyUtils.CanCastAbility(AbilitySlot.Ability2))
                {
                    if (orb.Health <= 38 && JadeHero.EnemiesAround(6f) == 0)
                    {
                        LocalPlayer.CastAbility(AbilitySlot.Ability2);
                    }

                    if (orb.Health <= 12f && JadeHero.Energy >= 25f)
                    {
                        LocalPlayer.CastAbility(AbilitySlot.EXAbility1);
                    }
                }
            }

            if (Vector2.Distance(JadeHero.WorldPosition, orb.WorldPosition) <= M1ProjRange)
            {
                if (orb.EnemiesAround(8f) == 0)
                {
                    LocalPlayer.CastAbility(AbilitySlot.Ability1, 10);
                }
                else
                {
                    if (orb.Health <= 6 * 4 || orb.Health >= 6 * 4 + (6 * 4 / 2))
                    {
                        LocalPlayer.CastAbility(AbilitySlot.Ability1, 10);
                    }
                }
            }
        }

        private static void OnDraw(EventArgs e)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (JadeHero.CharName != "Gunner")
            {
                return;
            }

            if (JadeMenu.GetBoolean("draw.rangeM1"))
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, M1ProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.red);
            }

            if (JadeMenu.GetBoolean("draw.rangeM2"))
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, M2ProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.red);
            }

            if (JadeMenu.GetBoolean("draw.rangeE"))
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, EProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.red);
            }

            if (JadeMenu.GetBoolean("draw.rangeR"))
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, RProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.red);
            }

            if (JadeMenu.GetBoolean("draw.rangeF"))
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, FProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.magenta);
            }

            if (JadeDebugMode)
            {
                if (JadeMenu.GetBoolean("debug.newPrediction"))
                {
                    if (EntitiesManager.EnemyTeam.Any())
                    {
                        foreach (var enemy in EntitiesManager.EnemyTeam)
                        {
                            if (enemy.IsValid && !enemy.IsImmaterial && !enemy.IsDead)
                            {
                                var pred = NewPrediction.Prediction.GetPrediction(
                                    new NewPrediction.PredictionInput(
                                        JadeHero.WorldPosition, enemy, M1ProjSpeed, M1ProjRange, 0f, 0f), true);

                                Drawing.DrawCircle(pred.TargetPosition, 1f, UnityEngine.Color.cyan);
                            }
                        }
                    }
                }

                if (myLastProj != null)
                {
                    Drawing.DrawCircle(myLastProj.WorldPosition, 5f, UnityEngine.Color.green);
                }
            }
        }

        private static void LoadMenu()
        {

        }

        private static void DebugStuff()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Keypad5))
            {
                Console.WriteLine(LocalPlayer.AbilitesData.Count);
                for (int i = 0; i < LocalPlayer.AbilitesData.Count; i++)
                {
                    var abilityData = LocalPlayer.AbilitesData[i];

                    Console.WriteLine(String.Format("Index: {0}", i));
                    Console.WriteLine(String.Format("Slot: {0}", abilityData.Slot));
                    Console.WriteLine(String.Format("SlotIndex: {0}", abilityData.SlotIndex));
                    Console.WriteLine(String.Format("Name: {0}", abilityData.IconName));
                    //Console.WriteLine(String.Format("Cooldown Time: {0}", abilityData.CooldownTime));
                    Console.WriteLine(String.Empty);
                }
            }

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Keypad8))
            {
                var allProjectiles = EntitiesManager.ActiveProjectiles.Where(x => x.TeamId == JadeHero.TeamId);

                if (allProjectiles.Any())
                {
                    foreach (var proj in allProjectiles)
                    {
                        Console.WriteLine(String.Format("Projectile: {0}", proj.ObjectName));
                        Console.WriteLine(String.Format("Generation: {0} - Index: {1}", proj.Generation, proj.Index));
                        Console.WriteLine(String.Format("Range: {0}", proj.Range));
                        Console.WriteLine(String.Empty);
                    }
                }
            }

            if (JadeMenu.GetBoolean("debug.projSpeed"))
            {
                myLastProj = EntitiesManager.ActiveProjectiles.Where(x => x.TeamId == JadeHero.TeamId).Last();

                if (myLastProj != null)
                {
                    var distance = Vector2.Distance(myLastProj.WorldPosition, myLastProjPreviousPos);
                    var time = UnityEngine.Time.deltaTime;
                    var speed = distance / time;

                    Console.WriteLine(String.Format("Projectile of name {0} has speed {1}", myLastProj.ObjectName, speed));

                    myLastProjPreviousPos = myLastProj.WorldPosition;
                }
                else
                {
                    myLastProjPreviousPos = Vector2.Zero;
                }
            }
        }
    }
}
