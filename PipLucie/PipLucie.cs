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

using BattleRight.Sandbox;

using PipLibrary.Extensions;
using PipLibrary.Utils;

namespace PipLucie
{
    public class PipLucie : IAddon
    {
        private static Menu LucieMenu;
        private static Menu KeysMenu, ComboMenu, HealMenu, DrawingsMenu;

        private static Character LucieHero;

        private static AbilitySlot? LastAbilityFired = null;

        private static bool BackgroundTasks = false;

        private const float M1Range = 7.4f;
        private const float M2Range = 10f;
        private const float QRange = 8f;
        private const float ERange = 6.8f;
        private const float FRange = 7.5f;
        private const float EX1Range = 8.8f;
        private const float EX2Range = 9.8f;

        private const float M1Speed = 16.8f;
        private const float M2Speed = 28.6f;
        private const float QSpeed = 14.5f;
        private const float ESpeed = 22.5f;
        private const float EX1Speed = 23.9f;
        private const float EX2Speed = 22.5f;

        private const float M1Radius = 0.25f;
        private const float M2Radius = 1.2f;
        private const float QRadius = 2f;
        private const float ERadius = 0.35f;
        private const float FRadius = 3.5f;
        private const float EX1Radius = 0.35f;
        private const float EX2Radius = 0.35f;

        private static Character DebugTarget = null;

        public void OnInit()
        {
            LucieMenu = new Menu("pipluciemenu", "DaPip's Lucie");
            LucieMenu.Add(new MenuCheckBox("main.includePing", "Include ping in prediction?", false));

            KeysMenu = new Menu("keysmenu", "Keys", true);
            KeysMenu.Add(new MenuKeybind("keys.combo", "Combo Key", UnityEngine.KeyCode.LeftControl));
            KeysMenu.Add(new MenuKeybind("keys.healOthers", "Heal others", UnityEngine.KeyCode.Mouse3));
            KeysMenu.Add(new MenuKeybind("keys.healSelf", "Heal self", UnityEngine.KeyCode.G));
            KeysMenu.Add(new MenuKeybind("keys.changeTargeting", "Change targeting mode", UnityEngine.KeyCode.T, false, true));
            LucieMenu.Add(KeysMenu);

            ComboMenu = new Menu("combomenu", "Combo", true);
            ComboMenu.Add(new MenuCheckBox("combo.useM1", "Use Left Mouse (Toxic Bolt)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useQ", "Use Q (Clarity Potion) when enemies are too close", false));
            ComboMenu.Add(new MenuCheckBox("combo.useE", "Use E (Panic Flask)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useEX1", "Use EX1 (Deadly Injection)", false));
            ComboMenu.Add(new MenuIntSlider("combo.useEX1.minEnergyBars", "    ^ Min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useEX2", "Use EX2 (Petrify Bolt)", false));
            ComboMenu.Add(new MenuIntSlider("combo.useEX2.minEnergyBars", "    ^ Min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useF", "Use F (Crippling Goo)", true));
            LucieMenu.Add(ComboMenu);

            HealMenu = new Menu("healmenu", "Healing", true);
            HealMenu.AddLabel("Hold Healing key to use");
            HealMenu.Add(new MenuSlider("heal.allySafeRange", "Target ally safe range", 4f, 10f, 0f));
            HealMenu.Add(new MenuCheckBox("heal.useM2", "Heal with M2 (Healing Potion)", true));
            HealMenu.Add(new MenuCheckBox("heal.useM2.fullHP", "    ^ Use even if target ally has full health", true));
            HealMenu.Add(new MenuCheckBox("heal.useSpace", "Use Space (Barrier)", false));
            HealMenu.AddLabel("Runs automatically");
            HealMenu.Add(new MenuCheckBox("heal.useQ", "Use Q (Clarity Potion) to cleanse allies", true));
            LucieMenu.Add(HealMenu);

            DrawingsMenu = new Menu("drawmenu", "Drawings", true);
            DrawingsMenu.Add(new MenuCheckBox("draw.healSafeRange", "Draw healing safe range", true));
            LucieMenu.Add(DrawingsMenu);

            MainMenu.AddMenu(LucieMenu);

            Game.OnUpdate += OnUpdate;
            Game.OnDraw += OnDraw;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            LucieHero = EntitiesManager.LocalPlayer;

            if (LucieHero.CharName != "Lucie")
            {
                return;
            }

            LocalPlayer.EditAimPosition = false;

            HealOthersBackground();

            if (!BackgroundTasks)
            {
                if (KeysMenu.GetKeybind("keys.healOthers"))
                {
                    HealOthers();
                }
                else if (KeysMenu.GetKeybind("keys.healSelf"))
                {
                    HealSelf();
                }
                else if (KeysMenu.GetKeybind("keys.combo"))
                {
                    ComboMode();
                }
            }
        }

