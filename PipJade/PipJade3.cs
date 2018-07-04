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

        private static readonly List<Battlerite> Battlerites = new List<Battlerite>(5);

        private const CollisionFlags ColFlags = CollisionFlags.InvisWalls | CollisionFlags.HighBlock | CollisionFlags.LowBlock;

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

        private static bool HasDeadlyFocus;
        private static bool HasExplosiveJump;
        private static bool HasMagicBullet;

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
            ComboMenu.Add(new MenuCheckBox("combo.interrupt", "Interrupt casting when target lost or enters countering", true));
            ComboMenu.Add(new MenuCheckBox("combo.noBulwark", "Don't shoot/Cancel shot if target has Bulwark", true));
            ComboMenu.Add(new MenuCheckBox("combo.useM1", "Use Left Mouse (Revolver Shot)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useM2", "Use Right Mouse (Snipe) when in safe range", true));
            ComboMenu.Add(new MenuSlider("combo.useM2.safeRange", "    ^ Safe range", 7f, M2Range - 1f, 0f));
            ComboMenu.Add(new MenuCheckBox("combo.useSpace", "Use Space (Blast Vault) when enemies are too close", true));
            ComboMenu.Add(new MenuCheckBox("combo.useQ.reset", "Use Q (Stealth) if M2 (Snipe) is on cooldown", false));
            ComboMenu.Add(new MenuCheckBox("combo.useQ.near", "Use Q (Stealth) when enemies are too close", true));
            ComboMenu.Add(new MenuCheckBox("combo.useE", "Use E (Disabling Shot) to interrupt", true));
            ComboMenu.Add(new MenuCheckBox("combo.useR", "Use R (Junk Shot)", true));
            ComboMenu.Add(new MenuIntSlider("combo.useR.minEnergyBars", "    ^ Min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useR.closeRange", "    ^ Only use at close range", true));
            ComboMenu.Add(new MenuCheckBox("combo.useEX1", "Use EX1 (Snap Shot)", false));
            ComboMenu.Add(new MenuIntSlider("combo.useEX1.minEnergyBars", "    ^ Min energy bars", 2, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useF", "Use F (Explosive Shells)", true));
            ComboMenu.Add(new MenuSlider("combo.useF.safeRange", "    ^ Safe range", 3f, M2Range - 1f, 0f));
            JadeMenu.Add(ComboMenu);

            KSMenu = new Menu("ksmenu", "Killsteal", true);
            KSMenu.AddLabel("Combo Key must be held for these to work");
            KSMenu.Add(new MenuCheckBox("ks.useEX1", "Killsteal with EX1", true));
            KSMenu.Add(new MenuCheckBox("ks.useR", "Killsteal with R", true));
            JadeMenu.Add(KSMenu);

            DrawingsMenu = new Menu("drawingsmenu", "Drawings", true);
            DrawingsMenu.Add(new MenuCheckBox("draw.disableAll", "Disable all drawings", false));
            DrawingsMenu.AddSeparator();
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeM1", "Draw Left Mouse Range (Revolver Shot)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeM2", "Draw Right Mouse Range (Snipe)", true));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeM2.safeRange", "Draw Right Mouse Safe-Range (Snipe)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeSpace", "Draw Space Range (Blast Vault)", true));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeE", "Draw E Range (Disabling Shot)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeR", "Draw R Range (Junk Shot)", true));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeF", "Draw F Range (Explosive Shells)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeF.safeRange", "Draw F Safe-Range (Explosive Shells)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.escapeSkillsScreen", "Draw escape skills CDs on screen", true));
            JadeMenu.Add(DrawingsMenu);

            MainMenu.AddMenu(JadeMenu);

            Game.OnUpdate += OnUpdate;
            Game.OnDraw += OnDraw;
            Game.OnMatchStateUpdate += OnMatchStateUpdate;
        }

        private void OnMatchStateUpdate(MatchStateUpdate args)
        {
            if (JadeHero.CharName != "Gunner")
            {
                return;
            }

            if (args.OldMatchState == MatchState.BattleritePicking && args.NewMatchState != MatchState.BattleritePicking)
            {
                GetBattlerites();
            }
        }

        private static void GetBattlerites()
        {
            if (Battlerites.Any())
            {
                Battlerites.Clear();
            }

            for (var i = 0; i < 5; i++)
            {
                var br = JadeHero.BattleriteSystem.GetEquippedBattlerite(i);
                if (br != null)
                {
                    Battlerites.Add(br);
                }
            }
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

            //if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.G))
            //{
            //    GetBattlerites();
            //}

            FinalDelay = JadeMenu.GetBoolean("main.includePing") ? JadeHero.AbilitySystem.Latency / 2000f : 0f;

            HasDeadlyFocus = Battlerites.Any(x => x.Name == "DeadlyFocusUpgrade");
            HasExplosiveJump = Battlerites.Any(x => x.Name == "ExplosiveJumpUpgrade");
            HasMagicBullet = Battlerites.Any(x => x.Name == "MagicBulletUpgrade");

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
            var RTarget = TargetSelector.GetTarget(targetMode, !ComboMenu.GetBoolean("combo.useR.closeRange") ? RRange : RRange / 2f);
            var ETarget = EntitiesManager.EnemyTeam
                .Where(x => x.IsValid && !x.Living.IsDead && (x.AbilitySystem.IsCasting || x.IsChanneling) && !x.IsCountering && x.Distance(JadeHero) < (!HasMagicBullet ? ERange : ERange + (ERange * 10f / 100f)))
                .OrderBy(x => x.Distance(JadeHero))
                .FirstOrDefault();

            var isCastingOrChanneling = JadeHero.AbilitySystem.IsCasting || JadeHero.IsChanneling;

            if (!isCastingOrChanneling && ComboMenu.GetBoolean("combo.useSpace") && MiscUtils.CanCast(AbilitySlot.Ability3) && JadeHero.EnemiesAroundAlive(2.5f) > 0)
            {
                if (!MiscUtils.HasBuff(JadeHero, "Stealth")) //Not stealthed
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability3, true);
                }
            }

            if (!isCastingOrChanneling && ComboMenu.GetBoolean("combo.useQ.near") && MiscUtils.CanCast(AbilitySlot.Ability4) && JadeHero.EnemiesAroundAlive(2f) > 0)
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
                        if (ETarget != null && !ETarget.IsCountering)
                        {
                            if (ComboMenu.GetBoolean("combo.noBulwark") && ETarget.HasBuff("BulwarkBuff"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                                break;
                            }

                            var pred = JadeHero.GetPrediction(ETarget, ESpeed, ERange, ERadius, SkillType.Line, FinalDelay, ColFlags);

                            if (pred.HitChancePercent >= 35f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        else
                        {
                            if (ComboMenu.GetBoolean("combo.interrupt"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        break;

                    case AbilitySlot.Ability7: //F
                        if (M2_FTarget != null && !M2_FTarget.IsCountering)
                        {
                            if (ComboMenu.GetBoolean("combo.noBulwark") && M2_FTarget.HasBuff("BulwarkBuff"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                                break;
                            }

                            var pred = JadeHero.GetPrediction(M2_FTarget, FSpeed, M2Range, FRadius, SkillType.Line, FinalDelay, ColFlags);

                            if (pred.HitChancePercent >= 50f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        else
                        {
                            if (ComboMenu.GetBoolean("combo.interrupt"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        break;

                    case AbilitySlot.Ability6: //R
                        if (RTarget != null && !RTarget.IsCountering)
                        {
                            if (ComboMenu.GetBoolean("combo.noBulwark") && RTarget.HasBuff("BulwarkBuff"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                                break;
                            }

                            var pred = JadeHero.GetPrediction(RTarget, RSpeed, RRange, RRadius, SkillType.Line, FinalDelay, ColFlags);

                            if (pred.HitChancePercent >= 40f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        else
                        {
                            if (ComboMenu.GetBoolean("combo.interrupt"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        break;

                    case AbilitySlot.Ability2: //M2
                    case AbilitySlot.EXAbility1:
                        if (M2_FTarget != null && !M2_FTarget.IsCountering)
                        {
                            if (ComboMenu.GetBoolean("combo.noBulwark") && M2_FTarget.HasBuff("BulwarkBuff"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                                break;
                            }

                            var pred = JadeHero.GetPrediction(M2_FTarget, M2Speed, M2Range, M2Radius, SkillType.Line, FinalDelay, ColFlags);

                            if (pred.HitChancePercent >= 50f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        else
                        {
                            if (ComboMenu.GetBoolean("combo.interrupt"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        break;

                    case AbilitySlot.Ability1: //M1
                        if (M1Target != null && !M1Target.IsCountering)
                        {
                            if (ComboMenu.GetBoolean("combo.noBulwark") && M1Target.HasBuff("BulwarkBuff"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                                break;
                            }

                            var pred = JadeHero.GetPrediction(M1Target, M1Speed, M1Range, M1Radius, SkillType.Line, FinalDelay, ColFlags);

                            if (pred.HitChancePercent >= 25f)
                            {
                                LocalPlayer.Aim(pred.PredictedPosition);
                            }
                        }
                        else
                        {
                            if (ComboMenu.GetBoolean("combo.interrupt"))
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
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
                if (LastAbilityFired == null && ETarget != null && !ETarget.IsCountering && ((!ComboMenu.GetBoolean("combo.noBulwark")) || (ComboMenu.GetBoolean("combo.noBulwark") && !ETarget.HasBuff("BulwarkBuff"))))
                {
                    var pred = JadeHero.GetPrediction(ETarget, ESpeed, ERange, ERadius, SkillType.Line, FinalDelay, ColFlags);

                    if (pred.HitChancePercent >= 20f)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability5, true);
                        LastAbilityFired = AbilitySlot.Ability5;
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useF") && MiscUtils.CanCast(AbilitySlot.Ability7))
            {
                if (JadeHero.EnemiesAroundAlive(ComboMenu.GetSlider("combo.useF.safeRange")) == 0)
                {
                    if (LastAbilityFired == null && M2_FTarget != null && !M2_FTarget.IsCountering && ((!ComboMenu.GetBoolean("combo.noBulwark")) || (ComboMenu.GetBoolean("combo.noBulwark") && !M2_FTarget.HasBuff("BulwarkBuff"))))
                    {
                        var pred = JadeHero.GetPrediction(M2_FTarget, FSpeed, M2Range, FRadius, SkillType.Line, FinalDelay, ColFlags);

                        if (pred.HitChancePercent >= 20f)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability7, true);
                            LastAbilityFired = AbilitySlot.Ability7;
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useR") && MiscUtils.CanCast(AbilitySlot.Ability6))
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useR.minEnergyBars") * 25;
                if (energyRequired <= JadeHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && RTarget != null && !RTarget.IsCountering && ((!ComboMenu.GetBoolean("combo.noBulwark")) || (ComboMenu.GetBoolean("combo.noBulwark") && !RTarget.HasBuff("BulwarkBuff"))))
                    {
                        var pred = JadeHero.GetPrediction(RTarget, RSpeed, RRange, RRadius, SkillType.Line, FinalDelay, ColFlags);

                        if (pred.HitChancePercent >= 20f)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                            LastAbilityFired = AbilitySlot.Ability6;
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useEX1") && MiscUtils.CanCast(AbilitySlot.EXAbility1))
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useEX1.minEnergyBars") * 25;
                if (energyRequired <= JadeHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && M2_FTarget != null && !M2_FTarget.IsCountering && ((!ComboMenu.GetBoolean("combo.noBulwark")) || (ComboMenu.GetBoolean("combo.noBulwark") && !M2_FTarget.HasBuff("BulwarkBuff"))))
                    {
                        var pred = JadeHero.GetPrediction(M2_FTarget, M2Speed, M2Range, M2Radius, SkillType.Line, FinalDelay, ColFlags);

                        if (pred.HitChancePercent >= 20f)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                            LastAbilityFired = AbilitySlot.EXAbility1;
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useM2") && MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                if (JadeHero.EnemiesAroundAlive(ComboMenu.GetSlider("combo.useM2.safeRange")) == 0)
                {
                    if (LastAbilityFired == null && M2_FTarget != null && !M2_FTarget.IsCountering && ((!ComboMenu.GetBoolean("combo.noBulwark")) || (ComboMenu.GetBoolean("combo.noBulwark") && !M2_FTarget.HasBuff("BulwarkBuff"))))
                    {
                        var pred = JadeHero.GetPrediction(M2_FTarget, M2Speed, M2Range, M2Radius, SkillType.Line, FinalDelay, ColFlags);

                        if (pred.HitChancePercent >= 20f)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                            LastAbilityFired = AbilitySlot.Ability2;
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useM1") && JadeHero.Blessings.Blessings > 0)
            {
                if (LastAbilityFired == null && M1Target != null && !M1Target.IsCountering && ((!ComboMenu.GetBoolean("combo.noBulwark")) || (ComboMenu.GetBoolean("combo.noBulwark") && !M1Target.HasBuff("BulwarkBuff"))))
                {
                    var pred = JadeHero.GetPrediction(M1Target, M1Speed, M1Range, M1Radius, SkillType.Line, FinalDelay, ColFlags);

                    if (pred.HitChancePercent >= 10f)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        LastAbilityFired = AbilitySlot.Ability1;
                    }
                }
            }
        }

        private static void KillstealMode()
        {
            var possibleEnemies = EntitiesManager.EnemyTeam.Where(x => x.IsValid && !x.Living.IsDead && !x.IsCountering && !x.PhysicsCollision.IsImmaterial);

            foreach (var enemy in possibleEnemies)
            {
                if (KSMenu.GetBoolean("ks.useEX1") && LastAbilityFired == null && enemy.Living.Health <= (!HasDeadlyFocus ? 12f : 12f + 5f) && enemy.Distance(JadeHero) < M2Range && MiscUtils.CanCast(AbilitySlot.EXAbility1)) //EX1
                {
                    var pred = JadeHero.GetPrediction(enemy, M2Speed, M2Range, M2Radius, SkillType.Line, FinalDelay, ColFlags);

                    if (pred.HitChancePercent >= 20f)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                        LastAbilityFired = AbilitySlot.EXAbility1;
                    }
                }

                if (KSMenu.GetBoolean("ks.useR") && LastAbilityFired == null && enemy.Living.Health <= 6f && enemy.Distance(JadeHero) < RRange && MiscUtils.CanCast(AbilitySlot.Ability6)) //R
                {
                    var pred = JadeHero.GetPrediction(enemy, RSpeed, RRange, RRadius, SkillType.Line, FinalDelay, ColFlags);

                    if (pred.HitChancePercent >= 20f)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                        LastAbilityFired = AbilitySlot.Ability6;
                    }
                }

                if (KSMenu.GetBoolean("ks.useR") && LastAbilityFired == null && enemy.Living.Health <= 6f * 3f && enemy.Distance(JadeHero) < 1.25f && MiscUtils.CanCast(AbilitySlot.Ability6)) //R
                {
                    var pred = JadeHero.GetPrediction(enemy, RSpeed, RRange, RRadius, SkillType.Line, FinalDelay, ColFlags);

                    if (pred.HitChancePercent >= 20f)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                        LastAbilityFired = AbilitySlot.Ability6;
                    }
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
                if (MiscUtils.CanCast(AbilitySlot.EXAbility1) && orbHealth <= (!HasDeadlyFocus ? 12f : 12f + 5f))
                {
                    LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                }
                else if (MiscUtils.CanCast(AbilitySlot.Ability2) && orbHealth > 6f * 4f && orbHealth <= (!HasDeadlyFocus ? 38f : 38f + 5f))
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                }
            }

            if (JadeHero.Distance(orbPos) <= M1Range)
            {
                if (JadeHero.Blessings.Blessings > 0)
                {
                    if (orb.EnemiesAroundAlive(6f) == 0)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                    }
                    else
                    {
                        if (orbHealth <= 6f * 4f || orbHealth >= 6f * 4f + (6f * 4f / 2f))
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                }
            }
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

            if (DrawingsMenu.GetBoolean("draw.disableAll"))
            {
                return;
            }

            Drawing.DrawString(new Vector2(1920f / 2f, 1080f / 2f - 5f).ScreenToWorld(),
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
                Drawing.DrawCircle(JadeHero.MapObject.Position, (!HasExplosiveJump ? SpaceRange : SpaceRange + (SpaceRange * 20f / 100f)), UnityEngine.Color.green);
            }

            if (DrawingsMenu.GetBoolean("draw.rangeE"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, (!HasMagicBullet ? ERange : ERange + (ERange * 10f / 100f)), UnityEngine.Color.red);
            }

            if (DrawingsMenu.GetBoolean("draw.rangeR"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, RRange, UnityEngine.Color.red);
            }

            if (DrawingsMenu.GetBoolean("draw.rangeF"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, FRange, UnityEngine.Color.magenta);
            }

            if (DrawingsMenu.GetBoolean("draw.rangeF.safeRange"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, ComboMenu.GetSlider("combo.useF.safeRange"), UnityEngine.Color.blue);
            }

            if (DrawingsMenu.GetBoolean("draw.escapeSkillsScreen"))
            {
                var drawSpacePos = new Vector2(760f, 350f);
                var abilitySpace = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability3);
                var abilitySpaceReady = MiscUtils.CanCast(AbilitySlot.Ability3);
                var textToDrawSpace = "Space state: " + (abilitySpaceReady ? "Ready" : Math.Round(abilitySpace.CooldownLeft, 2).ToString());
                Drawing.DrawString(drawSpacePos.ScreenToWorld(), textToDrawSpace, abilitySpaceReady ? UnityEngine.Color.cyan : UnityEngine.Color.gray);

                var drawQPos = new Vector2(1920f - 760f, 350f);
                var abilityQ = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability4);
                var abilityQReady = MiscUtils.CanCast(AbilitySlot.Ability4);
                var textToDrawQ = "Q state: " + (abilityQReady ? "Ready" : Math.Round(abilityQ.CooldownLeft, 2).ToString());
                Drawing.DrawString(drawQPos.ScreenToWorld(), textToDrawQ, abilityQReady ? UnityEngine.Color.cyan : UnityEngine.Color.gray);
            }
        }
    }
}
