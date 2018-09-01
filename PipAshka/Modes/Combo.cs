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

using PipLibrary.Extensions;
using PipLibrary.Utils;

using PipAshka.Modes;
using PipAshka.Utils;

using AH = PipAshka.AbilityHandler;
using BM = PipAshka.Utils.BattleriteManager;

namespace PipAshka.Modes
{
    internal static class Combo
    {
        private static Menu ComboMenu => AshkaMenu.ComboMenu;
        private static AbilitySlot? LastAbility
        {
            get
            {
                return MainLogic.LastAbilityFired;
            }
            set
            {
                MainLogic.LastAbilityFired = value;
            }
        }
        private static bool IsCasting => MainLogic.IsCastingOrChanneling;

        private static bool HasEnhancedM1 => Utilities.Hero.Buffs.Any(x =>
            x.ObjectName.Equals("SearingFireBuff") || x.ObjectName.Equals("BlazeNewBuff") ||
            x.ObjectName.Equals("RagingFireBuff"));

        private const TargetingMode TargetMode = TargetingMode.NearMouse;
        private const float JumpSafeRange = 2.2f;

        private static bool IsCondemning = false;
        private static Vector2 CondemnPos = Vector2.Zero;

        private static bool IsChainingCC = false;
        private static Vector2 ChainCCPos = Vector2.Zero;

