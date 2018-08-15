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

using TestPrediction2NS;

namespace PipJade
{
    public class PipJade3 : IAddon
    {
        private static Menu JadeMenu;
        private static Menu KeysMenu, ComboMenu, DrawingsMenu, KSMenu;

        private static Character JadeHero;

        private static AbilitySlot? LastAbilityFired = null;

        private static readonly List<Battlerite> Battlerites = new List<Battlerite>(5);

        private static readonly List<SpecialEnemyCircleObject> SpecialCircleObjects = new List<SpecialEnemyCircleObject>()
        {
            new SpecialEnemyCircleObject("BubbleBarrierArea", 2f, false), //Big bubble - Pearl
            new SpecialEnemyCircleObject("OceanSageWaterBarrierArea", 1.5f, false), //Small bubble - Pearl
            new SpecialEnemyCircleObject("UnstableBubbleArea", 2f, false), //EX2 Bubble - Pearl
            new SpecialEnemyCircleObject("ChronofluxArea", 2f, false), //Chronoflux - Oldur
            new SpecialEnemyCircleObject("ChronofluxAreaLesser", 1.1f, false), //Chronoflux Lesser - Oldur
        };

        private const CollisionFlags ColFlags = CollisionFlags.Bush | CollisionFlags.NPCBlocker;

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

        private static bool HasDeadlyFocus;
        private static bool HasExplosiveJump;
        private static bool HasMagicBullet;

