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

namespace TestPredictionNS
{
    public enum TestSkillshotType
    {
        Line,
        Circle
    }

    public enum TestHitChance
    {
        OutOfRange,
        Impossible,
        Low,
        Medium,
        High,
        VeryHigh,
        Immobile
    }

    public class TestInput
    {
        public float Radius = 1f;
        public float FixedDelay;
        public float Speed = float.MaxValue;
        public float Range = float.MaxValue;
        public TestSkillshotType SkillshotType = TestSkillshotType.Line;
        public Vector2 FromPos;
        public Character TargetUnit;
        public bool UseSpellCollisionRadius = true;

        internal float RealRadius
        {
            get
            {
                return UseSpellCollisionRadius ? Radius + TargetUnit.SpellCollision.SpellCollisionRadius : Radius;
            }
        }
    }

    public class TestOutput
    {
        public TestHitChance Hitchance = TestHitChance.Impossible;
        public Vector2 CastPosition;
        public Vector2 UnitPosition;

        internal TestInput Input;
    }

    public static class TestPrediction
    {
        public static TestOutput GetPrediction(
            Vector2 fromPos, 
            Character target, 
            float range, 
            float speed, 
            float radius, 
            float fixedDelay = 0f, 
            TestSkillshotType skillType = TestSkillshotType.Line, 
            bool includePing = true,
            bool checkCollision = false,
            float extendScalar = 4f)
        {
            return GetPrediction(
                new TestInput()
                {
                    FromPos = fromPos,
                    TargetUnit = target,
                    Range = range,
                    Speed = speed,
                    Radius = radius,
                    FixedDelay = fixedDelay,
                    SkillshotType = skillType,
                }, includePing, checkCollision, extendScalar);
        }

        internal static TestOutput GetPrediction(TestInput input, bool includePing, bool checkCollision, float extendScalar = 4f)
        {
            if (!input.TargetUnit.IsValid)
            {
                return new TestOutput();
            }

            if (includePing)
            {
                //TODO: includeping
            }

            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon && input.TargetUnit.Distance(input.FromPos) > input.Range * 1.5f)
            {
                return new TestOutput()
                {
                    Hitchance = TestHitChance.OutOfRange,
                    Input = input,
                };
            }

            if (false) //TODO: dashing
            {

            }
            else
            {
                if (false) //TODO: check immobile
                {

                }
            }

            TestOutput result = GetStandardPrediction(input, extendScalar);

            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon)
            {
                if (result.Hitchance >= TestHitChance.High && input.FromPos.Distance(input.TargetUnit.MapObject.Position) > input.Range + input.RealRadius * 3f / 4f)
                {
                    result.Hitchance = TestHitChance.Medium;
                }

                if (input.FromPos.Distance(result.UnitPosition) > input.Range + (input.SkillshotType == TestSkillshotType.Circle ? input.RealRadius : 0f))
                {
                    result.Hitchance = TestHitChance.OutOfRange;
                }

                if (input.FromPos.Distance(result.CastPosition) > input.Range)
                {
                    if (result.Hitchance != TestHitChance.OutOfRange)
                    {
                        result.CastPosition = input.FromPos + input.Range * (result.UnitPosition - input.FromPos).Normalized;
                    }
                }
            }

