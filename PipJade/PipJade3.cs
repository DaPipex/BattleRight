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

using PipLibrary.Extensions;
using PipLibrary.Utils;

namespace PipJade
{
    public class PipJade3 : IAddon
    {
        private static Menu JadeMenu;
        private static Menu KeysMenu, ComboMenu, DrawingsMenu, KSMenu;

        private static Character JadeHero;

        private static AbilitySlot? LastAbilityFired = null;

        private const float M1Speed = 17f;
        private const float M2Speed = 28.5f;
        private const float ESpeed = 28f;
        private const float RSpeed = 24f;
        private const float FSpeed = 23f;

        private const float M1Range = 6.8f;
        private const float M2Range = 11.5f;
        private const float SpaceRange = 7f;
        private const float ERange = 9.5f;
        private const float RRange = 5f;
        private const float FRange = 11.6f; //Not really used in this script but kept here just in case

        private const float M1Radius = 0.25f;
        private const float M2Radius = 0.4f;
        private const float ERadius = 0.35f;
        private const float RRadius = 0.3f; //Not precise, need to take cone's shape into consideration
        private const float FRadius = 0.4f;

        private static float FinalDelay;

        public void OnInit()
        {
            JadeMenu = new Menu("pipjademenu", "DaPip's Jade");
            JadeMenu.Add(new MenuCheckBox("main.includePing", "Include ping in prediction?", false));

            KeysMenu = new Menu("keysmenu", "Keys", true);
            KeysMenu.Add(new MenuKeybind("keys.combo", "Combo Key", UnityEngine.KeyCode.LeftControl));
            KeysMenu.Add(new MenuKeybind("keys.orb", "Orb Key", UnityEngine.KeyCode.Mouse3));
            KeysMenu.Add(new MenuKeybind("keys.changeTargeting", "Change targeting mode", UnityEngine.KeyCode.T, false, true));
            JadeMenu.Add(KeysMenu);

            ComboMenu = new Menu("combomenu", "Combo", true);
            ComboMenu.Add(new MenuCheckBox("combo.useM1", "Use Left Mouse (Revolver Shot)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useM2", "Use Right Mouse (Snipe) when in safe range", true));
            ComboMenu.Add(new MenuSlider("combo.useM2.safeRange", "    ^ Safe range", 7f, M2Range - 1f, 0f));
            ComboMenu.Add(new MenuCheckBox("combo.useSpace", "Use Space (Blast Vault) when enemies are too close", true));
            ComboMenu.Add(new MenuCheckBox("combo.useQ.reset", "Use Q (Stealth) if M2 (Snipe) is on cooldown", false));
            ComboMenu.Add(new MenuCheckBox("combo.useQ.near", "Use Q (Stealth) when enemies are too close", false));
            ComboMenu.Add(new MenuCheckBox("combo.useE", "Use E (Disabling Shot) to interrupt", true));
            ComboMenu.Add(new MenuCheckBox("combo.useR", "Use R (Junk Shot)", true));
            ComboMenu.Add(new MenuIntSlider("combo.useR.minEnergyBars", "    ^ Min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useR.closeRange", "    ^ Only use at close range", true));
            ComboMenu.Add(new MenuCheckBox("combo.useEX1", "Use EX1 (Snap Shot)", false));
            ComboMenu.Add(new MenuIntSlider("combo.useEX1.minEnergyBars", "    ^ Min energy bars", 2, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useF", "Use F (Explosive Shells)", true));
            JadeMenu.Add(ComboMenu);

            KSMenu = new Menu("ksmenu", "Killsteal", true);
            KSMenu.AddLabel("Combo Key must be held for these to work");
            KSMenu.Add(new MenuCheckBox("ks.useEX1", "Killsteal with EX1", true));
            KSMenu.Add(new MenuCheckBox("ks.useR", "Killsteal with R", true));
            JadeMenu.Add(KSMenu);

            DrawingsMenu = new Menu("drawingsmenu", "Drawings", true);
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeM1", "Draw Left Mouse Range (Revolver Shot)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeM2", "Draw Right Mouse Range (Snipe)", true));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeM2.safeRange", "Draw Right Mouse Safe-Range (Snipe)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeSpace", "Draw Space Range (Blast Vault)", true));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeE", "Draw E Range (Disabling Shot)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeR", "Draw R Range (Junk Shot)", true));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeF", "Draw F Range (Explosive Shells)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.escapeSkillsScreen", "Draw escape skills CDs on screen", true));
            JadeMenu.Add(DrawingsMenu);

