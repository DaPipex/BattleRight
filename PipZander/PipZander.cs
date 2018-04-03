using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.Math;
using BattleRight.Core.Models;

using BattleRight.SDK;
using BattleRight.SDK.Enumeration;
using BattleRight.SDK.Events;
using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Models;
using BattleRight.SDK.UI.Values;

using PipZander.Extensions;
using PipZander.Utils;

namespace PipZander
{
    internal static class PipZander
    {
        private static Menu ZanderMenu = null;
        private static Player ZanderHero = null;

        private const float ZanderSpellRadius = 0.6f;

        private const float M1Range = 6.5f;
        private const float M1Radius = 0f; //0.3f * 2f;
        private const float M1Speed = 15.5f;

        private const float M2Range = 10f;
        private const float M2Radius = 0f; //0.4f;
        private const float M2Speed = 24.5f;

        private const float EX1Delay = 0.55f;

        private const float SpaceEX2Range = 6.5f;

        private const float QRange = 10f;


        private const float ERange = 9.1f;
        private const float EDelay = 0.8f;

        private static readonly string SpaceCloneName = "MirrorImage";
        private static readonly string EX2CloneName = "MindgameIllusion2";
        private static readonly string UltiCloneName = "ThePrestige";

        private static ActiveGameObject SpaceClone = null;
        private static ActiveGameObject EX2Clone = null;
        private static ActiveGameObject UltiClone = null;

        private static AbilitySlot? LastAbilityFired = null;
        private static bool IsQRecast = false;

        public static void Init()
        {
            var _zanderMenu = new Menu("pipzandermenu", "DaPipex's Zander");

            _zanderMenu.AddLabel("Combo");
            _zanderMenu.Add(new MenuCheckBox("combo.useM1", "Use Left Mouse (Trick Shot)", true));
            _zanderMenu.Add(new MenuCheckBox("combo.useM2", "Use Right Mouse (Grand Conjuration)", true));
            _zanderMenu.Add(new MenuCheckBox("combo.useEX2", "Use EX2 to poke (Mind Game)", false));
            _zanderMenu.Add(new MenuCheckBox("combo.useQ", "Use Q to extend ulti clone (Portal)", true));
            _zanderMenu.Add(new MenuCheckBox("combo.useF", "Use F (The Prestige)", true));

            _zanderMenu.AddSeparator(10f);

            _zanderMenu.AddLabel("Healing");
            //_zanderMenu.Add(new MenuCheckBox("heal.useM1", "Use Left Mouse (Trick Shot)", true));
            _zanderMenu.Add(new MenuCheckBox("heal.useEX1", "Use EX1 when enemies are too close(Spotlight)", true));
            //_zanderMenu.Add(new MenuSlider("heal.useEX1.slider", "%HP under X to use EX1 (Spotlight)", 50f, 100f, 0f));

            _zanderMenu.AddSeparator(10f);

            _zanderMenu.AddLabel("Misc");
            _zanderMenu.Add(new MenuCheckBox("misc.useE", "Use E to interrupt channels (Sheep Trick)", true));

            _zanderMenu.AddSeparator(10f);

            _zanderMenu.AddLabel("Drawings");
            _zanderMenu.Add(new MenuCheckBox("draw.clones", "Draw circle around clones", true));
            _zanderMenu.Add(new MenuCheckBox("draw.portalRecastTime", "Portal recast time", false));

            MainMenu.AddMenu(_zanderMenu);

            CustomEvents.Instance.OnMatchStart += OnMatchStart;
            CustomEvents.Instance.OnMatchEnd += OnMatchEnd;
            CustomEvents.Instance.OnUpdate += delegate
            {
                ZanderMenu = _zanderMenu;
                ZanderHero = EntitiesManager.LocalPlayer;

                OnUpdate();
            };
            CustomEvents.Instance.OnDraw += OnDraw;
        }

        private static void OnMatchStart(EventArgs args)
        {

        }

        private static void OnMatchEnd(EventArgs args)
        {

        }

        private static void OnUpdate()
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (ZanderHero.CharName != Champion.Zander.ToString())
            {
                return;
            }

