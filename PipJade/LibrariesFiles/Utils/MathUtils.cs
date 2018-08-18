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

namespace PipLibrary.Utils
{
    public static class MathUtils
    {
        private static Character MyHero => LocalPlayer.Instance;

        public static Vector2 MidPoint(Vector2 pA, Vector2 pB)
        {
            return new Vector2((pA.X + pB.X) / 2, (pA.Y + pB.Y) / 2);
        }

        public static Vector2 GetBestJumpPosition(int towards, int pointsToConsider, float jumpRange)
        {
            var allies = EntitiesManager.LocalTeam.Where(x => !x.IsLocalPlayer && !x.Living.IsDead);

            var alliesInRange = allies
                .Where(x => x.Distance(MyHero) <= jumpRange)
                .OrderByDescending(x => x.Distance(MyHero));

            var alliesNotInRange = allies
                .Except(alliesInRange)
                .OrderBy(x => x.Distance(MyHero));


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
                        if (Math.Abs(MyHero.Distance(ally) - jumpRange) <= 4.5f)
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
                            X = (int)(MyHero.MapObject.Position.X + jumpRange * Math.Cos(angleIteration)),
                            Y = (int)(MyHero.MapObject.Position.Y + jumpRange * Math.Sin(angleIteration))
                        };

                        PossibleSafeSpots.Add(point);
                    }

                    //No ally is safe, let's just find a safe spot in the circumference that is closest to the ally who in turn is closest to our jump distance

                    var orderedByEdgeDistance = allies.OrderBy(x => Math.Abs(MyHero.Distance(x) - jumpRange));

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
    }
}
