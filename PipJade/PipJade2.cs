//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//using BattleRight.Core;
//using BattleRight.Core.Models;
//using BattleRight.Core.Enumeration;
//using BattleRight.Core.GameObjects;
//using BattleRight.Core.Math;

//using BattleRight.SDK;
//using BattleRight.SDK.Enumeration;
//using BattleRight.SDK.Events;
//using BattleRight.SDK.UI;
//using BattleRight.SDK.UI.Models;
//using BattleRight.SDK.UI.Values;

//using PipLibrary.Utils;
//using PipLibrary.Extensions;

//namespace PipJade
//{
//    internal static class PipJade2
//    {
//        private static Menu JadeMenu;
//        private static Player JadeHero;

//        private const float M1Speed = 17f;
//        private const float M2Speed = 30f;
//        private const float ESpeed = 30f;
//        private const float RSpeed = 20f;
//        private const float FSpeed = 22.5f;

//        private const float M1Range = 7f;
//        private const float M2Range = 11.5f;
//        private const float SpaceRange = 4f;
//        private const float ERange = 9.5f;
//        private const float RRange = 5f;
//        private const float FRange = 11.6f;

//        private const float M1Radius = 0f;
//        private const float M2Radius = 0f;
//        private const float ERadius = 0f;
//        private const float RRadius = 0f;
//        private const float FRadius = 0f;

//        private static AbilitySlot? LastAbilityFired = null;

//        private static Player InterruptTarget = null;

//        public static void Init()
//        {
//            var _jadeMenu = new Menu("pipjademenu", "DaPipex's Jade");

//            _jadeMenu.AddLabel("Keys");
//            _jadeMenu.Add(new MenuKeybind("keys.combo", "Combo mode", UnityEngine.KeyCode.LeftControl, false, false));
//            _jadeMenu.Add(new MenuKeybind("keys.misc", "Misc mode", UnityEngine.KeyCode.LeftControl, false, false));
//            _jadeMenu.Add(new MenuKeybind("keys.orb", "Orb mode", UnityEngine.KeyCode.Mouse3, false, false));
//            _jadeMenu.Add(new MenuKeybind("keys.changeTargeting", "Change Targeting mode", UnityEngine.KeyCode.T, false, true));

//            _jadeMenu.AddSeparator();

//            _jadeMenu.AddLabel("Combo");
//            _jadeMenu.Add(new MenuCheckBox("combo.useM1", "Use Left Mouse (Revolver Shot)", true));
//            _jadeMenu.Add(new MenuCheckBox("combo.useM2", "Use Right Mouse (Snipe) when in safe range", true));
//            _jadeMenu.Add(new MenuSlider("combo.useM2.safeRange", "    ^ Safe range", 7f, M2Range - 1f, 0f));
//            _jadeMenu.Add(new MenuCheckBox("combo.useR", "Use R (Junk Shot)", true));
//            _jadeMenu.Add(new MenuIntSlider("combo.useR.minEnergyBars", "    ^ Min energy bars", 2, 4, 1));
//            _jadeMenu.Add(new MenuCheckBox("combo.useEX1", "Use EX1 (Snap Shot)", true));
//            _jadeMenu.Add(new MenuIntSlider("combo.useEX1.minEnergyBars", "    ^ Min energy bars", 2, 4, 1));
//            _jadeMenu.Add(new MenuCheckBox("combo.useF", "Use F (Explosive Shells)", true));

//            _jadeMenu.AddSeparator();

//            _jadeMenu.AddLabel("Misc");
//            _jadeMenu.Add(new MenuCheckBox("misc.useE", "Use E (Disabling Shot) to interrupt", true));
//            _jadeMenu.Add(new MenuCheckBox("misc.useSpace", "Use Space (Blast Vault) when enemies are too close", false));

//            _jadeMenu.AddSeparator();

//            _jadeMenu.AddLabel("Drawings");
//            _jadeMenu.Add(new MenuCheckBox("draw.rangeM1", "Draw Left Mouse Range (Revolver Shot)", false));
//            _jadeMenu.Add(new MenuCheckBox("draw.rangeM2", "Draw Right Mouse Range (Snipe)", true));
//            _jadeMenu.Add(new MenuCheckBox("draw.rangeM2.safeRange", "Draw Right Mouse Safe-Range (Snipe)", true));
//            _jadeMenu.Add(new MenuCheckBox("draw.rangeE", "Draw E Range (Disabling Shot)", false));
//            _jadeMenu.Add(new MenuCheckBox("draw.rangeR", "Draw R Range (Junk Shot)", true));
//            _jadeMenu.Add(new MenuCheckBox("draw.rangeF", "Draw F Range (Explosive Shells)", false));
//            _jadeMenu.Add(new MenuCheckBox("draw.escapeSkillsScreen", "Draw escape skills CDs on screen", true));