            return result;
        }

        internal static TestOutput GetPositionOnPath(TestInput input, Vector2 direction, float speed = -1f, float extendScalar = 4f)
        {
            speed = Math.Abs(speed - (-1)) < float.Epsilon ? input.TargetUnit.Destructible.MovementSpeed : speed;

            if (speed == 0)
            {
                return new TestOutput()
                {
                    Input = input,
                    CastPosition = input.TargetUnit.MapObject.Position,
                    Hitchance = TestHitChance.VeryHigh,
                };
            }

            var targetPos = input.TargetUnit.MapObject.Position;
            var extendedPos = targetPos + direction.Normalized * extendScalar; //TODO: Check other values for this constant
            var lineVector = extendedPos - targetPos;
            var pLength = extendedPos.Distance(targetPos);

            if (pLength >= speed - input.RealRadius && Math.Abs(input.Speed - float.MaxValue) > float.Epsilon)
            {
                var d = speed - input.RealRadius;

                if (input.SkillshotType == TestSkillshotType.Line)
                {
                    if (input.FromPos.Distance(input.TargetUnit.MapObject.Position) < 10f)
                    {
                        d = speed;
                    }
                }

                var cutPath = targetPos + lineVector.Normalized * d;
                var a = cutPath;
                var b = extendedPos;
                //var tT = 0f;
                var tB = pLength / speed;
                var dir = lineVector.Normalized;
                a = a - speed * tB * dir;
                var solution = TestGeometry.VectorMovementCollision(a, b, speed, input.FromPos, input.Speed, tB);
                var t = (float)solution[0];
                var pos = (Vector2)solution[1];

                if (pos != new Vector2() && t >= tB && t <= tB + tB)
                {
                    var unitPos = pos + input.RealRadius * dir;

                    return new TestOutput()
                    {
                        Input = input,
                        CastPosition = pos,
                        UnitPosition = unitPos,
                        Hitchance = pos.Distance(input.FromPos) < 2f ? TestHitChance.VeryHigh : TestHitChance.High,
                    };
                }
            }

            return new TestOutput()
            {
                Input = input,
                CastPosition = extendedPos,
                Hitchance = TestHitChance.Medium,
            };
        }

        internal static TestOutput GetStandardPrediction(TestInput input, float extendScalar)
        {
            var unitSpeed = input.TargetUnit.Destructible.MovementSpeed;

            if (input.TargetUnit.Distance(input.FromPos) < 10f)
            {
                unitSpeed /= 1.5f;
            }

            return GetPositionOnPath(input, input.TargetUnit.NetworkMovement.Velocity, unitSpeed, extendScalar);
        }
    }

    public static class TestGeometry
    {
        public static Object[] VectorMovementCollision(
            Vector2 startPoint1,
            Vector2 endPoint1,
            float v1,
            Vector2 startPoint2,
            float v2,
            float delay = 0f)
        {
            float sP1x = startPoint1.X,
                  sP1y = startPoint1.Y,
                  eP1x = endPoint1.X,
                  eP1y = endPoint1.Y,
                  sP2x = startPoint2.X,
                  sP2y = startPoint2.Y;

            float d = eP1x - sP1x, e = eP1y - sP1y;
            float dist = (float)Math.Sqrt(d * d + e * e), t1 = float.NaN;
            float S = Math.Abs(dist) > float.Epsilon ? v1 * d / dist : 0,
                  K = (Math.Abs(dist) > float.Epsilon) ? v1 * e / dist : 0f;

            float r = sP2x - sP1x, j = sP2y - sP1y;
            var c = r * r + j * j;

            if (dist > 0f)
            {
                if (Math.Abs(v1 - float.MaxValue) < float.Epsilon)
                {
                    var t = dist / v1;
                    t1 = v2 * t >= 0f ? t : float.NaN;
                }
                else if (Math.Abs(v2 - float.MaxValue) < float.Epsilon)
                {
                    t1 = 0f;
                }
                else
                {
                    float a = S * S + K * K - v2 * v2, b = -r * S - j * K;

                    if (Math.Abs(a) < float.Epsilon)
                    {
                        if (Math.Abs(b) < float.Epsilon)
                        {
                            t1 = (Math.Abs(c) < float.Epsilon) ? 0f : float.NaN;
                        }
                        else
                        {
                            var t = -c / (2 * b);
                            t1 = (v2 * t >= 0f) ? t : float.NaN;
                        }
                    }
                    else
                    {
                        var sqr = b * b - a * c;
                        if (sqr >= 0)
                        {
                            var nom = (float)Math.Sqrt(sqr);
                            var t = (-nom - b) / a;
                            t1 = v2 * t >= 0f ? t : float.NaN;
                            t = (nom - b) / a;
                            var t2 = (v2 * t >= 0f) ? t : float.NaN;

                            if (!float.IsNaN(t2) && !float.IsNaN(t1))
                            {
                                if (t1 >= delay && t2 >= delay)
                                {
                                    t1 = Math.Min(t1, t2);
                                }
                                else if (t2 >= delay)
                                {
                                    t1 = t2;
                                }
                            }
                        }
                    }
                }
            }
            else if (Math.Abs(dist) < float.Epsilon)
            {
                t1 = 0f;
            }

            return new Object[] { t1, (!float.IsNaN(t1)) ? new Vector2(sP1x + S * t1, sP1y + K * t1) : new Vector2() };
        }
    }
}