            CheckClones();

            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.Mouse3))
            {
                HealOthers();
            }
            else if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftAlt))
            {
                HealSelf();
            }
            else if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl))
            {
                DamageCombo();
                InterruptCombo();
            }
        }

        private static void CheckClones()
        {
            var activeGOs = EntitiesManager.ActiveGameObjects;

            SpaceClone = activeGOs.Find(x => x.ObjectName.Equals(SpaceCloneName));
            EX2Clone = activeGOs.Find(x => x.ObjectName.Equals(EX2CloneName));
            UltiClone = activeGOs.Find(x => x.ObjectName.Equals(UltiCloneName));
        }

        private static void HealOthers()
        {
            var playerToHeal = EntitiesManager.LocalTeam
                  .Where(x => !x.IsLocalPlayer && !x.IsDead)
                  .OrderBy(x => x.Health)
                  .FirstOrDefault(x => x.MaxRecoveryHealth != x.Health && x.Distance(ZanderHero) <= M1Range);

            if (playerToHeal != null)
            {
                var pred = NewPrediction.Prediction.GetPrediction(
                    new NewPrediction.PredictionInput(
                        ZanderHero.WorldPosition, playerToHeal, M1Speed, M1Range, 0f, M1Radius), true);

                if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                {
                    LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                    LocalPlayer.CastAbility(AbilitySlot.Ability1);
                }

                //if (ZanderMenu.GetBoolean("heal.useEX1") /*&& MiscUtils.CanCast(AbilitySlot.EXAbility1)*/)
                //{
                //    if (MiscUtils.EnemiesAround(playerToHeal, 2f) > 0)
                //    {
                //        LocalPlayer.UpdateCursorPosition(playerToHeal);
                //        LocalPlayer.CastAbility(AbilitySlot.EXAbility1);
                //    }
                //}
            }
        }

        private static void HealSelf()
        {
            ActiveGameObject cloneToUse = null;

            if (SpaceClone != null && ZanderHero.Distance(SpaceClone) <= M1Range)
            {
                cloneToUse = SpaceClone;
            }
            else if (EX2Clone != null && ZanderHero.Distance(EX2Clone) <= M1Range)
            {
                cloneToUse = EX2Clone;
            }
            else if (UltiClone != null && ZanderHero.Distance(UltiClone) <= M1Range)
            {
                cloneToUse = UltiClone;
            }

            if (cloneToUse != null)
            {
                var midpoint = MathUtils.MidPoint(cloneToUse.WorldPosition, ZanderHero.WorldPosition).WorldToScreen();

                LocalPlayer.UpdateCursorPosition(midpoint);
                LocalPlayer.CastAbility(AbilitySlot.Ability1);

                //var pred = NewPrediction.Prediction.GetPrediction(
                //    new NewPrediction.PredictionInput(
                //        cloneToUse.WorldPosition, ZanderHero, M1Speed, M1Range, 0f, M1Radius), true);

                //if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                //{
                //    LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                //    LocalPlayer.CastAbility(AbilitySlot.Ability1);
                //}
            }
            else
            {
                if (MiscUtils.CanCast(AbilitySlot.Ability3))
                {
                    LocalPlayer.CastAbility(AbilitySlot.Ability3);
                }
            }
        }

        private static void DamageCombo()
        {
            bool useEX2 = ZanderMenu.GetBoolean("combo.useEX2");
            bool IsCastingOrChanneling = ZanderHero.IsCasting || ZanderHero.IsChanneling;

            float extraZanderRange = useEX2 && MiscUtils.CanCast(AbilitySlot.EXAbility2) ? SpaceEX2Range : 0f;
            Player TargetM1Zander = MyTargetSelector.GetTarget(TargetingMode.LowestHealth, M1Range + extraZanderRange);
            Player TargetM2Zander = MyTargetSelector.GetTarget(TargetingMode.LowestHealth, M2Range);

            Player TargetM1Space = SpaceClone == null ? null : MyTargetSelector.GetTarget(TargetingMode.LowestHealth, M1Range, SpaceClone);

            Player TargetM1EX2 = EX2Clone == null ? null : MyTargetSelector.GetTarget(TargetingMode.LowestHealth, M1Range, EX2Clone);

            Player TargetM1Ulti = UltiClone == null ? null : MyTargetSelector.GetTarget(TargetingMode.LowestHealth, M1Range, UltiClone);
            Player TargetM2Ulti = UltiClone == null ? null : MyTargetSelector.GetTarget(TargetingMode.LowestHealth, M2Range, UltiClone);

            if (IsCastingOrChanneling)
            {
                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability4:
                        if (!IsQRecast)
                        {
                            LocalPlayer.UpdateCursorPosition(UltiClone.ScreenPosition);
                        }
                        else
                        {
                            if (TargetM2Zander != null)
                            {
                                var midpoint = MathUtils.MidPoint(ZanderHero.WorldPosition, TargetM2Zander.WorldPosition).WorldToScreen();
                                LocalPlayer.UpdateCursorPosition(midpoint);
                            }
                            else
                            {
                                LocalPlayer.UpdateCursorPosition(ZanderHero);
                            }
                        }
                        break;

                    case AbilitySlot.UltimateAbility:
                        if (TargetM2Zander != null)
                        {
                            var midpoint = MathUtils.MidPoint(ZanderHero.WorldPosition, TargetM2Zander.WorldPosition).WorldToScreen();
                            LocalPlayer.UpdateCursorPosition(midpoint);
                        }
                        break;

                    case AbilitySlot.Ability2:
                        if (TargetM2Zander != null)
                        {
                            var pred = NewPrediction.Prediction.GetPrediction(
                                new NewPrediction.PredictionInput(
                                    ZanderHero.WorldPosition, TargetM2Zander, M2Speed, M2Range, 0f, M2Radius, NewPrediction.Enumerations.SkillType.Line,
                                    CollisionFlags.InvisWalls | CollisionFlags.Bush), true);

                            if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                            {
                                LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                            }
                        }
                        break;

                    case AbilitySlot.EXAbility2:
                        if (TargetM1Zander != null)
                        {
                            LocalPlayer.UpdateCursorPosition(TargetM1Zander.ScreenPosition);
                        }
                        break;

                    case AbilitySlot.Ability1:
                        if (TargetM1Zander != null)
                        {
                            if (TargetM1Zander.Distance(ZanderHero) <= M1Range)
                            {
                                var pred = NewPrediction.Prediction.GetPrediction(
                                    new NewPrediction.PredictionInput(
                                        ZanderHero.WorldPosition, TargetM1Zander, M1Speed, M1Range, 0f, M1Radius), true);

                                if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                                {
                                    LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                                }
                            }
                        }
                        else if (TargetM1Space != null && SpaceClone != null && TargetM1Space.Distance(SpaceClone) <= M1Range)
                        {
                            var pred = NewPrediction.Prediction.GetPrediction(
                                new NewPrediction.PredictionInput(
                                    SpaceClone.WorldPosition, TargetM1Space, M1Speed, M1Range, 0f, M1Radius / 2), true);

                            if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                            {
                                LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                            }
                        }
                        else if (TargetM1EX2 != null && EX2Clone != null && TargetM1EX2.Distance(EX2Clone) <= M1Range)
                        {
                            var pred = NewPrediction.Prediction.GetPrediction(
                                new NewPrediction.PredictionInput(
                                    EX2Clone.WorldPosition, TargetM1EX2, M1Speed, M1Range, 0f, M1Radius / 2), true);

                            if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                            {
                                LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                            }
                        }
                        else if (TargetM1Ulti != null && UltiClone != null && TargetM1Ulti.Distance(UltiClone) <= M1Range)
                        {
                            var pred = NewPrediction.Prediction.GetPrediction(
                                new NewPrediction.PredictionInput(
                                    UltiClone.WorldPosition, TargetM1Ulti, M1Speed, M1Range, 0f, M1Radius / 2), true);

                            if (pred.Hitchance >= NewPrediction.Enumerations.Hitchance.Medium)
                            {
                                LocalPlayer.UpdateCursorPosition(pred.MousePosition);
                            }
                        }
                        break;
                }
            }
            else
            {
                LastAbilityFired = null;
            }

            if (LastAbilityFired == null && LocalPlayer.GetAbilityHudData(AbilitySlot.Ability4).Name == "PortalRecastAbility" && MiscUtils.CanCast(AbilitySlot.Ability4))
            {
                Buff portalBuff;

                if (MiscUtils.CanCast(AbilitySlot.Ability2))
                {
                    LocalPlayer.CastAbility(AbilitySlot.Ability4);
                    LastAbilityFired = AbilitySlot.Ability4;
                    IsQRecast = true;
                }
                else if (ZanderHero.HasBuff("PortalRecastBuff", out portalBuff))
                {
                    if (portalBuff.TimeToExpire <= 0.8f)
                    {
                        LocalPlayer.CastAbility(AbilitySlot.Ability4);
                        LastAbilityFired = AbilitySlot.Ability4;
                        IsQRecast = true;
                    }
                }
            }
            else if (LastAbilityFired == null && ZanderMenu.GetBoolean("combo.useQ") && MiscUtils.CanCast(AbilitySlot.Ability4))
            {
                if (UltiClone != null && !MiscUtils.CanCast(AbilitySlot.Ability2))
                {
                    LocalPlayer.CastAbility(AbilitySlot.Ability4);
                    LastAbilityFired = AbilitySlot.Ability4;
                    IsQRecast = false;
                }
            }

            if (LastAbilityFired == null && TargetM2Zander != null && ZanderMenu.GetBoolean("combo.useM2") && MiscUtils.CanCast(AbilitySlot.Ability2))
            {
                if (ZanderMenu.GetBoolean("combo.useF") && MiscUtils.CanCast(AbilitySlot.UltimateAbility))
                {
                    LocalPlayer.CastAbility(AbilitySlot.UltimateAbility);
                    LastAbilityFired = AbilitySlot.UltimateAbility;
                }
                else
                {
                    LocalPlayer.CastAbility(AbilitySlot.Ability2);
                    LastAbilityFired = AbilitySlot.Ability2;
                }
            }

            if (LastAbilityFired == null 
                && (TargetM1Zander != null || TargetM1Space != null || TargetM1EX2 != null || TargetM1Ulti != null) 
                && ZanderMenu.GetBoolean("combo.useM1") && MiscUtils.CanCast(AbilitySlot.Ability1))
            {
                if (useEX2 && MiscUtils.CanCast(AbilitySlot.EXAbility2))
                {
                    if (EX2Clone == null)
                    {
                        if (ZanderHero.Distance(TargetM1Zander) > M1Range)
                        {
                            LocalPlayer.CastAbility(AbilitySlot.EXAbility2);
                            LastAbilityFired = AbilitySlot.EXAbility2;
                        }
                    }
                }
                else
                {
                    LocalPlayer.CastAbility(AbilitySlot.Ability1);
                    LastAbilityFired = AbilitySlot.Ability1;
                }
            }
        }

        private static void InterruptCombo()
        {
            Player InterruptTargetE = null;
            bool IsCastingOrChanneling = ZanderHero.IsCasting || ZanderHero.IsChanneling;

            if (EntitiesManager.EnemyTeam.Any())
            {
                foreach (var enemy in EntitiesManager.EnemyTeam)
                {
                    if (InterruptTargetE == null)
                    {
                        if (ZanderMenu.GetBoolean("misc.useE") && MiscUtils.CanCast(AbilitySlot.Ability5) && ZanderHero.Distance(enemy) <= ERange)
                        {
                            if (!enemy.IsDead && !enemy.IsCountering && !enemy.IsImmaterial && (enemy.IsCasting || enemy.IsChanneling))
                            {
                                InterruptTargetE = enemy;
                                LocalPlayer.CastAbility(AbilitySlot.Ability5);
                                LastAbilityFired = AbilitySlot.Ability5;
                                break;
                            }
                        }
                    }
                }
            }

            if (IsCastingOrChanneling)
            {
                switch (LastAbilityFired)
                {
                    case AbilitySlot.Ability5:
                        if (InterruptTargetE != null)
                        {
                            //TODO: Maybe Prediction?
                            LocalPlayer.UpdateCursorPosition(InterruptTargetE.ScreenPosition);
                        }
                        break;
                }
            }
            else
            {
                LastAbilityFired = null;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (ZanderHero.CharName != Champion.Zander.ToString())
            {
                return;
            }

            if (ZanderMenu.GetBoolean("draw.clones"))
            {
                if (SpaceClone != null)
                {
                    Drawing.DrawCircle(SpaceClone.WorldPosition, 1f, UnityEngine.Color.cyan);
                }

                if (EX2Clone != null)
                {
                    Drawing.DrawCircle(EX2Clone.WorldPosition, 1f, UnityEngine.Color.blue);
                }

                if (UltiClone != null)
                {
                    Drawing.DrawCircle(UltiClone.WorldPosition, 1f, UnityEngine.Color.magenta);
                }
            }

            if (ZanderMenu.GetBoolean("draw.portalRecastTime"))
            {
                Buff portalBuff;
                if (ZanderHero.HasBuff("PortalRecastBuff", out portalBuff))
                {
                    Drawing.DrawString(ZanderHero.WorldPosition, portalBuff.TimeToExpire.ToString(), UnityEngine.Color.green);
                }
            }
        }
    }
}
