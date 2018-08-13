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
        private static Menu KaanMenu;
        private static Menu KeysMenu, ComboMenu, DrawMenu;

        private static Character KaanHero;
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

        private static readonly List<Battlerite> Battlerites = new List<Battlerite>(5);

        private static bool HasTenaciousDemon;
        private static bool HasNetherBlade;

        private static AbilitySlot? LastAbilityFired = null;

        public void OnInit()
        {
            InitMenu();

            Game.OnMatchStateUpdate += OnMatchStateUpdate;
            Game.OnUpdate += OnUpdate;
            Game.OnDraw += OnDraw;
        }

        private void OnMatchStateUpdate(MatchStateUpdate args)
        {
            if (EntitiesManager.LocalPlayer.CharName != HeroName)
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
            KaanHero = EntitiesManager.LocalPlayer;

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
        }

        private void OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            KaanHero = EntitiesManager.LocalPlayer;

            if (KaanHero.CharName != HeroName)
            {
                return;
            }

            HasTenaciousDemon = Battlerites.Any(x => x.Name.Equals("TenaciousDemonUpgrade"));
            HasNetherBlade = Battlerites.Any(x => x.Name.Equals("NetherBladeUpgrade"));

            if (KeysMenu.GetKeybind("keys.combo"))
            {
                ComboMode();
            }
            else if (KeysMenu.GetKeybind("keys.orb"))
            {
                //OrbMode();
            }
            else if (KeysMenu.GetKeybind("keys.heal"))
            {
                //HealTeammate();
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

            var M1Target = TargetSelector.GetTarget(targetMode, M1Range);
            var M2Target = TargetSelector.GetTarget(targetMode, M2Range);
            var SpaceTarget = TargetSelector.GetTarget(targetMode, SpaceMaxRange);
            var ETarget = TargetSelector.GetTarget(targetMode, ERange);
            var RTarget = TargetSelector.GetTarget(targetMode, RRange);
            var F_M2Target = TargetSelector.GetTarget(targetMode, F_M2Range);

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
                        break;

                    case AbilitySlot.Ability5:
                        if (ETarget != null && !ETarget.IsCountering && !ETarget.HasShield())
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
                            if (M2Target != null && !M2Target.IsCountering && !M2Target.HasShield())
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
                            if (F_M2Target != null && !F_M2Target.IsCountering && !F_M2Target.HasShield())
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
                        if (M1Target != null && !M1Target.IsCountering && !M1Target.HasShield())
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
                if (LastAbilityFired == null && ETarget != null && !ETarget.IsCountering && !ETarget.HasShield() && ETarget.Distance(KaanHero) > ComboMenu.GetSlider("combo.useE.minRange"))
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

            if ((ComboMenu.GetBoolean("combo.useM2") || ComboMenu.GetBoolean("combo.ultiMode.useM2")) && MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                if (LocalPlayer.GetAbilityHudData(AbilitySlot.Ability2).Name.Equals("ShadowBoltAbility"))
                {
                    if (LastAbilityFired == null && M2Target != null && !M2Target.IsCountering && !M2Target.HasShield() 
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
                else
                {
                    if (LastAbilityFired == null && F_M2Target != null && !F_M2Target.IsCountering && !F_M2Target.HasShield() 
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
                if (LastAbilityFired == null && M1Target != null && !M1Target.IsCountering && !M1Target.HasShield())
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

            if (KaanHero.CharName != HeroName)
            {
                return;
            }

            Drawing.DrawString(new Vector2(1920f / 2f, 1080f / 2f - 5f),
                "Targeting mode: " + (KeysMenu.GetKeybind("keys.changeTargeting") ? "LowestHealth" : "NearMouse"), UnityEngine.Color.yellow, ViewSpace.ScreenSpacePixels);
        }

        private static void InitMenu()
        {
            KaanMenu = new Menu("pipkaanmenu", "DaPip's Ruh Kaan", false);

            KeysMenu = new Menu("keysmenu", "Keys", true);
            KeysMenu.Add(new MenuKeybind("keys.combo", "Combo key", UnityEngine.KeyCode.LeftControl));
            KeysMenu.Add(new MenuKeybind("keys.orb", "Orb mode", UnityEngine.KeyCode.Mouse3));
            KeysMenu.Add(new MenuKeybind("keys.heal", "Heal teammate", UnityEngine.KeyCode.G));
            KeysMenu.Add(new MenuKeybind("keys.changeTargeting", "Change targeting mode", UnityEngine.KeyCode.T, false, true));
            KaanMenu.Add(KeysMenu);

            ComboMenu = new Menu("combomenu", "Combo", true);
            ComboMenu.Add(new MenuCheckBox("combo.useM1", "Use Left-Mouse (Defiled Blade)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useM2", "Use Right-Mouse (Shadowbolt)", true));
            ComboMenu.Add(new MenuSlider("combo.useM2.safeRange", "    ^ Safe range", 4.5f, M2Range - 1f, 0f));
            ComboMenu.Add(new MenuCheckBox("combo.useSpace", "Use Space (Sinister Strike)", true));
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
