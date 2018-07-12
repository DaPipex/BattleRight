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

namespace PipZander
{
    public class PipZander2 : IAddon
    {
        private static Menu ZanderMenu;
        private static Menu KeysMenu, ComboMenu, HealMenu, DrawMenu;

        private static Character ZanderHero;

        private const float M1Speed = 15.5f;
        private const float M2Speed = 25f;
        private const float EAirTime = 0.9f;
        private const float EX1AirTime = 0.55f;

        private const float M1Range = 6.5f;
        private const float M2Range = 10f;
        private const float SpaceRange = 6.5f;
        private const float QRange = 9f;
        private const float ERange = 9f;
        private const float EX1Range = 10f;
        private const float FRange = 6.6f;

        private const float M1Radius = 0.25f * 2;
        private const float M2Radius = 0.4f;
        private const float QRadius = 1.8f;
        private const float ERadius = 1.9f;
        private const float EX1Radius = 2.5f;
        private const float EX2Radius = 2.5f;

        private static List<Battlerite> Battlerites = new List<Battlerite>(5);

        private static readonly string SpaceCloneName = "MirrorImage";
        private static readonly string EX2CloneName = "MindgameIllusion2";
        private static readonly string UltiCloneName = "ThePrestige";

        private static InGameObject SpaceClone = null;
        private static InGameObject EX2Clone = null;
        private static InGameObject UltiClone = null;

        private static AbilitySlot? LastAbilityFired = null;

        private static bool IsQRecast = false;

        public void OnInit()
        {
            InitMenu();

            Game.OnMatchStart += args => NullClones();
            Game.OnMatchEnd += args => NullClones();
            Game.OnMatchStateUpdate += OnMatchStateUpdate;
            Game.OnUpdate += OnUpdate;
            Game.OnDraw += OnDraw;

            InGameObject.OnCreate += OnCreate;
            InGameObject.OnDestroy += OnDestroy;
        }

        private static void NullClones()
        {
            SpaceClone = null;
            EX2Clone = null;
            UltiClone = null;
        }

        private void OnCreate(InGameObject inGameObject)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (ZanderHero.CharName != "Zander")
            {
                return;
            }

            var baseObject = inGameObject.Get<BaseGameObject>();
            if (baseObject != null && baseObject.TeamId == ZanderHero.BaseObject.TeamId)
            {
                if (inGameObject.ObjectName.Equals(SpaceCloneName))
                {
                    SpaceClone = inGameObject;
                }
                else if (inGameObject.ObjectName.Equals(EX2CloneName))
                {
                    EX2Clone = inGameObject;
                }
                else if (inGameObject.ObjectName.Equals(UltiCloneName))
                {
                    UltiClone = inGameObject;
                }
            }
        }

        private void OnDestroy(InGameObject inGameObject)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (ZanderHero.CharName != "Zander")
            {
                return;
            }