//            MainMenu.AddMenu(_jadeMenu);

//            //CustomEvents.Instance.OnUpdate += delegate (EventArgs args)
//            //{
//            //    if (!Game.IsInGame)
//            //    {
//            //        return;
//            //    }

//            //    JadeMenu = _jadeMenu;
//            //    JadeHero = EntitiesManager.LocalPlayer;

//            //    OnUpdate(args);
//            //};
//            //CustomEvents.Instance.OnDraw += OnDraw;
//        }

//        private static void OnUpdate(EventArgs args)
//        {
//            if (JadeHero.CharName != "Gunner")
//            {
//                return;
//            }

//            if (JadeMenu.GetKeybind("keys.combo"))
//            {
//                ComboMode();
//            }

//            if (JadeMenu.GetKeybind("keys.misc"))
//            {
//                //MiscMode();
//            }

//            if (JadeMenu.GetKeybind("keys.orb"))
//            {
//                OrbMode();
//            }

//            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.G))
//            {
//                Console.WriteLine("Enemy Count: " + EntitiesManager.EnemyTeam.Length);
//                Console.WriteLine("Ally Count: " + EntitiesManager.LocalTeam.Length);
//            }
//        }

//        private static void ComboMode()
//        {
//            var targetMode = JadeMenu.GetKeybind("keys.changeTargeting");
//            var M1Target = TargetSelector.GetTarget(targetMode ? TargetingMode.LowestHealth : TargetingMode.NearLocalPlayer, M1Range);
//            var M2_FTarget = TargetSelector.GetTarget(targetMode ? TargetingMode.LowestHealth : TargetingMode.NearLocalPlayer, M2Range);
//            var RTarget = TargetSelector.GetTarget(targetMode ? TargetingMode.LowestHealth : TargetingMode.NearLocalPlayer, RRange);

//            bool IsCastingOrChanneling = JadeHero.IsCasting || JadeHero.IsChanneling;

//            if (IsCastingOrChanneling)
//            {
//                switch (LastAbilityFired)
//                {
//                    case AbilitySlot.Ability7:
//                        if (M2_FTarget != null)
//                        {
//                            var skillType = NewPrediction.Enumerations.SkillType.Line;
//                            var collisionObjects = CollisionFlags.InvisWalls | CollisionFlags.Bush | CollisionFlags.NPCBlocker;

//                            var pred = NewPrediction.Prediction.GetPrediction(
//                                new NewPrediction.PredictionInput(
//                                    JadeHero.WorldPosition, M2_FTarget, FSpeed, FRange, 0f, FRadius, skillType, collisionObjects), true);

//                            if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
//                            {
//                                LocalPlayer.UpdateCursorPosition(pred.MousePosition);
//                            }
//                        }
//                        break;

//                    case AbilitySlot.Ability6:
//                        if (RTarget != null)
//                        {
//                            var skillType = NewPrediction.Enumerations.SkillType.Line;
//                            var collisionObjects = CollisionFlags.InvisWalls | CollisionFlags.Bush | CollisionFlags.NPCBlocker;

//                            var pred = NewPrediction.Prediction.GetPrediction(
//                                new NewPrediction.PredictionInput(
//                                    JadeHero.WorldPosition, RTarget, RSpeed, RRange, 0f, RRadius, skillType, collisionObjects), true);

//                            if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
//                            {
//                                LocalPlayer.UpdateCursorPosition(pred.MousePosition);
//                            }
//                        }
//                        break;

//                    case AbilitySlot.Ability2:
//                    case AbilitySlot.EXAbility1:
//                        if (M2_FTarget != null)
//                        {
//                            var skillType = NewPrediction.Enumerations.SkillType.Line;
//                            var collisionObjects = CollisionFlags.InvisWalls | CollisionFlags.Bush | CollisionFlags.NPCBlocker;

//                            var pred = NewPrediction.Prediction.GetPrediction(
//                                new NewPrediction.PredictionInput(
//                                    JadeHero.WorldPosition, M2_FTarget, M2Speed, M2Range, 0f, M2Radius, skillType, collisionObjects), true);

//                            if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
//                            {
//                                LocalPlayer.UpdateCursorPosition(pred.MousePosition);
//                            }
//                        }
//                        break;
//                }
//            }
//            else
//            {
//                LastAbilityFired = null;
//            }

