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
        private static readonly string HeroCharName = "Gunner";

        private static Menu JadeMenu = null;

        private static Player JadeHero;

        private static AbilityData Mouse1Ability;
        private static AbilityData Mouse2Ability;
        private static AbilityData SpaceAbility;
        private static AbilityData EAbility;
        private static AbilityData RAbility;
        private static AbilityData EX1Ability;
        private static AbilityData FAbility;

        //TEMP MENU
        //Combo
        private static bool comboUseM1;
        private static bool comboUseM2;
        private static bool comboUseSpace;
        private static bool comboUseR;
        private static bool comboUseEX1;
        private static bool comboUseF;
        private static bool comboUseNewPred;

        //Misc
        private static bool miscUseE;

        //Drawings
        private static bool drawRangeM1;
        private static bool drawRangeM2;
        private static bool drawRangeSpace;
        private static bool drawRangeE;
        private static bool drawRangeR;
        private static bool drawRangeF;

        //Debug
        private static bool debugProjSpeed;
        private static float debugProjCheckInterval;
        private static bool debugProjRange;
        private static bool debugNewPrediction;
        //TEMP MENU

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
        private static int myLastProjAnalyseTimes = 0;
        private static Vector2 myLastProjPos1 = Vector2.Zero;
        private static Vector2 myLastProjPos2 = Vector2.Zero;
        private static int myLastProjLastExamineT = 0;
        private static int myLastProjLastExamineT2 = 0;
        private static int myLastProjMeasureIntervalMs = 250;

        private static Vector2 myLastProjPreviousPos = Vector2.Zero;

        private static bool JustFiredF = false;
        private static bool JustFiredM2 = false;
        private static bool JustFiredEX1 = false;

        private static float JustFiredF_Time = 0;
        private static float JustFiredM2_Time = 0;
        private static float JustFiredEX1_Time = 0;

        private const float JustFiredF_Casting = 0.6f;
        private const float JustFiredF_Channeling = 1.5f;
        private const float JustFiredM2_Casting = 1.6f;
        private const float JustFiredEX1_Casting = 0.4f;

        private static AbilitySlot? lastAbilityFired = null;

        private static readonly CollisionFlags normalCollisions = CollisionFlags.NPCBlocker | CollisionFlags.Bush | CollisionFlags.InvisWalls;

        private static List<MenuItem> children = new List<MenuItem>();
        //HELPERS

        public static void Init()
        {
            //LoadMenu();
            var _jadeMenu = MainMenu.AddMenu(new Menu("pipjademenu", "DaPipex's Jade"));
            //Console.WriteLine("Added to lmao menu!");

            _jadeMenu.AddLabel("Basic Combo");
            var mComboUseM1 = _jadeMenu.Add(new MenuCheckBox("combo.useM1", "Use Left-Mouse (Revolver Shot)", true));
            var mComboUseM2 = _jadeMenu.Add(new MenuCheckBox("combo.useM2", "Use Right-Mouse (Snipe)", true));
            //var mComboUseSpace = _jadeMenu.Add(new MenuCheckBox("combo.useSpace", "Use Space (Blast Vault)", false));
            var mComboUseR = _jadeMenu.Add(new MenuCheckBox("combo.useR", "Use R (Junk Shot)", true));
            var mComboUseEX1 = _jadeMenu.Add(new MenuCheckBox("combo.useEX1", "Use EX1 (Snap Shot)", true));
            var mComboUseF = _jadeMenu.Add(new MenuCheckBox("combo.useF", "Use F (Explosive Shells)", true));
            var mComboUseNewPred = _jadeMenu.Add(new MenuCheckBox("combo.useNewPred", "Use new prediction (EXPERIMENTAL)", false));

            _jadeMenu.AddSeparator(10f);

            _jadeMenu.AddLabel("Misc");
            var mMiscUseE = _jadeMenu.Add(new MenuCheckBox("misc.useE", "Use E (Disabling Shot) to interrupt", true));

            _jadeMenu.AddSeparator(10f);

            _jadeMenu.AddLabel("Drawings");
            var mDrawRangeM1 = _jadeMenu.Add(new MenuCheckBox("draw.rangeM1", "Draw Left-Mouse range (Revolver Shot)", false)); //true
            var mDrawRangeM2 = _jadeMenu.Add(new MenuCheckBox("draw.rangeM2", "Draw Right-Mouse range (Snipe)", true));
            //var mDrawRangeSpace = _jadeMenu.Add(new MenuCheckBox("draw.rangeSpace", "Draw Space Range (Blast vault)", false));
            var mDrawRangeE = _jadeMenu.Add(new MenuCheckBox("draw.rangeE", "Draw E range (Disabling Shot)", true));
            var mDrawRangeR = _jadeMenu.Add(new MenuCheckBox("draw.rangeR", "Draw R range (Junk Shot)", false));
            var mDrawRangeF = _jadeMenu.Add(new MenuCheckBox("draw.rangeF", "Draw F range (Explosive Shells)", false));

            _jadeMenu.AddSeparator(10f);

            _jadeMenu.AddLabel("Debug");
            var mDebugProjSpeed = _jadeMenu.Add(new MenuCheckBox("debug.projSpeed", "My last projectile's speed", false));
            var mDebugProjCheckInterval = _jadeMenu.Add(new MenuSlider("debug.projSpeedInterval", "^ Check interval", 200f, 250f, 60f));
            var mDebugProjRange = _jadeMenu.Add(new MenuCheckBox("debug.projRange", "My last projectile's range", false));
            var mDebugNewPrediction = _jadeMenu.Add(new MenuCheckBox("debug.newPrediction", "Enemies Prediction", false));
            _jadeMenu.Add(new MenuCheckBox("debug.Test", "Menu test", false));

            //Console.WriteLine("Menu loaded!");

            CustomEvents.Instance.OnMatchStart += OnMatchStart;
            CustomEvents.Instance.OnDraw += OnDraw;
            CustomEvents.Instance.OnUpdate += delegate
            {
                comboUseM1 = mComboUseM1.CurrentValue;
                comboUseM2 = mComboUseM2.CurrentValue;
                //comboUseSpace = mComboUseSpace.CurrentValue;
                comboUseR = mComboUseR.CurrentValue;
                comboUseEX1 = mComboUseEX1.CurrentValue;
                comboUseF = mComboUseF.CurrentValue;
                comboUseNewPred = mComboUseNewPred.CurrentValue;

                miscUseE = mMiscUseE.CurrentValue;

                drawRangeM1 = mDrawRangeM1.CurrentValue;
                drawRangeM2 = mDrawRangeM2.CurrentValue;
                //drawRangeSpace = mDrawRangeSpace.CurrentValue;
                drawRangeE = mDrawRangeE.CurrentValue;
                drawRangeR = mDrawRangeR.CurrentValue;
                drawRangeF = mDrawRangeF.CurrentValue;

                debugProjSpeed = mDebugProjSpeed.CurrentValue;
                debugProjCheckInterval = mDebugProjCheckInterval.CurrentValue;
                debugNewPrediction = mDebugNewPrediction.CurrentValue;

                JadeHero = EntitiesManager.LocalPlayer;
                JadeMenu = _jadeMenu;

                OnUpdate();
            };
        }

        private static void OnMatchStart(EventArgs e)
        {
            JadeHero = EntitiesManager.LocalPlayer;

            if (JadeHero.CharName != HeroCharName)
            {
                return;
            }
        }

        private static void OnUpdate()
        {
            //if (JadeMenu == null)
            //{
            //    Console.WriteLine("JadeMenu is null");
            //}
            //else if (JadeMenu.Get<MenuCheckBox>("debug.Test").CurrentValue)
            //{
            //    Console.WriteLine("debug.Test is on!");
            //}
            //else
            //{
            //    Console.WriteLine("debug.Test is off!");
            //}

            if (!Game.IsInGame /*|| JadeHero.IsDead*/)
            {
                return;
            }

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

            //if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Keypad8))
            //{
            //    if (JadeMenu.Children.Any())
            //    {
            //        foreach (var child in JadeMenu.Children)
            //        {
            //            Console.WriteLine(child.Name);
            //            Console.WriteLine(child.DisplayName);
            //            Console.WriteLine(String.Empty);
            //        }
            //    }
            //}

            //Mouse1Ability = LocalPlayer.GetAbilityData(AbilitySlot.Ability1);
            //Mouse2Ability = LocalPlayer.GetAbilityData(AbilitySlot.Ability2);
            //SpaceAbility = LocalPlayer.GetAbilityData(AbilitySlot.Ability3);
            //EAbility = LocalPlayer.GetAbilityData(AbilitySlot.Ability5);
            //RAbility = LocalPlayer.GetAbilityData(AbilitySlot.EnergyAbility);
            //EX1Ability = LocalPlayer.GetAbilityData(AbilitySlot.EXAbility1);
            //FAbility = LocalPlayer.GetAbilityData(AbilitySlot.UltimateAbility);

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.LeftControl))
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

            if (debugProjSpeed)
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

            
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.Mouse3))
            {
                OrbStealerMode();
            }
            else if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl))
            {
                //Free aim
            }
            else
            {
                ComboMode();
            }

            //UpdateAbilitiesStates();
        }

        private static void UpdateAbilitiesStates()
        {
            //AbilityHudData M1AbilityHud = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability1);
            AbilityHudData M2AbilityHud = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability2);
            //AbilityHudData SpaceAbilityHud = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability3);
            //AbilityHudData EAbilityHud = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability5);
            //AbilityHudData RAbilityHud = LocalPlayer.GetAbilityHudData(AbilitySlot.EnergyAbility);
            AbilityHudData EX1AbilityHud = LocalPlayer.GetAbilityHudData(AbilitySlot.EXAbility1);
            AbilityHudData FAbilityHud = LocalPlayer.GetAbilityHudData(AbilitySlot.UltimateAbility);

            if (FAbilityHud.CooldownTime == 0 || UnityEngine.Time.time > JustFiredF_Time + JustFiredF_Casting + JustFiredF_Channeling)
            {
                JustFiredF = false;
            }

            if (M2AbilityHud.CooldownTime == 0 || UnityEngine.Time.time > JustFiredM2_Time + JustFiredM2_Casting)
            {
                JustFiredM2 = false;
            }

            if (EX1AbilityHud.CooldownTime == 0 || UnityEngine.Time.time > JustFiredEX1_Time + JustFiredEX1_Casting)
            {
                JustFiredEX1 = false;
            }
        }

        private static void ComboMode()
        {
            Player targetM1 = TargetSelector.GetTarget(TargetingMode.LowestHealth, M1ProjRange);
            Player targetM2_F = TargetSelector.GetTarget(TargetingMode.LowestHealth, M2ProjRange);
            Player targetR = TargetSelector.GetTarget(TargetingMode.LowestHealth, RProjRange);
            Player targetE = null;

            bool castingSomething = JadeHero.IsCasting || JadeHero.IsChanneling;

            if ((miscUseE && MyUtils.CanCastAbility(AbilitySlot.Ability5)) || (castingSomething && lastAbilityFired == AbilitySlot.Ability5))
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
                    if (!comboUseNewPred)
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
                            if (!comboUseNewPred)
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
                            if (!comboUseNewPred)
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
                            if (!comboUseNewPred)
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
                            if (!comboUseNewPred)
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
            //if (((comboUseF && MyUtils.CanCastAbility(AbilitySlot.UltimateAbility)) || ((JadeHero.IsCasting || JadeHero.IsChanneling) && JustFiredF)) && targetM2_F != null)
            if (comboUseF && MyUtils.CanCastAbility(AbilitySlot.UltimateAbility) && lastAbilityFired == null && targetM2_F != null)
            {
                if (!comboUseNewPred)
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
            //else if (comboUseR && MyUtils.CanCastAbility(AbilitySlot.EnergyAbility) && JadeHero.Energy >= 50f && targetR != null)
            if (comboUseR && MyUtils.CanCastAbility(AbilitySlot.EnergyAbility) && JadeHero.Energy >= 50f && lastAbilityFired == null && targetR != null)
            {
                if (!comboUseNewPred)
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
            //else if (((comboUseM2 && MyUtils.CanCastAbility(AbilitySlot.Ability2) && JadeHero.EnemiesAround(5f) == 0) || (JadeHero.IsCasting && JustFiredM2)) && targetM2_F != null)
            if (comboUseM2 && MyUtils.CanCastAbility(AbilitySlot.Ability2) && JadeHero.EnemiesAround(6.5f) == 0 && lastAbilityFired == null && targetM2_F != null)
            {
                if (!comboUseNewPred)
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
            //else if (((comboUseEX1 && MyUtils.CanCastAbility(AbilitySlot.EXAbility1) && JadeHero.Energy >= 50f) || (JadeHero.IsCasting && JustFiredEX1)) && targetM2_F != null)
            if (comboUseEX1 && MyUtils.CanCastAbility(AbilitySlot.Ability2) && JadeHero.Energy >= 50f && lastAbilityFired == null && targetM2_F != null)
            {
                if (!comboUseNewPred)
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
            /*else*/
            if (comboUseM1 && lastAbilityFired == null && targetM1 != null)
            {
                if (!comboUseNewPred)
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
                    if (orb.Health <= 6 * 4 || orb.Health >= 6 * 4 * 2)
                    {
                        LocalPlayer.CastAbility(AbilitySlot.Ability1, 10);
                    }
                }
            }
        }

        private static void OnDraw(EventArgs e)
        {
            if (!Game.IsInGame /*|| JadeHero.IsDead*/)
            {
                return;
            }

            if (myLastProj != null)
            {
                Drawing.DrawCircle(myLastProj.WorldPosition, 5f, UnityEngine.Color.green);
            }

            if (drawRangeM1)
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, M1ProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.red);
            }

            if (drawRangeM2)
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, M2ProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.red);
            }

            if (drawRangeSpace)
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, SpaceRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.red);
            }

            if (drawRangeE)
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, EProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.red);
            }

            if (drawRangeR)
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, RProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.red);
            }

            if (drawRangeF)
            {
                Drawing.DrawCircle(JadeHero.WorldPosition, FProjRange /*+ JadeHero.SpellCollisionRadius*/, UnityEngine.Color.magenta);
            }

            if (debugNewPrediction)
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
        }

        private static void LoadMenu()
        {

        }
    }
}