            var baseObject = inGameObject.Get<BaseGameObject>();
            if (baseObject != null && baseObject.TeamId == ZanderHero.BaseObject.TeamId)
            {
                if (inGameObject.ObjectName.Equals(SpaceCloneName))
                {
                    SpaceClone = null;
                }
                else if (inGameObject.ObjectName.Equals(EX2CloneName))
                {
                    EX2Clone = null;
                }
                else if (inGameObject.ObjectName.Equals(UltiCloneName))
                {
                    UltiClone = null;
                }
            }
        }

        private void OnMatchStateUpdate(MatchStateUpdate args)
        {
            if (args.NewMatchState == MatchState.PreRound)
            {
                NullClones();
            }
        }

        private void OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            ZanderHero = EntitiesManager.LocalPlayer;

            if (ZanderHero.CharName != "Zander")
            {
                return;
            }

            //CheckClones();

            //LocalPlayer.EditAimPosition = false;

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
            else
            {
                LocalPlayer.EditAimPosition = false;
                LastAbilityFired = null;
            }
        }

        private static void HealOthers()
        {
            var possibleAllies = EntitiesManager.LocalTeam.Where(x => !x.IsLocalPlayer && !x.Living.IsDead);

            //var allyToHealM1 = possibleAllies.Where(x => x.Distance(ZanderHero) < M1Range)
            //    .OrderBy(x => x.Living.Health)
            //    .FirstOrDefault(x => x.Living.Health < x.Living.MaxRecoveryHealth);

            var allyToHealEX1 = possibleAllies.Where(x => x.Distance(ZanderHero) < EX1Range)
                .OrderBy(x => x.Living.Health)
                .FirstOrDefault(x => x.HasHardCC() && x.EnemiesAroundAlive(5f) > 0);

            var nearMouseAllyZander = TargetSelector.GetAlly(TargetingMode.NearMouse, M1Range);
            var nearMouseAllySpace = SpaceClone == null ? null : TargetSelector.GetAlly(TargetingMode.NearMouse, M1Range, SpaceClone.Get<MapGameObject>().Position);
            var nearMouseAllyEX2 = EX2Clone == null ? null : TargetSelector.GetAlly(TargetingMode.NearMouse, M1Range, EX2Clone.Get<MapGameObject>().Position);
            var nearMouseAllyUlti = UltiClone == null ? null : TargetSelector.GetAlly(TargetingMode.NearMouse, M1Range, UltiClone.Get<MapGameObject>().Position);

            var isCastingOrChanneling = ZanderHero.AbilitySystem.IsCasting || ZanderHero.IsChanneling;

            if (isCastingOrChanneling && LastAbilityFired == null)
            {
                LastAbilityFired = CastingIndexToSlot(ZanderHero.AbilitySystem.CastingAbilityIndex);
            }

            var myPos = ZanderHero.MapObject.Position;

            if (isCastingOrChanneling)
            {
                LocalPlayer.EditAimPosition = true;
                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability1:
                        if (nearMouseAllyZander != null)
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(myPos, nearMouseAllyZander, M1Range, M1Speed, M1Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        else if (nearMouseAllyUlti != null)
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(UltiClone.Get<MapGameObject>().Position, nearMouseAllyUlti, M1Range, M1Speed, M1Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        else if (nearMouseAllySpace != null)
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(SpaceClone.Get<MapGameObject>().Position, nearMouseAllySpace, M1Range, M1Speed, M1Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        else if (nearMouseAllyEX2 != null)
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(EX2Clone.Get<MapGameObject>().Position, nearMouseAllyEX2, M1Range, M1Speed, M1Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                            else
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                        }
                        break;

                    case AbilitySlot.EXAbility1:
                        if (allyToHealEX1 != null)
                        {
                            var pred = TestPrediction.GetPrediction(myPos, allyToHealEX1, EX1Range, 0f, EX1Radius, EX1AirTime);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
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

            if (HealMenu.GetBoolean("heal.useEX1.CC") && MiscUtils.CanCast(AbilitySlot.EXAbility1))
            {
                if (LastAbilityFired == null && allyToHealEX1 != null && allyToHealEX1.Living.HealthPercent <= HealMenu.GetSlider("heal.useEX1.CC.minHealth"))
                {
                    var pred = TestPrediction.GetPrediction(myPos, allyToHealEX1, EX1Range, 0f, EX1Radius, EX1AirTime);
                    if (pred.CanHit)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.EXAbility1, true);
                    }
                }
            }

            if (HealMenu.GetBoolean("heal.useM1") && MiscUtils.CanCast(AbilitySlot.Ability1))
            {
                if (LastAbilityFired == null)
                {
                    if (nearMouseAllyUlti != null)
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(UltiClone.Get<MapGameObject>().Position, nearMouseAllyUlti, M1Range, M1Speed, M1Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                    else if (nearMouseAllyZander != null)
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(myPos, nearMouseAllyZander, M1Range, M1Speed, M1Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                    else if (nearMouseAllySpace != null)
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(SpaceClone.Get<MapGameObject>().Position, nearMouseAllySpace, M1Range, M1Speed, M1Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                    else if (nearMouseAllyEX2 != null)
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(EX2Clone.Get<MapGameObject>().Position, nearMouseAllyEX2, M1Range, M1Speed, M1Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                }
            }
        }

        private static void HealSelf()
        {
            InGameObject cloneToUse = null;

            if (UltiClone != null && ZanderHero.Distance(UltiClone.Get<MapGameObject>().Position) <= M1Range)
            {
                cloneToUse = UltiClone;
            }
            else if (EX2Clone != null && ZanderHero.Distance(EX2Clone.Get<MapGameObject>().Position) <= M1Range)
            {
                cloneToUse = EX2Clone;
            }
            else if (SpaceClone != null && ZanderHero.Distance(SpaceClone.Get<MapGameObject>().Position) <= M1Range)
            {
                cloneToUse = SpaceClone;
            }

            if (cloneToUse != null)
            {
                LocalPlayer.EditAimPosition = true;
                var pred = TestPrediction.GetNormalLinePrediction(cloneToUse.Get<MapGameObject>().Position, ZanderHero, M1Range, M1Speed, M1Radius, true);
                if (pred.CanHit && MiscUtils.CanCast(AbilitySlot.Ability1))
                {
                    LocalPlayer.Aim(pred.CastPosition);
                    LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                }
            }
        }

        private static void ComboMode()
        {
            var targetModeKey = KeysMenu.GetKeybind("keys.changeTargeting");
            var targetMode = targetModeKey ? TargetingMode.LowestHealth : TargetingMode.NearMouse;

            var M1TargetZander = TargetSelector.GetTarget(targetMode, M1Range);
            var M1TargetSpace = SpaceClone == null ? null : TargetSelector.GetTarget(targetMode, M1Range, SpaceClone.Get<MapGameObject>().Position);
            var M1TargetEX2 = EX2Clone == null ? null : TargetSelector.GetTarget(targetMode, M1Range, EX2Clone.Get<MapGameObject>().Position);
            var M1TargetUlti = UltiClone == null ? null : TargetSelector.GetTarget(targetMode, M1Range, UltiClone.Get<MapGameObject>().Position);

            var M2TargetZander = TargetSelector.GetTarget(targetMode, M2Range);
            var M2TargetUlti = UltiClone == null ? null : TargetSelector.GetTarget(targetMode, M2Range, UltiClone.Get<MapGameObject>().Position);

            var ETarget = EntitiesManager.EnemyTeam
                .Where(x => x.IsValid && !x.Living.IsDead && (x.AbilitySystem.IsCasting || x.IsChanneling) && !x.IsCountering && x.Distance(ZanderHero) < ERange)
                .OrderBy(x => x.Distance(ZanderHero))
                .FirstOrDefault();

            var isCastingOrChanneling = ZanderHero.AbilitySystem.IsCasting || ZanderHero.IsChanneling;

            if (isCastingOrChanneling && LastAbilityFired == null)
            {
                LastAbilityFired = CastingIndexToSlot(ZanderHero.AbilitySystem.CastingAbilityIndex);
            }

            var myPos = ZanderHero.MapObject.Position;

            if (isCastingOrChanneling)
            {
                LocalPlayer.EditAimPosition = true;
                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability4:
                        if (!IsQRecast)
                        {
                            LocalPlayer.Aim(UltiClone.Get<MapGameObject>().Position);
                        }
                        else
                        {
                            if (M2TargetZander != null)
                            {
                                LocalPlayer.Aim(M2TargetZander.MapObject.Position);
                            }
                            else
                            {
                                LocalPlayer.Aim(ZanderHero.MapObject.Position);
                            }
                        }
                        break;
                    case AbilitySlot.Ability5:
                        if (ETarget != null)
                        {
                            var pred = TestPrediction.GetPrediction(myPos, ETarget, ERange, 0f, ERadius, EAirTime);
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

                    case AbilitySlot.Ability7:
                        if (M2TargetZander != null)
                        {
                            LocalPlayer.Aim(M2TargetZander.MapObject.Position);
                        }
                        break;

                    case AbilitySlot.EXAbility2:
                        if (M1TargetZander != null)
                        {
                            LocalPlayer.Aim(M1TargetZander.MapObject.Position);
                        }
                        break;

                    case AbilitySlot.Ability2:
                        if (M2TargetZander != null && !M2TargetZander.IsCountering && !M2TargetZander.HasShield())
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(myPos, M2TargetZander, M2Range, M2Speed, M2Radius, true);
                            if (pred.CollisionResult.IsColliding && UltiClone == null)
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Interrupt, true);
                            }
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                        }
                        else if (M2TargetUlti != null)
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(UltiClone.Get<MapGameObject>().Position, M2TargetUlti, M2Range, M2Speed, M2Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                        }
                        break;

                    case AbilitySlot.Ability1:
                        if (M1TargetUlti != null && !M1TargetUlti.IsCountering && !M1TargetUlti.HasShield())
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(UltiClone.Get<MapGameObject>().Position, M1TargetUlti, M1Range, M1Speed, M1Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                        }
                        else if (M1TargetZander != null && !M1TargetZander.IsCountering && !M1TargetZander.HasShield())
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(myPos, M1TargetZander, M1Range, M1Speed, M1Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                        }
                        else if (M1TargetEX2 != null && !M1TargetEX2.IsCountering && !M1TargetEX2.HasShield())
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(EX2Clone.Get<MapGameObject>().Position, M1TargetEX2, M1Range, M1Speed, M1Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                        }
                        else if (M1TargetSpace != null && !M1TargetSpace.IsCountering && !M1TargetSpace.HasShield())
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(SpaceClone.Get<MapGameObject>().Position, M1TargetSpace, M1Range, M1Speed, M1Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
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

            if (LastAbilityFired == null && ComboMenu.GetBoolean("combo.useQ") && MiscUtils.CanCast(AbilitySlot.Ability4))
            {
                if (LocalPlayer.GetAbilityHudData(AbilitySlot.Ability4).Name.Equals("PortalRecastAbility"))
                {
                    Buff portalBuff;

                    if (MiscUtils.CanCast(AbilitySlot.Ability2))
                    {
                        IsQRecast = true;
                        LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
                        LocalPlayer.EditAimPosition = true;
                        if (M2TargetZander != null)
                        {
                            LocalPlayer.Aim(M2TargetZander.MapObject.Position);
                        }
                        else
                        {
                            LocalPlayer.Aim(ZanderHero.MapObject.Position);
                        }
                    }
                    else if (ZanderHero.HasBuff("PortalRecastBuff", out portalBuff))
                    {
                        if (portalBuff.TimeToExpire <= 1.0f)
                        {
                            IsQRecast = true;
                            LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
                            LocalPlayer.EditAimPosition = true;
                            if (M2TargetZander != null)
                            {
                                LocalPlayer.Aim(M2TargetZander.MapObject.Position);
                            }
                            else
                            {
                                LocalPlayer.Aim(ZanderHero.MapObject.Position);
                            }
                        }
                    }
                }
                else
                {
                    if (UltiClone != null && !MiscUtils.CanCast(AbilitySlot.Ability2))
                    {
                        IsQRecast = false;
                        LocalPlayer.PressAbility(AbilitySlot.Ability4, true);
                        LocalPlayer.EditAimPosition = true;
                        LocalPlayer.Aim(UltiClone.Get<MapGameObject>().Position);
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useE") && MiscUtils.CanCast(AbilitySlot.Ability5))
            {
                if (LastAbilityFired == null && ETarget != null)
                {
                    var pred = TestPrediction.GetPrediction(myPos, ETarget, ERange, 0f, ERadius, EAirTime);
                    if (pred.CanHit)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.Ability5, true);
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useM2") && MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                if (LastAbilityFired == null)
                {
                    if (M2TargetZander != null && !M2TargetZander.IsCountering && !M2TargetZander.HasShield())
                    {
                        if (ComboMenu.GetBoolean("combo.useF") && MiscUtils.CanCast(AbilitySlot.Ability7))
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability7, true);
                        }
                        else
                        {
                            var pred = TestPrediction.GetNormalLinePrediction(myPos, M2TargetZander, M2Range, M2Speed, M2Radius, true);
                            if (pred.CanHit)
                            {
                                LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                            }
                        }
                    }
                    else if (M2TargetUlti != null && !M2TargetUlti.IsCountering && !M2TargetUlti.HasShield())
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(UltiClone.Get<MapGameObject>().Position, M2TargetUlti, M2Range, M2Speed, M2Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability2, true);
                        }
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useEX2") && MiscUtils.CanCast(AbilitySlot.EXAbility2))
            {
                var energyRequired = ComboMenu.GetIntSlider("combo.useEX2.minEnergyBars") * 25;
                if (energyRequired <= ZanderHero.Energized.Energy)
                {
                    if (LastAbilityFired == null && M1TargetZander != null)
                    {
                        LocalPlayer.PressAbility(AbilitySlot.EXAbility2, true);
                    }
                }
            }

            if (ComboMenu.GetBoolean("combo.useM1") && MiscUtils.CanCast(AbilitySlot.Ability1))
            {
                if (LastAbilityFired == null)
                {
                    if (M1TargetUlti != null && !M1TargetUlti.IsCountering && !M1TargetUlti.HasShield())
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(UltiClone.Get<MapGameObject>().Position, M1TargetUlti, M1Range, M1Speed, M1Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                    else if (M1TargetZander != null && !M1TargetZander.IsCountering && !M1TargetZander.HasShield())
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(myPos, M1TargetZander, M1Range, M1Speed, M1Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                    else if (M1TargetEX2 != null && !M1TargetEX2.IsCountering && !M1TargetEX2.HasShield())
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(EX2Clone.Get<MapGameObject>().Position, M1TargetEX2, M1Range, M1Speed, M1Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                    else if (M1TargetSpace != null && !M1TargetSpace.IsCountering && !M1TargetSpace.HasShield())
                    {
                        var pred = TestPrediction.GetNormalLinePrediction(SpaceClone.Get<MapGameObject>().Position, M1TargetSpace, M1Range, M1Speed, M1Radius, true);
                        if (pred.CanHit)
                        {
                            LocalPlayer.PressAbility(AbilitySlot.Ability1, true);
                        }
                    }
                }
            }
        }

        private static void CheckClones()
        {
            SpaceClone = EntitiesManager.GetObjectsByName(SpaceCloneName).FirstOrDefault(x => x.Get<BaseGameObject>().TeamId == ZanderHero.BaseObject.TeamId);
            EX2Clone = EntitiesManager.GetObjectsByName(EX2CloneName).FirstOrDefault(x => x.Get<BaseGameObject>().TeamId == ZanderHero.BaseObject.TeamId);
            UltiClone = EntitiesManager.GetObjectsByName(UltiCloneName).FirstOrDefault(x => x.Get<BaseGameObject>().TeamId == ZanderHero.BaseObject.TeamId);
        }

        private void OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (ZanderHero.CharName != "Zander")
            {
                return;
            }

            Drawing.DrawString(new Vector2(1920f / 2f, 1080f / 2f - 5f),
                "Targeting mode: " + (KeysMenu.GetKeybind("keys.changeTargeting") ? "LowestHealth" : "NearMouse"), UnityEngine.Color.yellow, ViewSpace.ScreenSpacePixels);
        }

        private static void InitMenu()
        {
            ZanderMenu = new Menu("pipzandermenu", "DaPip's Zander");

            KeysMenu = new Menu("keysmenu", "Keys", true);
            KeysMenu.Add(new MenuKeybind("keys.combo", "Combo key", UnityEngine.KeyCode.LeftControl));
            KeysMenu.Add(new MenuKeybind("keys.healOthers", "Heal others", UnityEngine.KeyCode.Mouse3));
            KeysMenu.Add(new MenuKeybind("keys.healSelf", "Heal self", UnityEngine.KeyCode.G));
            KeysMenu.Add(new MenuKeybind("keys.changeTargeting", "Change targeting mode", UnityEngine.KeyCode.T, false, true));
            ZanderMenu.Add(KeysMenu);

            ComboMenu = new Menu("combomenu", "Combo", true);
            ComboMenu.Add(new MenuCheckBox("combo.useM1", "Use Left Mouse (Trick Shot)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useM2", "Use Right Mouse (Grand Conjuration)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useQ", "Use Q to extend ulti clone (Portal)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useE", "Use E to interrupt (Sheep Trick)", true));
            ComboMenu.Add(new MenuCheckBox("combo.useEX2", "Use EX2 to poke (Mind Game)", false));
            ComboMenu.Add(new MenuIntSlider("combo.useEX2.minEnergyBars", "    ^ Min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("combo.useF", "Use F (The Prestige) with M2", true));
            ZanderMenu.Add(ComboMenu);

            HealMenu = new Menu("healmenu", "Healing", true);
            HealMenu.Add(new MenuCheckBox("heal.useM1", "Use Left Mouse (Trick Shot)", true));
            HealMenu.Add(new MenuCheckBox("heal.useEX1.CC", "Use EX1 to cleanse hard CC (Spotlight)", false));
            HealMenu.Add(new MenuSlider("heal.useEX1.CC.minHealth", "    ^ when ally is under % HP", 65f, 100f, 0f));
            ZanderMenu.Add(HealMenu);

            DrawMenu = new Menu("drawmenu", "Drawings", true);
            ZanderMenu.Add(DrawMenu);

            MainMenu.AddMenu(ZanderMenu);
        }

        public void OnUnload()
        {

        }

        private static AbilitySlot? CastingIndexToSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return AbilitySlot.Ability1;
                case 1:
                    return AbilitySlot.Ability2;
                case 2:
                    return AbilitySlot.Ability3;
                case 3:
                    return AbilitySlot.EXAbility2;
                case 4:
                    return AbilitySlot.EXAbility1;
                case 5:
                    return AbilitySlot.Ability5;
                case 6:
                    return AbilitySlot.Ability7;
                case 7:
                case 8:
                    return AbilitySlot.Ability4;
                case 10:
                case 11:
                    return AbilitySlot.Ability6;
                case 14:
                    return AbilitySlot.Mount;
            }

            return null;
        }
    }
}