        private static void HealOthersBackground()
        {
            var teammates = EntitiesManager.LocalTeam.Where(x => !x.IsLocalPlayer && !x.Living.IsDead && !x.PhysicsCollision.IsImmaterial);

            var bestQ = teammates.Where(x => x.Buffs.Any(y => y.BuffType == BuffType.Debuff) && x.Distance(LucieHero) <= QRange)
                .OrderBy(x => x.Distance(LucieHero))
                .FirstOrDefault();

            var isCastingOrChanneling = LucieHero.AbilitySystem.IsCasting || LucieHero.IsChanneling;

            var latencyDelay = LucieHero.AbilitySystem.Latency / 2f;
            var finalDelay = LucieMenu.GetBoolean("main.includePing") ? latencyDelay : 0f;

            if (isCastingOrChanneling)
            {

                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability4:
                        if (bestQ != null && BackgroundTasks)
                        {
                            var pred = LucieHero.GetPrediction(bestQ, QSpeed, QRange, QRadius, SkillType.Circle, finalDelay, 0);

                            if (pred.HitChancePercent >= 30f)
                            {
                                LocalPlayer.EditAimPosition = true;
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;
                }
            }
            else
            {
                LocalPlayer.EditAimPosition = false;
                LastAbilityFired = null;
                BackgroundTasks = false;
            }

            if (HealMenu.GetBoolean("heal.useQ") && MiscUtils.CanCast(AbilitySlot.Ability4))
            {
                if (LastAbilityFired == null && bestQ != null)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
                    LastAbilityFired = AbilitySlot.Ability4;
                    BackgroundTasks = true;
                }
            }
        }

        private static void ComboMode()
        {
            var targetModeKey = KeysMenu.GetKeybind("keys.changeTargeting");
            var targetMode = targetModeKey ? TargetingMode.LowestHealth : TargetingMode.NearMouse;

            var targetM1 = TargetSelector.GetTarget(targetMode, M1Range);
            //var targetQ = EntitiesManager.EnemyTeam.Where(x => !x.IsDead && x.Buffs.Any(y => y.BuffType == BuffType.Buff) && x.Distance(LucieHero) <= QRange)
            //    .OrderBy(x => x.Distance(LucieHero))
            //    .FirstOrDefault();
            var targetQ = TargetSelector.GetTarget(TargetingMode.NearLocalPlayer, 1.5f);
            var targetE = TargetSelector.GetTarget(targetMode, ERange);
            var targetEX1 = TargetSelector.GetTarget(targetMode, EX1Range);
            var targetEX2 = TargetSelector.GetTarget(targetMode, EX2Range);
            var targetF = TargetSelector.GetTarget(targetMode, FRange);

            //DebugTarget = TargetSelector.GetTarget(TargetingMode.NearMouse);

            var latencyDelay = LucieHero.AbilitySystem.Latency / 2f;
            var finalDelay = LucieMenu.GetBoolean("main.includePing") ? latencyDelay : 0f;

            var isCastingOrChanneling = LucieHero.AbilitySystem.IsCasting || LucieHero.IsChanneling;

            if (isCastingOrChanneling)
            {
                LocalPlayer.EditAimPosition = true;

                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability7:
                        if (targetF != null)
                        {
                            LocalPlayer.Aim(targetF.MapObject.Position); //TODO?: Add prediction
                        }
                        break;

                    case AbilitySlot.Ability5:
                        if (targetE != null)
                        {
                            var pred = LucieHero.GetPrediction(targetE, ESpeed, ERange, ERadius, SkillType.Line, finalDelay, CollisionFlags.InvisWalls);

                            if (pred.HitChancePercent >= 40f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;

                    case AbilitySlot.Ability4:
                        if (targetQ != null)
                        {
                            var pred = LucieHero.GetPrediction(targetQ, QSpeed, QRange, QRadius, SkillType.Circle, finalDelay, 0);

                            if (pred.HitChancePercent >= 30f)
                            {
                                LocalPlayer.Aim(LucieHero.MapObject.Position);
                            }
                        }
                        break;

                    case AbilitySlot.EXAbility2:
                        if (targetEX2 != null)
                        {
                            var pred = LucieHero.GetPrediction(targetEX2, EX2Speed, EX2Range, EX2Radius, SkillType.Line, finalDelay, CollisionFlags.InvisWalls);

                            if (pred.HitChancePercent >= 40f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;

                    case AbilitySlot.EXAbility1:
                        if (targetEX1 != null)
                        {
                            var pred = LucieHero.GetPrediction(targetEX1, EX1Speed, EX1Range, EX1Radius, SkillType.Line, finalDelay, CollisionFlags.InvisWalls);

                            if (pred.HitChancePercent >= 40f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;

                    case AbilitySlot.Ability1:
                        if (targetM1 != null)
                        {
                            var pred = LucieHero.GetPrediction(targetM1, M1Speed, M1Range, M1Radius, SkillType.Line, finalDelay, CollisionFlags.InvisWalls);

                            if (pred.HitChancePercent >= 25f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;
                }
            }
            else
            {
                LastAbilityFired = null;
                LocalPlayer.EditAimPosition = false;
            }

            if (ComboMenu.GetBoolean("combo.useF") && MiscUtils.CanCast(AbilitySlot.Ability7))
            {
                if (LastAbilityFired == null && targetF != null && !targetF.Buffs.Any(x => x.ObjectName == "Panic"))
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability7, true);
                    LastAbilityFired = AbilitySlot.Ability7;
                }
            }

            if (ComboMenu.GetBoolean("combo.useEX2") && MiscUtils.CanCast(AbilitySlot.EXAbility2))
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useEX2.minEnergyBars") * 25;
                if (energyRequired <= LucieHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && targetEX2 != null)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.EXAbility2, true);
                        LastAbilityFired = AbilitySlot.EXAbility2;
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useEX1") && MiscUtils.CanCast(AbilitySlot.EXAbility1))
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useEX1.minEnergyBars") * 25;
                if (energyRequired <= LucieHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && targetEX1 != null)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                        LastAbilityFired = AbilitySlot.EXAbility1;
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useE") && MiscUtils.CanCast(AbilitySlot.Ability5))
            {
                if (LastAbilityFired == null && targetE != null && !targetE.Buffs.Any(x => x.ObjectName == "CripplingGooSlow"))
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability5, true);
                    LastAbilityFired = AbilitySlot.Ability5;
                }
            }

            if (ComboMenu.GetBoolean("combo.useQ") && MiscUtils.CanCast(AbilitySlot.Ability4))
            {
                if (LastAbilityFired == null && targetQ != null)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
                    LastAbilityFired = AbilitySlot.Ability4;
                }
            }

            if (ComboMenu.GetBoolean("combo.useM1") && MiscUtils.CanCast(AbilitySlot.Ability1))
            {
                if (LastAbilityFired == null && targetM1 != null)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                    LastAbilityFired = AbilitySlot.Ability1;
                }
            }
        }