            MainMenu.AddMenu(JadeMenu);

            Game.OnUpdate += OnUpdate;
            Game.OnDraw += OnDraw;
        }

        public void OnUnload()
        {

        }

        private void OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            JadeHero = EntitiesManager.LocalPlayer;

            if (JadeHero.CharName != "Gunner")
            {
                return;
            }

            LocalPlayer.EditAimPosition = false;

            FinalDelay = JadeMenu.GetBoolean("main.includePing") ? JadeHero.AbilitySystem.Latency / 2f : 0f;


            if (KeysMenu.GetKeybind("keys.combo"))
            {
                KillstealMode();
                ComboMode();
            }

            if (KeysMenu.GetKeybind("keys.orb"))
            {
                OrbMode();
            }
        }

        private static void ComboMode()
        {
            var targetModeKey = KeysMenu.GetKeybind("keys.changeTargeting");
            var targetMode = targetModeKey ? TargetingMode.LowestHealth : TargetingMode.NearMouse;

            var M1Target = TargetSelector.GetTarget(targetMode, M1Range);
            var M2_FTarget = TargetSelector.GetTarget(targetMode, M2Range);
            var RTarget = TargetSelector.GetTarget(targetMode, ComboMenu.GetBoolean("combo.useR.closeRange") ? RRange / 2f : RRange);
            var ETarget = EntitiesManager.EnemyTeam
                .Where(x => x.IsValid && !x.Living.IsDead && (x.AbilitySystem.IsCasting || x.IsChanneling) && !x.IsCountering && x.Distance(JadeHero) < ERange)
                .OrderBy(x => x.Distance(JadeHero))
                .FirstOrDefault();

            var isCastingOrChanneling = JadeHero.AbilitySystem.IsCasting || JadeHero.IsChanneling;

            if (!isCastingOrChanneling && ComboMenu.GetBoolean("combo.useSpace") && MiscUtils.CanCast(AbilitySlot.Ability3) && JadeHero.EnemiesAround(2.5f) > 0)
            {
                if (!MiscUtils.HasBuff(JadeHero, "Stealth")) //Not stealthed
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability3, true);
                }
            }

            if (!isCastingOrChanneling && ComboMenu.GetBoolean("combo.useQ.near") && MiscUtils.CanCast(AbilitySlot.Ability4) && JadeHero.EnemiesAround(2f) > 0)
            {
                LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
            }

            if (!isCastingOrChanneling && ComboMenu.GetBoolean("combo.useQ.reset") && MiscUtils.CanCast(AbilitySlot.Ability4) 
                && LocalPlayer.GetAbilityHudData(AbilitySlot.Ability2).CooldownLeft > 0f)
            {
                LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
            }

            if (isCastingOrChanneling)
            {
                LocalPlayer.EditAimPosition = true;

                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability5: //E
                        if (ETarget != null)
                        {
                            var pred = JadeHero.GetPrediction(ETarget, ESpeed, ERange, ERadius, SkillType.Line, FinalDelay, CollisionFlags.InvisWalls);

                            if (pred.HitChancePercent >= 35f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;

                    case AbilitySlot.Ability7: //F
                        if (M2_FTarget != null)
                        {
                            var pred = JadeHero.GetPrediction(M2_FTarget, FSpeed, M2Range, FRadius, SkillType.Line, FinalDelay, CollisionFlags.InvisWalls);

                            if (pred.HitChancePercent >= 50f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;

                    case AbilitySlot.Ability6: //R
                        if (RTarget != null)
                        {
                            var pred = JadeHero.GetPrediction(RTarget, RSpeed, RRange, RRadius, SkillType.Line, FinalDelay, CollisionFlags.InvisWalls);

                            if (pred.HitChancePercent >= 40f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;

                    case AbilitySlot.Ability2: //M2
                    case AbilitySlot.EXAbility1:
                        if (M2_FTarget != null)
                        {
                            var pred = JadeHero.GetPrediction(M2_FTarget, M2Speed, M2Range, M2Radius, SkillType.Line, FinalDelay, CollisionFlags.InvisWalls);

                            if (pred.HitChancePercent >= 50f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        break;

                    case AbilitySlot.Ability1: //M1
                        if (M1Target != null)
                        {
                            var pred = JadeHero.GetPrediction(M1Target, M1Speed, M1Range, M1Radius, SkillType.Line, FinalDelay, CollisionFlags.InvisWalls /*| CollisionFlags.NPCBlocker*/);

                            if (pred.HitChancePercent >= 50f)
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

            if (ComboMenu.GetBoolean("combo.useE") && MiscUtils.CanCast(AbilitySlot.Ability5))
            {
                if (LastAbilityFired == null && ETarget != null)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability5, true);
                    LastAbilityFired = AbilitySlot.Ability5;
                }
            }

            if (ComboMenu.GetBoolean("combo.useF") && MiscUtils.CanCast(AbilitySlot.Ability7))
            {
                if (LastAbilityFired == null && M2_FTarget != null)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability7, true);
                    LastAbilityFired = AbilitySlot.Ability7;
                }
            }

            if (ComboMenu.GetBoolean("combo.useR") && MiscUtils.CanCast(AbilitySlot.Ability6))
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useR.minEnergyBars") * 25;
                if (energyRequired <= JadeHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && RTarget != null)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                        LastAbilityFired = AbilitySlot.Ability6;
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useM2") && MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                if (JadeHero.EnemiesAround(ComboMenu.GetSlider("combo.useM2.safeRange")) == 0)
                {
                    if (LastAbilityFired == null && M2_FTarget != null)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                        LastAbilityFired = AbilitySlot.Ability2;
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useEX1") && MiscUtils.CanCast(AbilitySlot.EXAbility1))
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useEX1.minEnergyBars") * 25;
                if (energyRequired <= JadeHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && M2_FTarget != null)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                        LastAbilityFired = AbilitySlot.EXAbility1;
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useM1") && JadeHero.Blessings.Blessings > 0)
            {
                if (LastAbilityFired == null && M1Target != null)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                    LastAbilityFired = AbilitySlot.Ability1;
                }
            }
        }

        private static void KillstealMode()
        {
            var possibleEnemies = EntitiesManager.EnemyTeam.Where(x => x.IsValid && !x.Living.IsDead && !x.IsCountering && !x.PhysicsCollision.IsImmaterial);

            foreach (var enemy in possibleEnemies)
            {
                if (KSMenu.GetBoolean("ks.useEX1") && LastAbilityFired == null && enemy.Living.Health <= 12f && enemy.Distance(JadeHero) < M2Range && MiscUtils.CanCast(AbilitySlot.EXAbility1)) //EX1
                {
                    LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                    LastAbilityFired = AbilitySlot.EXAbility1;
                }

                if (KSMenu.GetBoolean("ks.useR") && LastAbilityFired == null && enemy.Living.Health <= 6f && enemy.Distance(JadeHero) < RRange && MiscUtils.CanCast(AbilitySlot.Ability6)) //R
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                    LastAbilityFired = AbilitySlot.Ability6;
                }

                if (KSMenu.GetBoolean("ks.useR") && LastAbilityFired == null && enemy.Living.Health <= 6f * 3f && enemy.Distance(JadeHero) < 1.75f && MiscUtils.CanCast(AbilitySlot.Ability6)) //R
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                    LastAbilityFired = AbilitySlot.Ability6;
                }
            }
        }

        private static void OrbMode()
        {
            var orb = EntitiesManager.CenterOrb;
            var orbHealth = orb.Get<LivingObject>().Health;
            var orbPos = orb.Get<MapGameObject>().Position;

            if (orbHealth <= 0)
            {
                return;
            }

            LocalPlayer.EditAimPosition = true;
            LocalPlayer.Aim(orbPos);

            if (JadeHero.Distance(orbPos) <= M2Range)
            {
                if (MiscUtils.CanCast(AbilitySlot.EXAbility1) && orbHealth <= 12f)
                {
                    LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                }
                else if (MiscUtils.CanCast(AbilitySlot.Ability2) && orbHealth <= 38f)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                }
            }

            if (JadeHero.Distance(orbPos) <= M1Range)
            {
                if (JadeHero.Blessings.Blessings > 0)
                {
                    if (orb.EnemiesAround(6f) == 0)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                    }
                    else
                    {
                        if (orbHealth <= 6 * 4 || orbHealth >= 6 * 4 + (6 * 4 / 2))
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                }
            }

            //LocalPlayer.EditAimPosition = false;
        }

        private void OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (JadeHero.CharName != "Gunner")
            {
                return;
            }

            Drawing.DrawString(new Vector2(1920f / 2f, 1080f / 2f - 5f).TempScreenToWorld(),
                "Targeting mode: " + (KeysMenu.GetKeybind("keys.changeTargeting") ? "LowestHealth" : "NearMouse"), UnityEngine.Color.yellow);

            if (DrawingsMenu.GetBoolean("draw.rangeM1"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, M1Range, UnityEngine.Color.red);
            }

            if (DrawingsMenu.GetBoolean("draw.rangeM2"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, M2Range, UnityEngine.Color.red);
            }

            if (DrawingsMenu.GetBoolean("draw.rangeM2.safeRange"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, ComboMenu.GetSlider("combo.useM2.safeRange"), UnityEngine.Color.blue);
            }

            if (DrawingsMenu.GetBoolean("draw.rangeSpace"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, SpaceRange, UnityEngine.Color.green);
            }

            if (DrawingsMenu.GetBoolean("draw.rangeR"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, RRange, UnityEngine.Color.red);
            }

            if (DrawingsMenu.GetBoolean("draw.rangeF"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, FRange, UnityEngine.Color.magenta);
            }

            if (DrawingsMenu.GetBoolean("draw.escapeSkillsScreen"))
            {
                var drawSpacePos = new Vector2(760f, 1080f - 350f);
                var abilitySpace = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability3);
                var abilitySpaceReady = MiscUtils.CanCast(AbilitySlot.Ability3);
                var textToDrawSpace = "Space state: " + (abilitySpaceReady ? "Ready" : Math.Round(abilitySpace.CooldownLeft, 2).ToString());
                Drawing.DrawString(drawSpacePos.TempScreenToWorld(), textToDrawSpace, abilitySpaceReady ? UnityEngine.Color.cyan : UnityEngine.Color.gray);

                var drawQPos = new Vector2(1920f - 760f, 1080f - 350f);
                var abilityQ = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability4);
                var abilityQReady = MiscUtils.CanCast(AbilitySlot.Ability4);
                var textToDrawQ = "Q state: " + (abilityQReady ? "Ready" : Math.Round(abilityQ.CooldownLeft, 2).ToString());
                Drawing.DrawString(drawQPos.TempScreenToWorld(), textToDrawQ, abilityQReady ? UnityEngine.Color.cyan : UnityEngine.Color.gray);
            }
        }
    }
}
