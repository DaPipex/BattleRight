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

namespace PipKaan
{
    public class PipKaan : IAddon
    {
        private const bool _debugMode = false;

        private static Menu KaanMenu;
        private static Menu KeysMenu, ComboMenu, DrawMenu, DebugMenu;

        private static Character KaanHero => LocalPlayer.Instance; 
        private static readonly string HeroName = "Ruh Kaan";

        private const float M2Speed = 25f;
        private const float SpaceAirTime = 0.3f;
        private const float ESpeed = 23.5f;
        private const float RAirTime = 0.8f;
        private const float F_M2Speed = 23.5f;

        private const float M2Radius = 0.35f;
        private const float SpaceRadius = 2f;
        private const float ERadius = 0.35f;
        private const float RRadius = 1.8f;
        private const float F_M2Radius = 0.35f;

        private const float M1Range = 2.5f;
        private const float M2Range = 11f;
        private const float SpaceMinRange = 3f;
        private const float SpaceMaxRange = 4.5f;
        private const float ERange = 7.1f;
        private const float RRange = 8f;
        private const float EX1Range = 2.5f;
        private const float FRange = 5f;
        private const float F_M1Range = 2.5f;
        private const float F_M2Range = 9.6f;

        private static readonly UnityEngine.Color RangeColor = new UnityEngine.Color(176f / 255f, 51f / 255f, 230f / 255f);
        private static readonly UnityEngine.Color MinRangeColor = new UnityEngine.Color(239f / 255f, 122f / 255f, 164f / 255f);
        private static readonly UnityEngine.Color SafeRangeColor = new UnityEngine.Color(231f / 255f, 240f / 255f, 40f / 255f);

        private static readonly List<Battlerite> Battlerites = new List<Battlerite>(5);

        private static bool HasTenaciousDemon = false;
        private static bool HasNetherBlade = false;

        private static AbilitySlot? LastAbilityFired = null;

        private static bool DidMatchInit = false;

        public void OnInit()
        {
            InitMenu();

            Game.OnMatchStart += OnMatchStart;
            Game.OnMatchEnd += OnMatchEnd;
        }

        private void OnMatchStart(EventArgs args)
        {
            if (KaanHero == null || !KaanHero.CharName.Equals(HeroName))
            {
                return;
            }

            //KaanHero = LocalPlayer.Instance;

            Game.OnUpdate += OnUpdate;
            Game.OnDraw += OnDraw;
            Game.OnMatchStateUpdate += OnMatchStateUpdate;

            if (Game.CurrentMatchState == MatchState.InRound)
            {
                GetBattlerites();
            }

            DidMatchInit = true;
        }

        private void OnMatchEnd(EventArgs args)
        {
            if (DidMatchInit)
            {
                Game.OnUpdate -= OnUpdate;
                Game.OnDraw -= OnDraw;
                Game.OnMatchStateUpdate -= OnMatchStateUpdate;

                //KaanHero = null;

                DidMatchInit = false;
            }
        }

        private void OnMatchStateUpdate(MatchStateUpdate args)
        {
            if (KaanHero == null)
            {
                return;
            }

            if (args.OldMatchState == MatchState.BattleritePicking || args.NewMatchState == MatchState.PreRound)
            {
                //KaanHero = LocalPlayer.Instance;
                GetBattlerites();
            }
        }

        private static void GetBattlerites()
        {
            if (KaanHero == null)
            {
                return;
            }

            if (Battlerites.Any())
            {
                Battlerites.Clear();
            }

            for (var i = 0; i < 5; i++)
            {
                var br = KaanHero.BattleriteSystem.GetEquippedBattlerite(i);
                if (br != null)
                {
                    Battlerites.Add(br);
                }
            }

            HasTenaciousDemon = Battlerites.Any(x => x.Name.Equals("TenaciousDemonUpgrade"));
            HasNetherBlade = Battlerites.Any(x => x.Name.Equals("NetherBladeUpgrade"));
        }

        private void OnUpdate(EventArgs args)
        {
            if (_debugMode)
            {
                DebugUpdate();
            }

            if (KeysMenu.GetKeybind("keys.combo"))
            {
                ComboMode();
            }
            else if (/*KeysMenu.GetKeybind("keys.orb")*/false)
            {
                //OrbMode();
            }
            else if (/*KeysMenu.GetKeybind("keys.heal")*/false)
            {
                //HealTeammate();
            }
            else
            {
                LocalPlayer.EditAimPosition = false;
                LastAbilityFired = null;
            }
        }

