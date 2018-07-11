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

using TestPrediction2NS;

namespace TestMain
{
    public class TestMain : IAddon
    {
        private static ArenaDummy ArenaMovingDummy;
        private const float ProjSpeedPolomaM1 = 15.5f;
        private const float ProjSpeedDestinyR = 4f;

        private const float AirTimeLucieQ = 0.55f;

        private static float ProjSpeed = 0f;
        private static float AirTimeProj = 0f;

        public void OnInit()
        {
            Game.OnMatchStart += OnMatchStart;
            Game.OnUpdate += OnUpdate;
            Game.OnDraw += OnDraw;
        }

        public void OnMatchStart(EventArgs args)
        {
            ProjSpeed = 0f;
            AirTimeProj = 0f;

            switch (EntitiesManager.LocalPlayer.CharName)
            {
                case "Poloma":
                    ProjSpeed = ProjSpeedPolomaM1;
                    break;
                case "Destiny":
                    ProjSpeed = ProjSpeedDestinyR;
                    break;
                case "Lucie":
                    AirTimeProj = AirTimeLucieQ;
                    break;
            }
        }

        public void OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (ProjSpeed < float.Epsilon && AirTimeProj < float.Epsilon)
            {
                return;
            }

            LocalPlayer.EditAimPosition = false;

            var movingDummy = EntitiesManager.GetObjectByName("ArenaWalkingDummy");
            if (movingDummy != null)
            {
                ArenaMovingDummy = movingDummy as ArenaDummy;
            }
            else
            {
                return;
            }

            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl))
            {
                if (EntitiesManager.LocalPlayer.AbilitySystem.IsCasting)
                {
                    LocalPlayer.EditAimPosition = true;
                    if (ArenaMovingDummy != null)
                    {
                        if (ProjSpeed > float.Epsilon)
                        {
                            var predProj = TestPrediction.GetPrediction(EntitiesManager.LocalPlayer.MapObject.Position, ArenaMovingDummy, float.MaxValue, ProjSpeed);

                            if (predProj.CanHit)
                            {
                                LocalPlayer.Aim(predProj.CastPosition);
                            }
                        }
                        else if (AirTimeProj > float.Epsilon)
                        {
                            var predAir = TestPrediction.GetPrediction(EntitiesManager.LocalPlayer.MapObject.Position, ArenaMovingDummy, float.MaxValue, 0f, 0f, AirTimeProj);

                            if (predAir.CanHit)
                            {
                                LocalPlayer.Aim(predAir.CastPosition);
                            }
                        }
                    }
                }
            }
        }

        public void OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (ProjSpeed < float.Epsilon && AirTimeProj < float.Epsilon)
            {
                return;
            }

            if (ArenaMovingDummy != null)
            {
                if (ProjSpeed > float.Epsilon)
                {
                    var predProj = TestPrediction.GetPrediction(EntitiesManager.LocalPlayer.MapObject.Position, ArenaMovingDummy, float.MaxValue, ProjSpeed, 0, 0, 2f, true);

                    if (predProj.CanHit)
                    {
                        Drawing.DrawCircle(predProj.CastPosition, 1f, UnityEngine.Color.red);
                    }

                    Drawing.DrawString(ArenaMovingDummy.MapObject.Position, predProj.HitchancePercentage.ToString() + " - " + Enum.GetName(typeof(TestHitchance), predProj.Hitchance), UnityEngine.Color.cyan);
                }

                if (AirTimeProj > float.Epsilon)
                {
                    var predAir = TestPrediction.GetPrediction(EntitiesManager.LocalPlayer.MapObject.Position, ArenaMovingDummy, float.MaxValue, 0f, 0f, AirTimeProj, 2f, true);

                    if (predAir.CanHit)
                    {
                        Drawing.DrawCircle(predAir.CastPosition, 1.5f, UnityEngine.Color.blue);
                    }

                    Drawing.DrawString(ArenaMovingDummy.MapObject.Position, predAir.HitchancePercentage.ToString() + " - " + Enum.GetName(typeof(TestHitchance), predAir.Hitchance), UnityEngine.Color.white);
                }
            }
        }

        public void OnUnload()
        {

        }
    }
}