        private static void HealOthers()
        {
            Character bestM2 = null;
            Character bestSpace = null;

            var useM2 = HealMenu.GetBoolean("heal.useM2");
            var useSpace = HealMenu.GetBoolean("heal.useSpace");
            var useQ = HealMenu.GetBoolean("heal.useQ");

            var teammates = EntitiesManager.LocalTeam.Where(x => !x.IsLocalPlayer && !x.Living.IsDead && !x.PhysicsCollision.IsImmaterial);

            var firstPriorityM2 = teammates.Where(x => x.Distance(LucieHero) <= M2Range && x.Living.Health < x.Living.MaxRecoveryHealth && x.EnemiesAround(HealMenu.GetSlider("heal.allySafeRange")) > 0)
                .OrderBy(x => x.Living.Health)
                .FirstOrDefault();

            if (firstPriorityM2 != null)
            {
                bestM2 = firstPriorityM2;
            }
            else
            {
                var secondPriorityM2 = teammates.Where(x => x.Distance(LucieHero) <= M2Range && x.Living.Health < x.Living.MaxRecoveryHealth)
                    .OrderBy(x => x.Living.Health)
                    .FirstOrDefault();

                if (secondPriorityM2 != null)
                {
                    bestM2 = secondPriorityM2;
                }
                else
                {
                    if (HealMenu.GetBoolean("heal.useM2.fullHP"))
                    {
                        var thirdPriorityM2 = teammates.Where(x => x.Distance(LucieHero) <= M2Range && x.EnemiesAround(HealMenu.GetSlider("heal.allySafeRange")) > 0)
                            .OrderBy(x => x.Living.Health)
                            .FirstOrDefault();

                        if (thirdPriorityM2 != null)
                        {
                            bestM2 = thirdPriorityM2;
                        }
                    }
                }
            }

            bestSpace = teammates.Where(x => x.EnemiesAround(HealMenu.GetSlider("heal.allySafeRange")) > 0)
                .OrderByDescending(x => x.EnemiesAround(HealMenu.GetSlider("heal.allySafeRange")))
                .FirstOrDefault();

            var latencyDelay = LucieHero.AbilitySystem.Latency / 2f;
            var finalDelay = LucieMenu.GetBoolean("main.includePing") ? latencyDelay : 0f;

            var isCastingOrChanneling = LucieHero.AbilitySystem.IsCasting || LucieHero.IsChanneling;

            if (isCastingOrChanneling)
            {
                LocalPlayer.EditAimPosition = true;

                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability3:
                        if (bestSpace != null)
                        {
                            LocalPlayer.Aim(bestSpace.MapObject.Position);
                        }
                        break;

                    case AbilitySlot.Ability2:
                        if (bestM2 != null)
                        {
                            var pred = LucieHero.GetPrediction(bestM2, M2Speed, M2Range, M2Radius, SkillType.Circle, finalDelay, 0);

                            if (pred.HitChancePercent >= 30f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;
                }
            }
            else
            {
                LocalPlayer.EditAimPosition = false;
                LastAbilityFired = null;
            }

            if (useSpace && MiscUtils.CanCast(AbilitySlot.Ability3))
            {
                if (LastAbilityFired == null && bestSpace != null)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability3, true);
                    LastAbilityFired = AbilitySlot.Ability3;
                }
            }