        private static void DebugUpdate()
        {
            if (DebugMenu.GetBoolean("debug.updateBattlerites"))
            {
                GetBattlerites();
                DebugMenu.SetBoolean("debug.updateBattlerites", false);
            }
        }

        private static void ComboMode()
        {
            var targetModeKey = KeysMenu.GetKeybind("keys.changeTargeting");
            var targetMode = targetModeKey ? TargetingMode.LowestHealth : TargetingMode.NearMouse;

            var invisibleTargets = ComboMenu.GetBoolean("combo.invisibleTargets");

            var enemiesToTarget = EntitiesManager.EnemyTeam.Where(x => x.IsValid && !x.Living.IsDead
            && !x.PhysicsCollision.IsImmaterial && !x.IsCountering && !x.HasShield() && !x.HasConsumeBuff && !x.HasParry());

            if (!invisibleTargets)
            {
                enemiesToTarget = enemiesToTarget.Where(x => !x.CharacterModel.IsModelInvisible);
            }

            var M1Target = TargetSelector.GetTarget(enemiesToTarget, targetMode, M1Range);
            var M2Target = TargetSelector.GetTarget(enemiesToTarget, targetMode, M2Range);
            var SpaceTarget = TargetSelector.GetTarget(enemiesToTarget, targetMode, SpaceMaxRange);
            var ETarget = TargetSelector.GetTarget(enemiesToTarget, targetMode, ERange);
            var RTarget = TargetSelector.GetTarget(enemiesToTarget, targetMode, RRange);
            var F_M2Target = TargetSelector.GetTarget(enemiesToTarget, targetMode, F_M2Range);

            var isCastingOrChanneling = KaanHero.AbilitySystem.IsCasting || KaanHero.IsChanneling || KaanHero.HasBuff("ConsumeBuff") || KaanHero.HasBuff("ReapingScytheBuff");

            if (isCastingOrChanneling && LastAbilityFired == null)
            {
                LastAbilityFired = CastingIndexToSlot(KaanHero.AbilitySystem.CastingAbilityIndex);
            }

            var myPos = KaanHero.MapObject.Position;

            if (isCastingOrChanneling)
            {
                LocalPlayer.EditAimPosition = true;

                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability4:
                        Projectile enemyProj;
                        if (EnemyProjectileGoingToHitUnit(KaanHero, out enemyProj))
                        {
                            LocalPlayer.Aim(enemyProj.MapObject.Position);
                        }
                        else
                        {
                            LocalPlayer.Aim(InputManager.MousePosition.ScreenToWorld());
                        }
                        break;

                    case AbilitySlot.Ability5:
                        if (ETarget != null)
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(myPos, ETarget, ERange, ESpeed, ERadius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                        }
                        else
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                        }
                        break;

                    case AbilitySlot.Ability2:
                        if (LocalPlayer.GetAbilityHudData(AbilitySlot.Ability2).Name.Equals("ShadowBoltAbility")) //Normal mode
                        {
                            if (M2Target != null)
                            {
                                var pred = TestPrediction.GetNormalLinePrediction(myPos, M2Target, M2Range, M2Speed, M2Radius, true);
                                if (pred.CanHit)
                                {
                                    LocalPlayer.Aim(pred.CastPosition);
                                }
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        else //Ulti mode
                        {
                            if (F_M2Target != null)
                            {
                                var pred = TestPrediction.GetNormalLinePrediction(myPos, F_M2Target, F_M2Range, F_M2Speed, F_M2Radius, true);
                                if (pred.CanHit)
                                {
                                    LocalPlayer.Aim(pred.CastPosition);
                                }
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        break;

                    case AbilitySlot.Ability6:
                        if (RTarget != null)
                        {
                            var pred = TestPrediction.GetPrediction(myPos, RTarget, RRange, 0f, RRadius, RAirTime);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                        }
                        break;

                    case AbilitySlot.Ability3:
                        if (SpaceTarget != null)
                        {
                            var pred = TestPrediction.GetPrediction(myPos, SpaceTarget, SpaceMaxRange, 0f, SpaceRadius, SpaceAirTime);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                        }
                        break;

                    case AbilitySlot.Ability1:
                        if (M1Target != null)
                        {
                            LocalPlayer.Aim(M1Target.MapObject.Position);
                        }
                        else
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                        }
                        break;
                }
            }
            else
            {
                LocalPlayer.EditAimPosition = false;
                LastAbilityFired = null;
            }

            if (ComboMenu.GetBoolean("combo.useQ") && MiscUtils.CanCast(AbilitySlot.Ability4) && !MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                Projectile closestProj;
                if (EnemyProjectileGoingToHitUnit(KaanHero, out closestProj))
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
                    LocalPlayer.EditAimPosition = true;
                    LocalPlayer.Aim(closestProj.MapObject.Position);
                }
            }

            if (ComboMenu.GetBoolean("combo.useF") && MiscUtils.CanCast(AbilitySlot.Ability7) && LocalPlayer.GetAbilityHudData(AbilitySlot.Ability7).Name.Equals("ShadowBeastAbility"))
            {
                if (LastAbilityFired == null && KaanHero.EnemiesAroundAlive(FRange) > 0)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability7, true);
                }
            }