        public void OnInit()
        {
            JadeMenu = new Menu("pipjademenu", "DaPip's Jade");

            KeysMenu = new Menu("keysmenu", "Keys", true);
            KeysMenu.Add(new MenuKeybind("keys.combo", "Combo Key", UnityEngine.KeyCode.LeftControl));
            KeysMenu.Add(new MenuKeybind("keys.orb", "Orb Key", UnityEngine.KeyCode.Mouse3));
            KeysMenu.Add(new MenuKeybind("keys.changeTargeting", "Change targeting mode", UnityEngine.KeyCode.T, false, true));
            JadeMenu.Add(KeysMenu);

            ComboMenu = new Menu("combomenu", "Combo", true);
            ComboMenu.Add(new MenuCheckBox("combo.invisibleTargets", "Aim at invisible targets", true));
            ComboMenu.Add(new MenuCheckBox("combo.noCounter", "Don't shoot if target is in countering/parries/consume", true));
            ComboMenu.Add(new MenuCheckBox("combo.noShield", "Don't shoot if target has Bakko/Ulric shield", true));
            ComboMenu.Add(new MenuCheckBox("combo.noBubble", "Don't shoot if will go through a bubble (Pearl/Oldur)", true));
            ComboMenu.Add(new MenuCheckBox("combo.interrupt", "Interrupt casting when no good target is available", true));
            ComboMenu.Add(new MenuCheckBox("combo.useM1", "Use Left Mouse (Revolver Shot)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useM2", "Use Right Mouse (Snipe) when in safe range", true));
            ComboMenu.Add(new MenuSlider("combo.useM2.safeRange", "    ^ Safe range", 7f, M2Range - 1f, 0f));
            ComboMenu.Add(new MenuCheckBox("combo.useSpace", "Use Space (Blast Vault) when enemies are too close", true));
            ComboMenu.Add(new MenuSlider("combo.useSpace.maxRange", "    ^ Safe range", 2.5f, 3f, 0.1f));
            ComboMenu.Add(new MenuComboBox("combo.useSpace.direction", "    ^ Direction", 0, new string[] { "Safe teammate closest to edge", "Mouse Position" }));
            ComboMenu.Add(new MenuIntSlider("combo.useSpace.accuracy", "    ^ Accuracy (Higher number = Slower)", 32, 64, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useQ.reset", "Use Q (Stealth) if M2 (Snipe) is on cooldown", false));
            ComboMenu.Add(new MenuCheckBox("combo.useQ.near", "Use Q (Stealth) when enemies are too close", true));
            ComboMenu.Add(new MenuCheckBox("combo.useE", "Use E (Disabling Shot) to interrupt", true));
            ComboMenu.Add(new MenuCheckBox("combo.useR", "Use R (Junk Shot)", true));
            ComboMenu.Add(new MenuIntSlider("combo.useR.minEnergyBars", "    ^ Min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useR.closeRange", "    ^ Only use at close range", true));
            ComboMenu.Add(new MenuCheckBox("combo.useEX1", "Use EX1 (Snap Shot)", false));
            ComboMenu.Add(new MenuIntSlider("combo.useEX1.minEnergyBars", "    ^ Min energy bars", 2, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useEX2", "Use EX2 (Smoke Veil) instead of normal Q (Stealth)", false));
            ComboMenu.Add(new MenuIntSlider("combo.useEX2.minEnergyBars", "    ^ When min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useF", "Use F (Explosive Shells)", true));
            ComboMenu.Add(new MenuSlider("combo.useF.safeRange", "    ^ Safe range", 3f, M2Range - 1f, 0f));
            JadeMenu.Add(ComboMenu);

            KSMenu = new Menu("ksmenu", "Killsteal", true);
            KSMenu.AddLabel("Combo Key must be held for these to work");
            KSMenu.Add(new MenuCheckBox("ks.invisibleTargets", "Killsteal invisible targets", true));
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
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeSpace.safeRange", "Draw Space Safe-Range (Blast Vault)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeE", "Draw E Range (Disabling Shot)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeR", "Draw R Range (Junk Shot)", true));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeF", "Draw F Range (Explosive Shells)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.rangeF.safeRange", "Draw F Safe-Range (Explosive Shells)", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.escapeSkillsScreen", "Draw escape skills CDs on screen", true));
            DrawingsMenu.Add(new MenuCheckBox("draw.debugTestPred", "Debug Test Prediction", false));
            DrawingsMenu.Add(new MenuCheckBox("draw.debugJumpToSafety", "Debug Jump to safety", false));
            JadeMenu.Add(DrawingsMenu);

            MainMenu.AddMenu(JadeMenu);

            InGameObject.OnCreate += OnCreate;
            InGameObject.OnDestroy += OnDestroy;
            Game.OnMatchStart += args =>
            {
                foreach (var obj in SpecialCircleObjects)
                {
                    obj.Active = false;
                }
            };
            Game.OnMatchEnd += args =>
            {
                foreach (var obj in SpecialCircleObjects)
                {
                    obj.Active = false;
                }
            };
            Game.OnUpdate += OnUpdate;
            Game.OnDraw += OnDraw;
            Game.OnMatchStateUpdate += OnMatchStateUpdate;
        }

        private void OnCreate(InGameObject gameObject)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (JadeHero.CharName != "Gunner")
            {
                return;
            }

            var baseObject = gameObject.Get<BaseGameObject>();
            if (baseObject != null && baseObject.TeamId != JadeHero.BaseObject.TeamId)
            {
                var matchedSpecial = SpecialCircleObjects.FirstOrDefault(x => x.Name.Equals(gameObject.ObjectName));
                if (matchedSpecial != null)
                {
                    matchedSpecial.Active = true;
                    matchedSpecial.Position = gameObject.Get<MapGameObject>().Position;
                }
            }
        }

        private void OnDestroy(InGameObject gameObject)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (JadeHero.CharName != "Gunner")
            {
                return;
            }

            var baseObject = gameObject.Get<BaseGameObject>();
            if (baseObject != null && baseObject.TeamId != JadeHero.BaseObject.TeamId)
            {
                var matchedSpecial = SpecialCircleObjects.FirstOrDefault(x => x.Name.Equals(gameObject.ObjectName));
                if (matchedSpecial != null)
                {
                    matchedSpecial.Active = false;
                    matchedSpecial.Position = Vector2.Zero;
                }
            }
        }

        private void OnMatchStateUpdate(MatchStateUpdate args)
        {
            if (EntitiesManager.LocalPlayer.CharName != "Gunner")
            {
                return;
            }

            if (args.OldMatchState == MatchState.BattleritePicking && args.NewMatchState != MatchState.BattleritePicking)
            {
                GetBattlerites();
            }

            if (args.NewMatchState == MatchState.PreRound)
            {
                foreach (var obj in SpecialCircleObjects)
                {
                    obj.Active = false;
                }
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
            JadeHero = EntitiesManager.LocalPlayer;

            if (JadeHero.CharName != "Gunner")
            {
                return;
            }

            HasDeadlyFocus = Battlerites.Any(x => x.Name == "DeadlyFocusUpgrade");
            HasExplosiveJump = Battlerites.Any(x => x.Name == "ExplosiveJumpUpgrade");
            HasMagicBullet = Battlerites.Any(x => x.Name == "MagicBulletUpgrade");

            if (KeysMenu.GetKeybind("keys.combo"))
            {
                KillstealMode();
                ComboMode();
            }
            else if (KeysMenu.GetKeybind("keys.orb"))
            {
                OrbMode();
            }
            else
            {
                LocalPlayer.EditAimPosition = false;
                LastAbilityFired = null;
            }
        }

        private static void ComboMode()
        {
            var targetModeKey = KeysMenu.GetKeybind("keys.changeTargeting");
            var targetMode = targetModeKey ? TargetingMode.LowestHealth : TargetingMode.NearMouse;

            var invisibleTargets = ComboMenu.GetBoolean("combo.invisibleTargets");
            var noCounter = ComboMenu.GetBoolean("combo.noCounter");
            var noShield = ComboMenu.GetBoolean("combo.noShield");
            var noBubble = ComboMenu.GetBoolean("combo.noBubble");

            var enemiesToTarget = EntitiesManager.EnemyTeam.Where(x => !x.PhysicsCollision.IsImmaterial);

            if (!invisibleTargets)
            {
                enemiesToTarget = enemiesToTarget.Where(x => !x.CharacterModel.IsModelInvisible);
            }

            if (noCounter)
            {
                enemiesToTarget = enemiesToTarget.Where(x => !x.IsCountering && !x.HasParry() && !x.HasConsumeBuff);
            }

            if (noShield)
            {
                enemiesToTarget = enemiesToTarget.Where(x => !x.HasShield()).ToList();
            }

            var M1Target = TargetSelector.GetTarget(enemiesToTarget, targetMode, M1Range);
            var M2_FTarget = TargetSelector.GetTarget(enemiesToTarget, targetMode, M2Range);
            var RTarget = TargetSelector.GetTarget(enemiesToTarget, targetMode, !ComboMenu.GetBoolean("combo.useR.closeRange") ? RRange : RRange / 2f);
            var ETarget = enemiesToTarget
                .Where(x => x.IsValid && !x.Living.IsDead && (x.AbilitySystem.IsCasting || x.IsChanneling) && !x.IsCountering && x.Distance(JadeHero) < (!HasMagicBullet ? ERange : ERange + (ERange * 10f / 100f)))
                .OrderBy(x => x.Distance(JadeHero))
                .FirstOrDefault();

            var isCastingOrChanneling = JadeHero.AbilitySystem.IsCasting || JadeHero.IsChanneling;

            if (isCastingOrChanneling && LastAbilityFired == null)
            {
                LastAbilityFired = CastingIndexToSlot(JadeHero.AbilitySystem.CastingAbilityIndex);
            }

            var useEX2 = ComboMenu.GetBoolean("combo.useEX2") && (ComboMenu.GetIntSlider("combo.useEX2.minEnergyBars") * 25 <= JadeHero.Energized.Energy);
            if (!isCastingOrChanneling && ComboMenu.GetBoolean("combo.useQ.near") && MiscUtils.CanCast(AbilitySlot.Ability4) && JadeHero.EnemiesAroundAlive(2f) > 0)
            {
                if (!useEX2)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
                }
                else
                {
                    LocalPlayer.PressAbility(AbilitySlot.EXAbility2, true);
                }
            }

            if (!isCastingOrChanneling && ComboMenu.GetBoolean("combo.useQ.reset") && MiscUtils.CanCast(AbilitySlot.Ability4)
                && LocalPlayer.GetAbilityHudData(AbilitySlot.Ability2).CooldownLeft > 0f)
            {
                if (!useEX2)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
                }
                else
                {
                    LocalPlayer.PressAbility(AbilitySlot.EXAbility2, true);
                }
            }

            var myPos = JadeHero.MapObject.Position;

            if (isCastingOrChanneling)
            {
                LocalPlayer.EditAimPosition = true;

                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability3: //Space
                        var priorityMode = ComboMenu.GetComboBox("combo.useSpace.direction");
                        var accuracy = ComboMenu.GetIntSlider("combo.useSpace.accuracy");
                        var bestPosition = GetBestJumpPosition(priorityMode, accuracy);

                        if (DrawingsMenu.GetBoolean("draw.debugJumpToSafety"))
                        {
                            Drawing.DrawCircleOneShot(bestPosition, 2f, UnityEngine.Color.yellow, 2f);
                        }

                        LocalPlayer.Aim(bestPosition);
                        break;

                    case AbilitySlot.Ability5: //E
                        if (ETarget != null)
                        {
                            var testPred = TestPrediction.GetNormalLinePrediction(myPos, ETarget, ERange, ESpeed, ERadius, true);

                            if (testPred.CanHit)
                            {
                                if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, ERadius))
                                {
                                    LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                                }
                                else
                                {
                                    LocalPlayer.Aim(testPred.CastPosition);
                                }
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
                        if (M2_FTarget != null)
                        {
                            var testPred = TestPrediction.GetNormalLinePrediction(myPos, M2_FTarget, M2Range, FSpeed, FRadius, true);

                            if (testPred.CanHit)
                            {
                                LocalPlayer.Aim(testPred.CastPosition);
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
                        if (RTarget != null)
                        {
                            var testPred = TestPrediction.GetNormalLinePrediction(myPos, RTarget, RRange, RSpeed, RRadius, true);

                            if (testPred.CanHit)
                            {
                                if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, RRadius))
                                {
                                    LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                                }
                                else
                                {
                                    LocalPlayer.Aim(testPred.CastPosition);
                                }
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
                        if (M2_FTarget != null)
                        {
                            var testPred = TestPrediction.GetNormalLinePrediction(myPos, M2_FTarget, M2Range, M2Speed, M2Radius, true);

                            if (testPred.CanHit)
                            {
                                if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, M2Radius))
                                {
                                    LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                                }
                                else
                                {
                                    LocalPlayer.Aim(testPred.CastPosition);
                                }
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
                        if (M1Target != null)
                        {
                            var testPred = TestPrediction.GetNormalLinePrediction(myPos, M1Target, M1Range, M1Speed, M1Radius, true);

                            if (testPred.CanHit)
                            {
                                if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, M1Radius))
                                {
                                    LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                                }
                                else
                                {
                                    LocalPlayer.Aim(testPred.CastPosition);
                                }
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

            if (!isCastingOrChanneling && ComboMenu.GetBoolean("combo.useSpace") && MiscUtils.CanCast(AbilitySlot.Ability3) && JadeHero.EnemiesAroundAlive(ComboMenu.GetSlider("combo.useSpace.maxRange")) > 0)
            {
                if (!MiscUtils.HasBuff(JadeHero, "Stealth")) //Not stealthed
                {
                    var priorityMode = ComboMenu.GetComboBox("combo.useSpace.direction");
                    var accuracy = ComboMenu.GetIntSlider("combo.useSpace.accuracy");
                    var bestPosition = GetBestJumpPosition(priorityMode, accuracy);

                    if (DrawingsMenu.GetBoolean("draw.debugJumpToSafety"))
                    {
                        Drawing.DrawCircleOneShot(bestPosition, 2f, UnityEngine.Color.yellow, 2f);
                    }

                    LocalPlayer.PressAbility(AbilitySlot.Ability3, true);
                    LocalPlayer.EditAimPosition = true;
                    LocalPlayer.Aim(bestPosition);
                    return;
                }
            }

            if (ComboMenu.GetBoolean("combo.useE") && MiscUtils.CanCast(AbilitySlot.Ability5))
            {
                if (LastAbilityFired == null && ETarget != null)
                {
                    var testPred = TestPrediction.GetNormalLinePrediction(myPos, ETarget, ERange, ESpeed, ERadius, true);

                    if (testPred.CanHit)
                    {
                        if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, ERadius))
                        {
                            //Do nothing
                        }
                        else
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability5, true);
                            LocalPlayer.EditAimPosition = true;
                            LocalPlayer.Aim(testPred.CastPosition);
                            return;
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useF") && MiscUtils.CanCast(AbilitySlot.Ability7))
            {
                if (JadeHero.EnemiesAroundAlive(ComboMenu.GetSlider("combo.useF.safeRange")) == 0)
                {
                    if (LastAbilityFired == null && M2_FTarget != null)
                    {
                        var testPred = TestPrediction.GetNormalLinePrediction(myPos, M2_FTarget, M2Range, FSpeed, FRadius, true);

                        if (testPred.CanHit)
                        {
                            if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, FRadius))
                            {
                                //Do nothing
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Ability7, true);
                                LocalPlayer.EditAimPosition = true;
                                LocalPlayer.Aim(testPred.CastPosition);
                                return;
                            }
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useR") && MiscUtils.CanCast(AbilitySlot.Ability6))
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useR.minEnergyBars") * 25;
                if (energyRequired <= JadeHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && RTarget != null)
                    {
                        var testPred = TestPrediction.GetNormalLinePrediction(myPos, RTarget, RRange, RSpeed, RRadius, true);

                        if (testPred.CanHit)
                        {
                            if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, RRadius))
                            {
                                //Do nothing
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                                LocalPlayer.EditAimPosition = true;
                                LocalPlayer.Aim(testPred.CastPosition);
                                return;
                            }
                        }
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
                        var testPred = TestPrediction.GetNormalLinePrediction(myPos, M2_FTarget, M2Range, M2Speed, M2Radius, true);

                        if (testPred.CanHit)
                        {
                            if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, M2Radius))
                            {
                                //Do nothing
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                                return;
                            }
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useM2") && MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                if (JadeHero.EnemiesAroundAlive(ComboMenu.GetSlider("combo.useM2.safeRange")) == 0)
                {
                    if (LastAbilityFired == null && M2_FTarget != null)
                    {
                        var testPred = TestPrediction.GetNormalLinePrediction(myPos, M2_FTarget, M2Range, M2Speed, M2Radius, true);

                        if (testPred.CanHit)
                        {
                            if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, M2Radius))
                            {
                                //Do nothing
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                                return;
                            }
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useM1") && JadeHero.Blessings.Blessings > 0)
            {
                if (LastAbilityFired == null && M1Target != null)
                {
                    var testPred = TestPrediction.GetNormalLinePrediction(myPos, M1Target, M1Range, M1Speed, M1Radius, true);

                    if (testPred.CanHit)
                    {
                        if (noBubble && WillCollideWithEnemyBubble(myPos, testPred.CastPosition, M1Radius))
                        {
                            //Do nothing
                        }
                        else
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                            LocalPlayer.EditAimPosition = true;
                            LocalPlayer.Aim(testPred.CastPosition);
                            return;
                        }
                    }
                }
            }
        }

        private static void KillstealMode()
        {
            var invisibleEnemies = KSMenu.GetBoolean("ks.invisibleTargets");

            var possibleEnemies = invisibleEnemies ? EntitiesManager.EnemyTeam.AsEnumerable() : EntitiesManager.EnemyTeam.Where(x => !x.CharacterModel.IsModelInvisible);
            possibleEnemies = possibleEnemies.Where(x => x.IsValid && !x.Living.IsDead && !x.IsCountering && !x.HasConsumeBuff && !x.PhysicsCollision.IsImmaterial);

            var myPos = JadeHero.MapObject.Position;

            foreach (var enemy in possibleEnemies)
            {
                if (KSMenu.GetBoolean("ks.useEX1") && LastAbilityFired == null && enemy.Living.Health <= (!HasDeadlyFocus ? 12f : 12f + 5f) && enemy.Distance(JadeHero) < M2Range && MiscUtils.CanCast(AbilitySlot.EXAbility1)) //EX1
                {
                    var testPred = TestPrediction.GetNormalLinePrediction(myPos, enemy, M2Range, M2Speed, M2Radius, true);

                    if (testPred.CanHit)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                    }
                }

                if (KSMenu.GetBoolean("ks.useR") && LastAbilityFired == null && enemy.Living.Health <= 6f && enemy.Distance(JadeHero) < RRange && MiscUtils.CanCast(AbilitySlot.Ability6)) //R
                {
                    var testPred = TestPrediction.GetNormalLinePrediction(myPos, enemy, RRange, RSpeed, RRadius, true);

                    if (testPred.CanHit)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                        LocalPlayer.EditAimPosition = true;
                        LocalPlayer.Aim(testPred.CastPosition);
                    }
                }

                if (KSMenu.GetBoolean("ks.useR") && LastAbilityFired == null && enemy.Living.Health <= 6f * 3f && enemy.Distance(JadeHero) < 1.25f && MiscUtils.CanCast(AbilitySlot.Ability6)) //R
                {
                    var testPred = TestPrediction.GetNormalLinePrediction(myPos, enemy, RRange, RSpeed, RRadius, true);

                    if (testPred.CanHit)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                        LocalPlayer.EditAimPosition = true;
                        LocalPlayer.Aim(testPred.CastPosition);
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
                else if (MiscUtils.CanCast(AbilitySlot.Ability2) && orbHealth > 6f * 4f && orbHealth <= (!HasDeadlyFocus ? 38f : 38f + 5f) && false)
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
            //Drawing.DrawString(new Vector2(1280f / 2f, 1024f / 2f), "Game.IsInGame: " + Game.IsInGame, UnityEngine.Color.white, ViewSpace.ScreenSpacePixels);

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

            foreach (var obj in SpecialCircleObjects)
            {
                if (obj.Active)
                {
                    Drawing.DrawCircle(obj.Position, obj.Radius, UnityEngine.Color.green);
                }
            }

            Drawing.DrawString(new Vector2(1920f / 2f, 1080f / 2f - 5f),
                "Targeting mode: " + (KeysMenu.GetKeybind("keys.changeTargeting") ? "LowestHealth" : "NearMouse"), UnityEngine.Color.yellow, ViewSpace.ScreenSpacePixels);

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

            if (DrawingsMenu.GetBoolean("draw.rangeSpace.safeRange"))
            {
                Drawing.DrawCircle(JadeHero.MapObject.Position, ComboMenu.GetSlider("combo.useSpace.maxRange"), UnityEngine.Color.blue);
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
                var abilitySpace = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability3);
                if (abilitySpace != null)
                {
                    var drawSpacePos = new Vector2(760f, 1080f - 350f);
                    var abilitySpaceReady = MiscUtils.CanCast(AbilitySlot.Ability3);
                    var textToDrawSpace = "Space state: " + (abilitySpaceReady ? "Ready" : Math.Round(abilitySpace.CooldownLeft, 2).ToString());
                    Drawing.DrawString(drawSpacePos, textToDrawSpace, abilitySpaceReady ? UnityEngine.Color.cyan : UnityEngine.Color.gray, ViewSpace.ScreenSpacePixels);
                }

                var abilityQ = LocalPlayer.GetAbilityHudData(AbilitySlot.Ability4);
                if (abilityQ != null)
                {
                    var drawQPos = new Vector2(1920f - 760f, 1080f - 350f);
                    var abilityQReady = MiscUtils.CanCast(AbilitySlot.Ability4);
                    var textToDrawQ = "Q state: " + (abilityQReady ? "Ready" : Math.Round(abilityQ.CooldownLeft, 2).ToString());
                    Drawing.DrawString(drawQPos, textToDrawQ, abilityQReady ? UnityEngine.Color.cyan : UnityEngine.Color.gray, ViewSpace.ScreenSpacePixels);
                }
            }

            if (DrawingsMenu.GetBoolean("draw.debugTestPred"))
            {
                Drawing.DrawString(JadeHero.MapObject.Position, JadeHero.NetworkMovement.Velocity.ToString(), UnityEngine.Color.cyan);

                var aliveEnemies = EntitiesManager.EnemyTeam.Where(x => !x.Living.IsDead);

                foreach (var enemy in aliveEnemies)
                {
                    Drawing.DrawString(enemy.MapObject.Position, enemy.NetworkMovement.Velocity.ToString(), UnityEngine.Color.green);

                    var testPred = TestPrediction.GetNormalLinePrediction(JadeHero.MapObject.Position, enemy, M2Range, M2Speed, M2Radius);

                    if (testPred.CanHit)
                    {
                        Drawing.DrawCircle(testPred.CastPosition, 1f, UnityEngine.Color.red);
                    }

                    if (testPred.CollisionResult != null ? testPred.CollisionResult.IsColliding : false)
                    {
                        Drawing.DrawCircle(testPred.CollisionResult.CollisionPoint, 1f, UnityEngine.Color.blue);
                    }
                }
            }

            if (DrawingsMenu.GetBoolean("draw.debugJumpToSafety"))
            {
                Drawing.DrawString(new Vector2(100f, 1080f - 30f), "Direction: " + ComboMenu.GetComboBox("combo.useSpace.direction").ToString(), UnityEngine.Color.cyan, ViewSpace.ScreenSpacePixels);
            }
        }

        private static AbilitySlot? CastingIndexToSlot(int index)
        {
            switch (index)
            {
                case 0:
                case 1:
                    return AbilitySlot.Ability1;
                case 2:
                    return AbilitySlot.Ability4;
                case 3:
                    return AbilitySlot.Ability2;
                case 4:
                    return AbilitySlot.EXAbility1;
                case 5:
                    return AbilitySlot.EXAbility2;
                case 6:
                case 10:
                    return AbilitySlot.Ability3;
                case 7:
                    return AbilitySlot.Ability5;
                case 8:
                    return AbilitySlot.Ability6;
                case 9:
                    return AbilitySlot.Ability7;
                case 11:
                    return AbilitySlot.Mount;
            }

            return null;
        }

        private static Vector2 GetBestJumpPosition(int towards, int pointsToConsider)
        {
            var allies = EntitiesManager.LocalTeam.Where(x => !x.IsLocalPlayer && !x.Living.IsDead);

            var maxJumpDistance = !HasExplosiveJump ? SpaceRange : SpaceRange + (SpaceRange * 20f / 100f);

            var alliesInRange = allies
                .Where(x => x.Distance(JadeHero) <= maxJumpDistance)
                .OrderByDescending(x => x.Distance(JadeHero));

            var alliesNotInRange = allies
                .Except(alliesInRange)
                .OrderBy(x => x.Distance(JadeHero));


            switch (towards)
            {
                case 0: //Closest to edge
                    foreach (var ally in alliesInRange)
                    {
                        if (ally.EnemiesAroundAlive(4.5f) == 0)
                        {
                            return ally.MapObject.Position;
                        }
                    }

                    foreach (var ally in alliesNotInRange)
                    {
                        if (Math.Abs(JadeHero.Distance(ally) - maxJumpDistance) <= 4.5f)
                        {
                            if (ally.EnemiesAroundAlive(4.5f) == 0)
                            {
                                return ally.MapObject.Position;
                            }
                        }
                    }

                    //No directly safe ally, lets find spots in our circumference
                    List<Vector2> PossibleSafeSpots = new List<Vector2>();

                    var sectorAngle = 2 * Math.PI / pointsToConsider;
                    for (int i = 0; i < pointsToConsider; i++)
                    {
                        var angleIteration = sectorAngle * i;

                        Vector2 point = new Vector2
                        {
                            X = (int)(JadeHero.MapObject.Position.X + maxJumpDistance * Math.Cos(angleIteration)),
                            Y = (int)(JadeHero.MapObject.Position.Y + maxJumpDistance * Math.Sin(angleIteration))
                        };

                        PossibleSafeSpots.Add(point);
                    }

                    if (DrawingsMenu.GetBoolean("draw.debugJumpToSafety"))
                    {
                        foreach (var p in PossibleSafeSpots)
                        {
                            Drawing.DrawCircleOneShot(p, 1f, UnityEngine.Color.green, 2f);
                        }
                    }

                    //No ally is safe, let's just find a safe spot in the circumference that is closest to the ally who in turn is closest to our jump distance

                    var orderedByEdgeDistance = allies.OrderBy(x => Math.Abs(JadeHero.Distance(x) - maxJumpDistance));

                    foreach (var ally in orderedByEdgeDistance)
                    {
                        var orderedPoints = PossibleSafeSpots.OrderBy(x => x.Distance(ally));
                        foreach (var point in orderedPoints)
                        {
                            if (point.EnemiesAroundAlive(4.5f) == 0)
                            {
                                return point;
                            }
                        }
                    }

                    break;

                case 1: // Straight to mouse pos
                    return InputManager.MousePosition.ScreenToWorld();
            }

            //If all else fails, just return mousepos
            return InputManager.MousePosition.ScreenToWorld();
        }

        private static bool WillCollideWithEnemyBubble(Vector2 fromPos, Vector2 toPos, float projRadius)
        {
            if (SpecialCircleObjects.All(x => x.Active == false))
            {
                return false;
            }

            foreach (var special in SpecialCircleObjects)
            {
                if (special.Active == false)
                {
                    continue;
                }

                var result = Geometry.CircleVsThickLine(special.Position, special.Radius, fromPos, toPos, projRadius, false);
                if (result)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class SpecialEnemyCircleObject
    {
        public string Name { get; private set; }
        public float Radius { get; set; }
        public bool Active { get; set; }
        public Vector2 Position { get; set; }

        public SpecialEnemyCircleObject(string name, float radius, bool active = false, Vector2 position = default(Vector2))
        {
            Name = name;
            Radius = radius;
            Active = active;
            Position = position;
        }
    }
}