//            if (JadeMenu.GetBoolean("combo.useF") && MiscUtils.CanCast(AbilitySlot.Ability7))
//            {
//                if (LastAbilityFired == null && M2_FTarget != null)
//                {
//                    LocalPlayer.PressAbility(AbilitySlot.Ability7, true);
//                    LastAbilityFired = AbilitySlot.Ability7;
//                }
//            }

//            if (JadeMenu.GetBoolean("combo.useR") && MiscUtils.CanCast(AbilitySlot.Ability6))
//            {
//                var energyRequired = JadeMenu.GetIntSlider("combo.useR.minEnergyBars") * 25;
//                if (energyRequired <= JadeHero.Energy)
//                {
//                    if (LastAbilityFired == null && RTarget != null)
//                    {
//                        LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
//                        LastAbilityFired = AbilitySlot.Ability6;
//                    }
//                }
//            }

//            if (JadeMenu.GetBoolean("combo.useM2") && MiscUtils.CanCast(AbilitySlot.Ability2))
//            {
//                if (JadeHero.EnemiesAround(JadeMenu.GetSlider("combo.useM2.safeRange")) == 0)
//                {
//                    if (LastAbilityFired == null && M2_FTarget != null)
//                    {
//                        LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
//                        LastAbilityFired = AbilitySlot.Ability2;
//                    }
//                }
//            }

//            if (JadeMenu.GetBoolean("combo.useEX1") && MiscUtils.CanCast(AbilitySlot.EXAbility1))
//            {
//                var energyRequired = JadeMenu.GetIntSlider("combo.useEX1.minEnergyBars") * 25;
//                if (energyRequired <= JadeHero.Energy)
//                {
//                    if (LastAbilityFired == null && M2_FTarget != null)
//                    {
//                        LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
//                        LastAbilityFired = AbilitySlot.EXAbility1;
//                    }
//                }
//            }

//            if (JadeMenu.GetBoolean("combo.useM1") && MiscUtils.CanCast(AbilitySlot.Ability1))
//            {
//                if (LastAbilityFired == null && M1Target != null)
//                {
//                    var pred = NewPrediction.Prediction.GetPrediction(
//                        new NewPrediction.PredictionInput(
//                            JadeHero.WorldPosition, M1Target, M1Speed, M1Range, 0f, M1Radius), true);

//                    if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
//                    {
//                        LocalPlayer.UpdateCursorPosition(pred.MousePosition);
//                        LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
//                        //LastAbilityFired = AbilitySlot.Ability1;
//                    }
//                }
//            }
//        }

//        private static void MiscMode()
//        {
//            bool IsCastingOrChanneling = JadeHero.IsCasting || JadeHero.IsChanneling;

//            if (IsCastingOrChanneling)
//            {
//                switch (LastAbilityFired)
//                {
//                    case AbilitySlot.Ability5:
//                        if (InterruptTarget != null)
//                        {
//                            var skillType = NewPrediction.Enumerations.SkillType.Line;
//                            var collisionObjects = CollisionFlags.InvisWalls | CollisionFlags.InvisWalls | CollisionFlags.Bush;

//                            var pred = NewPrediction.Prediction.GetPrediction(
//                                new NewPrediction.PredictionInput(
//                                    JadeHero.WorldPosition, InterruptTarget, ESpeed, ERange, 0f, ERadius, skillType, collisionObjects), true);

//                            if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
//                            {
//                                LocalPlayer.UpdateCursorPosition(pred.MousePosition);
//                            }
//                        }
//                        break;
//                }
//            }
//            else
//            {
//                LastAbilityFired = null;
//            }

//            if (JadeMenu.GetBoolean("misc.useE") && MiscUtils.CanCast(AbilitySlot.Ability5))
//            {
//                if (InterruptTarget == null)
//                {
//                    foreach (var enemy in EntitiesManager.EnemyTeam)
//                    {
//                        if (enemy.IsValid && !enemy.IsImmaterial && !enemy.IsCountering && !enemy.IsDead && (enemy.IsCasting || enemy.IsChanneling))
//                        {
//                            if (JadeHero.Distance(enemy) <= ERange)
//                            {
//                                InterruptTarget = enemy;
//                                break;
//                            }
//                        }
//                    }
//                }

//                if (InterruptTarget != null)
//                {
//                    LocalPlayer.PressAbility(AbilitySlot.Ability5, true);
//                    LastAbilityFired = AbilitySlot.Ability5;
//                }
//            }