            if (ComboMenu.GetBoolean("combo.useE") && MiscUtils.CanCast(AbilitySlot.Ability5))
            {
                if (LastAbilityFired == null && ETarget != null && ETarget.Distance(KaanHero) > ComboMenu.GetSlider("combo.useE.minRange"))
                {
                    var pred = TestPrediction.GetNormalLinePrediction(myPos, ETarget, ERange, ESpeed, ERadius, true);
                    if (pred.CanHit)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability5, true);
                        LocalPlayer.EditAimPosition = true;
                        LocalPlayer.Aim(pred.CastPosition);
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useM2") && MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                if (LocalPlayer.GetAbilityHudData(AbilitySlot.Ability2).Name.Equals("ShadowBoltAbility"))
                {
                    if (LastAbilityFired == null && M2Target != null
                        && KaanHero.EnemiesAroundAlive(ComboMenu.GetSlider("combo.useM2.safeRange")) == 0)
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(myPos, M2Target, M2Range, M2Speed, M2Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                            LocalPlayer.EditAimPosition = true;
                            LocalPlayer.Aim(pred.CastPosition);
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.ultiMode.useM2") && MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                if (!LocalPlayer.GetAbilityHudData(AbilitySlot.Ability2).Name.Equals("ShadowBoltAbility"))
                {
                    if (LastAbilityFired == null && F_M2Target != null
                        && F_M2Target.Distance(KaanHero) > ComboMenu.GetSlider("combo.ultiMode.useM2.minRange"))
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(myPos, F_M2Target, F_M2Range, F_M2Speed, F_M2Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                            LocalPlayer.EditAimPosition = true;
                            LocalPlayer.Aim(pred.CastPosition);
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useR") && MiscUtils.CanCast(AbilitySlot.Ability6) && !KaanHero.IsWeaponCharged && HasNetherBlade)
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useR.minEnergyBars") * 25;
                if (energyRequired <= KaanHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && RTarget != null)
                    {
                        var pred = TestPrediction.GetPrediction(myPos, RTarget, RRange, 0f, RRadius, RAirTime);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability6, true);
                            LocalPlayer.EditAimPosition = true;
                            LocalPlayer.Aim(pred.CastPosition);
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useEX1") && MiscUtils.CanCast(AbilitySlot.EXAbility1))
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useEX1.minEnergyBars") * 25;
                if (energyRequired <= KaanHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && M1Target != null)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useSpace") && MiscUtils.CanCast(AbilitySlot.Ability3))
            {
                if (LastAbilityFired == null && SpaceTarget != null)
                {
                    var pred = TestPrediction.GetPrediction(myPos, SpaceTarget, SpaceMaxRange, 0f, SpaceRadius, SpaceAirTime);
                    if (pred.CanHit)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability3, true);
                        LocalPlayer.EditAimPosition = true;
                        LocalPlayer.Aim(pred.CastPosition);
                    }
                }
            }

            if ((ComboMenu.GetBoolean("combo.useM1") || ComboMenu.GetBoolean("combo.ultiMode.useM1")) && MiscUtils.CanCast(AbilitySlot.Ability1))
            {
                if (LastAbilityFired == null && M1Target != null)
                {
                    LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                    LocalPlayer.EditAimPosition = true;
                    LocalPlayer.Aim(M1Target.MapObject.Position);
                }
            }
        }

        private void OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (DrawMenu.GetBoolean("draw.disableAll"))
            {
                return;
            }

            if (_debugMode)
            {
                DebugDraw();
            }

            Drawing.DrawString(new Vector2(1920f / 2f, 1080f / 2f - 5f),
                "Targeting mode: " + (KeysMenu.GetKeybind("keys.changeTargeting") ? "LowestHealth" : "NearMouse"), UnityEngine.Color.yellow, ViewSpace.ScreenSpacePixels);

            var myPos = KaanHero.MapObject.Position;

            if (DrawMenu.GetBoolean("draw.rangeM2"))
            {
                Drawing.DrawCircle(myPos, M2Range, RangeColor);
            }

            if (DrawMenu.GetBoolean("draw.rangeM2.safeRange"))
            {
                Drawing.DrawCircle(myPos, ComboMenu.GetSlider("combo.useM2.safeRange"), SafeRangeColor);
            }

            if (DrawMenu.GetBoolean("draw.rangeE"))
            {
                Drawing.DrawCircle(myPos, ERange, RangeColor);
            }

            if (DrawMenu.GetBoolean("draw.rangeE.minRange"))
            {
                Drawing.DrawCircle(myPos, ComboMenu.GetSlider("combo.useE.minRange"), MinRangeColor);
            }

            if (DrawMenu.GetBoolean("draw.ultiMode.rangeM2"))
            {
                Drawing.DrawCircle(myPos, F_M2Range, RangeColor);
            }

            if (DrawMenu.GetBoolean("draw.ultiMode.rangeM2.minRange"))
            {
                Drawing.DrawCircle(myPos, ComboMenu.GetSlider("combo.ultiMode.useM2.minRange"), MinRangeColor);
            }
        }

        private static void DebugDraw()
        {
            if (DebugMenu.GetBoolean("debug.checkBattlerites"))
            {
                var pos = new Vector2(100f, 100f);
                Drawing.DrawString(pos, "Tenacious Demon: " + HasTenaciousDemon, UnityEngine.Color.cyan, ViewSpace.ScreenSpacePixels);
                Drawing.DrawString(new Vector2(pos.X, pos.Y + 20f), "Nether Blade: " + HasNetherBlade, UnityEngine.Color.cyan, ViewSpace.ScreenSpacePixels);
            }
        }

        private static void InitMenu()
        {
            KaanMenu = new Menu("pipkaanmenu", "DaPip's Ruh Kaan", false);

            KeysMenu = new Menu("keysmenu", "Keys", true);
            KeysMenu.Add(new MenuKeybind("keys.combo", "Combo key", UnityEngine.KeyCode.LeftControl));
            //KeysMenu.Add(new MenuKeybind("keys.orb", "Orb mode", UnityEngine.KeyCode.Mouse3));
            //KeysMenu.Add(new MenuKeybind("keys.heal", "Heal teammate", UnityEngine.KeyCode.G));
            KeysMenu.Add(new MenuKeybind("keys.changeTargeting", "Change targeting mode", UnityEngine.KeyCode.T, false, true));
            KaanMenu.Add(KeysMenu);

            ComboMenu = new Menu("combomenu", "Combo", true);
            ComboMenu.Add(new MenuCheckBox("combo.invisibleTargets", "Attack invisible targets", true));
            ComboMenu.Add(new MenuCheckBox("combo.useM1", "Use Left-Mouse (Defiled Blade)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useM2", "Use Right-Mouse (Shadowbolt)", true));
            ComboMenu.Add(new MenuSlider("combo.useM2.safeRange", "    ^ Safe range", 4.5f, M2Range - 1f, 0f));
            ComboMenu.Add(new MenuCheckBox("combo.useSpace", "Use Space (Sinister Strike)", false));
            ComboMenu.Add(new MenuCheckBox("combo.useQ", "Use Q (Consume) to reset Right-Mouse", true));
            ComboMenu.Add(new MenuCheckBox("combo.useE", "Use E (Claw of the wicked)", true));
            ComboMenu.Add(new MenuSlider("combo.useE.minRange", "    ^ Minimum range", M1Range, ERange - 1f, 0f));
            ComboMenu.Add(new MenuCheckBox("combo.useR", "Use R (Nether Void) to refill Left-Mouse (if Nether Blade is active)", false));
            ComboMenu.Add(new MenuIntSlider("combo.useR.minEnergyBars", "    ^ Min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useEX1", "Use EX1 (Reaping Scythe)", false));
            ComboMenu.Add(new MenuIntSlider("combo.useEX1.minEnergyBars", "    ^ Min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useF", "Use F (Shadow Beast) when there's an enemy in range", true));

            ComboMenu.AddSeparator(10f);
            ComboMenu.AddLabel("While in Ultimate (Shadow Beast) Mode");
            ComboMenu.Add(new MenuCheckBox("combo.ultiMode.useM1", "Use Left-Mouse (Fang of the faceless)", true));
            ComboMenu.Add(new MenuCheckBox("combo.ultiMode.useM2", "Use Right-Mouse (Shadow Claw)", true));
            ComboMenu.Add(new MenuSlider("combo.ultiMode.useM2.minRange", "    ^ Min Range", F_M1Range, F_M2Range - 1f, 0f));
            KaanMenu.Add(ComboMenu);

            DrawMenu = new Menu("drawingsmenu", "Drawings", true);
            DrawMenu.Add(new MenuCheckBox("draw.disableAll", "Disable all drawings", false));
            DrawMenu.Add(new MenuCheckBox("draw.rangeM2", "Draw Right-Mouse Range (Shadowbolt)", true));
            DrawMenu.Add(new MenuCheckBox("draw.rangeM2.safeRange", "Draw Right-Mouse Safe-Range", false));
            DrawMenu.Add(new MenuCheckBox("draw.rangeE", "Draw E Range (Claw of the wicked)", true));
            DrawMenu.Add(new MenuCheckBox("draw.rangeE.minRange", "Draw E Minimum range to use (Claw of the wicked)", false));
            DrawMenu.AddSeparator(10f);
            DrawMenu.AddLabel("While in Ultimate (Shadow Beast) Ranges");
            DrawMenu.Add(new MenuCheckBox("draw.ultiMode.rangeM2", "Draw Right-Mouse Range (Shadow Claw)", false));
            DrawMenu.Add(new MenuCheckBox("draw.ultiMode.rangeM2.minRange", "Draw Right-Mouse Minimum range to use (Shadow Claw)", false));
            KaanMenu.Add(DrawMenu);

            DebugMenu = new Menu("debugmenu", "Debug", true);
            DebugMenu.Add(new MenuCheckBox("debug.updateBattlerites", "Update battlerites", false));
            DebugMenu.Add(new MenuCheckBox("debug.checkBattlerites", "Check battlerites status", false));
            if (_debugMode)
            {
                KaanMenu.Add(DebugMenu);
            }

            MainMenu.AddMenu(KaanMenu);
        }

        private static AbilitySlot? CastingIndexToSlot(int index)
        {
            switch (index)
            {
                case 8:
                case 9:
                case 13:
                case 14:
                case 18:
                    return AbilitySlot.Ability1;
                case 3:
                case 10:
                    return AbilitySlot.Ability2;
                case 0:
                    return AbilitySlot.Ability3;
                case 2:
                    return AbilitySlot.Ability4;
                case 4:
                    return AbilitySlot.Ability5;
                case 6:
                    return AbilitySlot.Ability6;
                case 11:
                case 12:
                    return AbilitySlot.Ability7;
                case 1:
                    return AbilitySlot.EXAbility1;
                case 5:
                    return AbilitySlot.EXAbility2;
                case 15:
                    return AbilitySlot.Mount;
            }

            return null;
        }

        private static bool EnemyProjectileGoingToHitUnit(InGameObject unit, out Projectile closestProj)
        {
            var unitPos = unit.Get<MapGameObject>().Position;
            var unitRadius = unit.Get<SpellCollisionObject>().SpellCollisionRadius;
            var enemyProjs = EntitiesManager.ActiveProjectiles.Where(x => x.BaseObject.TeamId != KaanHero.BaseObject.TeamId).OrderBy(x => x.MapObject.Position.Distance(unitPos));

            foreach (var enemyProj in enemyProjs)
            {
                if (Geometry.CircleVsThickLine(unitPos, unitRadius, enemyProj.StartPosition, enemyProj.CalculatedEndPosition, enemyProj.Radius, false))
                {
                    closestProj = enemyProj;
                    return true;
                }
            }

            closestProj = null;
            return false;
        }

        public void OnUnload()
        {

        }
    }
}