            if (useM2 && MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                if (LastAbilityFired == null && bestM2 != null)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                    LastAbilityFired = AbilitySlot.Ability2;
                }
            }
        }

        private static void HealSelf()
        {
            var shouldM2 = HealMenu.GetBoolean("heal.useM2") 
                && (LucieHero.Living.Health < LucieHero.Living.MaxRecoveryHealth 
                || (HealMenu.GetBoolean("heal.useM2.fullHP") && LucieHero.EnemiesAround(HealMenu.GetSlider("heal.allySafeRange")) > 0));

            var shouldSpace = HealMenu.GetBoolean("heal.useSpace") && LucieHero.EnemiesAround(HealMenu.GetSlider("heal.allySafeRange")) > 0;

            var isCastingOrChanneling = LucieHero.AbilitySystem.IsCasting || LucieHero.IsChanneling;

            if (isCastingOrChanneling)
            {
                LocalPlayer.EditAimPosition = true;

                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability3:
                        LocalPlayer.Aim(LucieHero.MapObject.Position);
                        break;

                    case AbilitySlot.Ability2:
                        LocalPlayer.Aim(LucieHero.MapObject.Position);
                        break;
                }
            }
            else
            {
                LocalPlayer.EditAimPosition = false;
                LastAbilityFired = null;
            }

            if (shouldSpace && MiscUtils.CanCast(AbilitySlot.Ability3) && LastAbilityFired == null)
            {
                LocalPlayer.PressAbility(AbilitySlot.Ability3, true);
                LastAbilityFired = AbilitySlot.Ability3;
            }

            if (shouldM2 && MiscUtils.CanCast(AbilitySlot.Ability2) && LastAbilityFired == null)
            {
                LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                LastAbilityFired = AbilitySlot.Ability2;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (LucieHero.CharName != "Lucie")
            {
                return;
            }

            Drawing.DrawString(new Vector2(1920f / 2f, 1080f / 2f - 5f).ScreenToWorld(),
                "Targeting mode: " + (KeysMenu.GetKeybind("keys.changeTargeting") ? "LowestHealth" : "NearMouse"), UnityEngine.Color.yellow);

            if (DrawingsMenu.GetBoolean("draw.healSafeRange"))
            {
                var allyTargets = EntitiesManager.LocalTeam.Where(x => !x.Living.IsDead);

                foreach (var ally in allyTargets)
                {
                    Drawing.DrawCircle(ally.MapObject.Position, HealMenu.GetSlider("heal.allySafeRange"), UnityEngine.Color.green);
                    //Drawing.DrawString(ally.MapObject.Position.WorldToScreen().ScreenToWorld(), "Ally is here", UnityEngine.Color.cyan);
                }

                //Drawing.DrawString(new Vector2(1920f / 2f, 1080 / 2f - 100f).ScreenToWorld(), "MousePosition: " + InputManager.MousePosition.ToString(), UnityEngine.Color.white);
                //Drawing.DrawString(new Vector2(1920f / 2f, 1080 / 2f - 200f).ScreenToWorld(), "MousePosition.WorldToScreen(): " + InputManager.MousePosition.WorldToScreen().ToString(), UnityEngine.Color.white);
                //Drawing.DrawString(new Vector2(1920f / 2f, 1080 / 2f - 300f).ScreenToWorld(), "Input.mousePosition: " + UnityEngine.Input.mousePosition.ToString(), UnityEngine.Color.white);
            }

            //if (DebugTarget != null)
            //{
            //    Drawing.DrawCircle(DebugTarget.MapObject.Position, 1f, UnityEngine.Color.red);
            //}
        }

        public void OnUnload()
        {

        }
    }
}