//            if (JadeMenu.GetBoolean("misc.useSpace") && MiscUtils.CanCast(AbilitySlot.Ability3))
//            {
//                if (JadeHero.EnemiesAround(2f) > 0)
//                {
//                    LocalPlayer.PressAbility(AbilitySlot.Ability3, true);
//                }
//            }
//        }

//        private static void OrbMode()
//        {
//            var orb = EntitiesManager.CenterOrb;
//            if (orb.Health <= 0)
//            {
//                return;
//            }

//            LocalPlayer.UpdateCursorPosition(orb);

//            if (JadeHero.Distance(orb) <= M2Range)
//            {
//                if (MiscUtils.CanCast(AbilitySlot.EXAbility1) && orb.Health <= 12f)
//                {
//                    LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
//                }
//                else if (MiscUtils.CanCast(AbilitySlot.Ability2) && orb.Health <= 38f)
//                {
//                    LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
//                }
//            }

//            if (JadeHero.Distance(orb) <= M1Range)
//            {
//                if (MiscUtils.CanCast(AbilitySlot.Ability1))
//                {
//                    if (orb.EnemiesAround(7f) == 0)
//                    {
//                        LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
//                    }
//                    else
//                    {
//                        if (orb.Health <= 6 * 4 || orb.Health >= 6 * 4 + (6 * 4 / 2))
//                        {
//                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
//                        }
//                    }
//                }
//            }
//        }

//        private static void OnDraw(EventArgs args)
//        {
//            if (!Game.IsInGame)
//            {
//                return;
//            }

//            if (JadeHero.CharName != "Gunner")
//            {
//                return;
//            }

//            Drawing.DrawString(new Vector2(1920f / 2f, 1080f / 2f - 5f).TempScreenToWorld()
//                , "Targeting Mode: " + (JadeMenu.GetKeybind("keys.changeTargeting") ? "LowestHealth" : "NearMe")
//                , UnityEngine.Color.yellow);

//            if (JadeMenu.GetBoolean("draw.rangeM1"))
//            {
//                Drawing.DrawCircle(JadeHero.WorldPosition, M1Range, UnityEngine.Color.red);
//            }

//            if (JadeMenu.GetBoolean("draw.rangeM2"))
//            {
//                Drawing.DrawCircle(JadeHero.WorldPosition, M2Range, UnityEngine.Color.red);
//            }

//            if (JadeMenu.GetBoolean("draw.rangeM2.safeRange"))
//            {
//                var range = JadeMenu.GetSlider("combo.useM2.safeRange");
//                Drawing.DrawCircle(JadeHero.WorldPosition, range, new UnityEngine.Color(1f, 0.75f, 0f)); //Orange
//            }

//            if (JadeMenu.GetBoolean("draw.rangeE"))
//            {
//                Drawing.DrawCircle(JadeHero.WorldPosition, ERange, UnityEngine.Color.magenta);
//            }

//            if (JadeMenu.GetBoolean("draw.rangeR"))
//            {
//                Drawing.DrawCircle(JadeHero.WorldPosition, RRange, UnityEngine.Color.red);
//            }

//            if (JadeMenu.GetBoolean("draw.rangeF"))
//            {
//                Drawing.DrawCircle(JadeHero.WorldPosition, FRange, UnityEngine.Color.red);
//            }

//            //if (JadeMenu.GetBoolean("draw.escapeSkillsScreen"))
//            //{
//            //    var drawSpacePos = new Vector2(760f, 1080f - 350f);
//            //    var abilitySpace = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability3);
//            //    var abilitySpaceReady = MiscUtils.CanCast(AbilitySlot.Ability3);
//            //    var textToDrawSpace = "Space state: " + (abilitySpaceReady ? "Ready" : Math.Round(abilitySpace.CooldownTime, 2).ToString());

//            //    Drawing.DrawString(drawSpacePos.TempScreenToWorld(), textToDrawSpace, abilitySpaceReady ? UnityEngine.Color.green : UnityEngine.Color.red);

//            //    var drawQPos = new Vector2(1920f - 760f, 1080f - 350f);
//            //    var abilityQ = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability4);
//            //    var abilityQReady = MiscUtils.CanCast(AbilitySlot.Ability4);
//            //    var textToDrawQ = "Q state: " + (abilityQReady ? "Ready" : Math.Round(abilityQ.CooldownTime, 2).ToString());

//            //    Drawing.DrawString(drawQPos.TempScreenToWorld(), textToDrawQ, abilityQReady ? UnityEngine.Color.green : UnityEngine.Color.red);
//            //}
//        }
//    }
//}