        internal static void Update()
        {
            var invisibleTargets = ComboMenu.GetBoolean("invisible");
            //TODO Bubble detection
            var enemiesBase = Utilities.EnemiesBase;
            var enemiesProj = enemiesBase.Where(x => !x.HasProjectileBlocker());

            if (!invisibleTargets)
            {
                enemiesBase = enemiesBase.Where(x => !x.CharacterModel.IsModelInvisible);
                enemiesProj = enemiesProj.Where(x => !x.CharacterModel.IsModelInvisible);
            }

            var TargetM1 = TargetSelector.GetTarget(enemiesProj, TargetMode, AH.M1.Range);
            var TargetM2 = TargetSelector.GetTarget(enemiesProj, TargetMode, AH.M2.Range);
            var TargetQNormal = TargetSelector.GetTarget(enemiesBase, TargetMode, AH.Q.Range);
            var TargetENear = TargetSelector.GetTarget(enemiesProj, TargetMode, ComboMenu.GetSlider("useE.near.safeRange"));
            var TargetR = TargetSelector.GetTarget(enemiesBase, TargetMode, AH.M1.Range);
            var TargetF = TargetSelector.GetTarget(enemiesBase, TargetMode, AH.F.Range);

            var hero = Utilities.Hero;
            var myPos = hero.MapObject.Position;

            Drawing.DrawCircle(myPos, 1f, UnityEngine.Color.green);

            if (TargetM1 != null)
            {
                var targetPos = TargetM1.MapObject.Position;
                var rPos = myPos.Extend(targetPos, 1f);
                Drawing.DrawCircle(rPos, 1f, UnityEngine.Color.magenta);
            }

            #region Aiming
            if (IsCasting)
            {
                LocalPlayer.EditAimPosition = true;

                switch (LastAbility)
                {
                    case AbilitySlot.Ability7:
                        if (TargetF != null)
                        {
                            var pred = AH.F.GetPrediction(TargetF, false, myPos);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                            else
                            {
                                LocalPlayer.Aim(InputManager.MousePosition.ScreenToWorld());
                            }
                        }
                        else
                        {
                            LocalPlayer.Aim(InputManager.MousePosition.ScreenToWorld());
                        }

                        break;

                    case AbilitySlot.EXAbility1:
                    case AbilitySlot.Ability3:
                        var bestPos = MathUtils.GetBestJumpPosition(0, 40, AH.Space.Range);
                        LocalPlayer.Aim(bestPos);
                        break;

                    case AbilitySlot.EXAbility2:
                        //Not Aimed
                        break;

                    case AbilitySlot.Ability6:
                        if (TargetM1 != null)
                        {
                            var targetPos = TargetM1.MapObject.Position;
                            var wallPos = myPos.Extend(targetPos, 1f);
                            LocalPlayer.Aim(wallPos);
                        }
                        break;

                    case AbilitySlot.Ability5:
                        if (IsCondemning)
                        {
                            LocalPlayer.Aim(CondemnPos);
                        }
                        else if (TargetENear != null)
                        {
                            var pred = AH.E.GetPrediction(TargetENear, true, myPos, Utilities.TrueERange);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                        }

                        break;

                    case AbilitySlot.Ability4:
                        if (IsChainingCC)
                        {
                            LocalPlayer.Aim(ChainCCPos);
                        }
                        else if (TargetQNormal != null)
                        {
                            var pred = AH.Q.GetPrediction(TargetQNormal, false, myPos);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                            else
                            {
                                AH.Interrupt.Cast();
                            }
                        }
                        else
                        {
                            AH.Interrupt.Cast();
                        }

                        break;

                    case AbilitySlot.Ability2:
                        if (TargetM2 != null)
                        {
                            var pred = AH.M2.GetPrediction(TargetM2, true, myPos);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                            else
                            {
                                AH.Interrupt.Cast();
                            }
                        }
                        else
                        {
                            AH.Interrupt.Cast();
                        }

                        break;

                    case AbilitySlot.Ability1:
                        if (TargetM1 != null)
                        {
                            var pred = AH.M1.GetPrediction(TargetM1, true, myPos);
                            if (pred.CanHit)
                            {
                                LocalPlayer.Aim(pred.CastPosition);
                            }
                            else
                            {
                                AH.Interrupt.Cast();
                            }
                        }
                        else
                        {
                            AH.Interrupt.Cast();
                        }

                        break;
                }
            }
            else
            {
                IsChainingCC = false;
                IsCondemning = false;
                LocalPlayer.EditAimPosition = false;
                LastAbility = null;
            }
            #endregion

            #region Casting
            if (LastAbility == null && !IsCasting && !hero.IsDashing && !hero.IsTraveling)
            {
                var priorityM1 = ComboMenu.GetBoolean("useM1.priority");

                if (ComboMenu.GetBoolean("useF") && AH.F.IsReady && TargetF != null)
                {
                    var pred = AH.F.GetPrediction(TargetF, false, myPos);
                    if (pred.CanHit)
                    {
                        AH.F.Cast();
                        return;
                    }
                }

                if (ComboMenu.GetBoolean("useEX1") && AH.EX1.IsReady)
                {
                    if (hero.EnemiesAroundAlive(JumpSafeRange) > 0)
                    {
                        var minHP = ComboMenu.GetSlider("useEX1.minHP");
                        var energyReq = ComboMenu.GetIntSlider("useEX1.energy") * 25;
                        if (hero.Living.HealthPercent <= minHP
                            && hero.Energized.Energy >= energyReq)
                        {
                            AH.EX1.Cast();
                            return;
                        }
                    }
                }

                if (ComboMenu.GetBoolean("useSpace") && AH.Space.IsReady)
                {
                    if (hero.EnemiesAroundAlive(JumpSafeRange) > 0)
                    {
                        AH.Space.Cast();
                        return;
                    }
                }

                if (AH.EX2.IsReady)
                {
                    var energyReq = ComboMenu.GetIntSlider("useEX2.energy") * 25;
                    if (hero.Energized.Energy >= energyReq)
                    {
                        if (ComboMenu.GetBoolean("useEX2.enemies"))
                        {
                            var count = ComboMenu.GetIntSlider("useEX2.enemies.count");
                            if (hero.EnemiesAroundAlive(AH.EX2.Range) >= count)
                            {
                                AH.EX2.Cast();
                                return;
                            }
                        }

                        if (ComboMenu.GetBoolean("useEX2.panic"))
                        {
                            var minHP = ComboMenu.GetSlider("useEX2.panic.minHP");
                            if (hero.EnemiesAroundAlive(AH.EX2.Range) > 0
                                && hero.Living.HealthPercent <= minHP)
                            {
                                AH.EX2.Cast();
                                return;
                            }
                        }
                    }
                }

                //if (ComboMenu.GetBoolean("useR.harass") && AH.R.IsReady && AH.M1.IsReady && TargetM1 != null
                //    && BM.HasConflagration)
                //{
                //    var energyReq = ComboMenu.GetIntSlider("useR.harass.energy") * 25;
                //    if (hero.Energized.Energy >= energyReq)
                //    {
                //        var pred = AH.M1.GetPrediction(TargetM1, true, myPos);
                //        if (pred.CanHit)
                //        {
                //            AH.R.Cast();
                //            return;
                //        }
                //    }
                //}

                if (AH.E.IsReady)
                {
                    var trueERange = Utilities.TrueERange;

                    if (ComboMenu.GetBoolean("useE.condemn") && BM.HasKnockout)
                    {
                        var enemies = enemiesBase
                            .Where(x => !x.IsCountering)
                            .OrderBy(x => x.MapObject.ScreenPosition.Distance(InputManager.MousePosition));

                        foreach (var enemy in enemies)
                        {
                            if (enemy.Distance(hero) > trueERange)
                            {
                                continue;
                            }

                            var pred = AH.E.GetPrediction(enemy, true, myPos, trueERange);
                            if (pred.CanHit)
                            {
                                var direction = pred.CastPosition - myPos;
                                var distance = pred.CastPosition.Distance(myPos);
                                var extendedPos = (direction.Normalized * (distance + 4.5f)) + myPos;
                                Drawing.DrawCircle(extendedPos, 1f, UnityEngine.Color.red);
                                var col = CollisionSolver.CheckLineCollision(pred.CastPosition, extendedPos);
                                if (col.IsColliding)
                                {
                                    IsCondemning = true;
                                    CondemnPos = pred.CastPosition;
                                    AH.E.Cast();
                                    return;
                                }
                            }
                        }
                    }

                    if (ComboMenu.GetBoolean("useE.near") && TargetENear != null)
                    {
                        var pred = AH.E.GetPrediction(TargetENear, true, myPos, trueERange);
                        if (pred.CanHit)
                        {
                            AH.E.Cast();
                            return;
                        }
                    }
                }

                if (!(priorityM1 && HasEnhancedM1))
                {
                    if (AH.Q.IsReady)
                    {
                        if (ComboMenu.GetBoolean("useQ"))
                        {
                            if (!ComboMenu.GetBoolean("useQ.chainCC") && TargetQNormal != null)
                            {
                                var pred = AH.Q.GetPrediction(TargetQNormal, false, myPos);
                                if (pred.CanHit)
                                {
                                    AH.Q.Cast();
                                    return;
                                }
                            }
                            else if (ComboMenu.GetBoolean("useQ.chainCC"))
                            {
                                var enemies = enemiesBase;
                                foreach (var enemy in enemies)
                                {
                                    if (enemy.Distance(hero) <= AH.Q.Range
                                        && enemy.HasHardCC()
                                        && enemy.CCTotalDuration >= 1.5f)
                                    {
                                        IsChainingCC = true;
                                        ChainCCPos = enemy.MapObject.Position;
                                        AH.Q.Cast();
                                        return;
                                    }
                                }
                            }
                        }
                    }


                    if (ComboMenu.GetBoolean("useM2") && AH.M2.IsReady && TargetM2 != null)
                    {
                        var safeRange = ComboMenu.GetSlider("useM2.safeRange");
                        if (hero.EnemiesAroundAlive(safeRange) == 0)
                        {
                            var pred = AH.M2.GetPrediction(TargetM2, true, myPos);
                            if (pred.CanHit)
                            {
                                AH.M2.Cast();
                                return;
                            }
                        }
                    }
                }

                if (ComboMenu.GetBoolean("useM1") && AH.M1.IsReady && TargetM1 != null)
                {
                    var pred = AH.M1.GetPrediction(TargetM1, true, myPos);
                    if (pred.CanHit)
                    {
                        AH.M1.Cast();
                        return;
                    }
                }
            }
            #endregion
        }
    }
}
