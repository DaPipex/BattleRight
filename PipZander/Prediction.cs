using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.Math;
using BattleRight.Core.Models;

using NewPrediction.Enumerations;
using NewPrediction.Extensions;

namespace NewPrediction
{
    public static class Prediction
    {
        public static PredictionOutput GetPrediction(PredictionInput input, bool includePing)
        {
            PredictionOutput result = new PredictionOutput();

            if (!input.Target.IsValidTarget(float.MaxValue, input.From))
            {
                result.Hitchance = Hitchance.Impossible;
                return result;
            }

            if (includePing)
            {
                input.Delay += EntitiesManager.LocalPlayer.Latency / 2000f;
            }

            if (Vector2.Distance(input.Target.WorldPosition, input.From) > input.Range * 1.4f)
            {
                result.Input = input;
                result.Hitchance = Hitchance.OutOfRange;
                return result;
            }

            //TODO: Check immobile & dashing

            result = GetStandardPrediction(input);

            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon)
            {
                if (result.Hitchance >= Hitchance.High
                    && Vector2.Distance(input.From, input.Target.WorldPosition) > input.Range * input.Radius * 3 / 4) //Use ScreenPosition instead?
                {
                    result.Hitchance = Hitchance.Medium;
                }

                if (Vector2.Distance(input.From, result.TargetPosition) > input.Range + (input.SkillType == SkillType.Circle ? input.Radius : 0))
                {
                    result.Hitchance = Hitchance.OutOfRange;
                }

                if (Vector2.Distance(input.From, result.MousePosition) > input.Range)
                {
                    if (result.Hitchance != Hitchance.OutOfRange)
                    {
                        result.MousePosition = input.From + input.Range * (result.TargetPosition - input.From).Normalized;
                    }
                    else
                    {
                        result.Hitchance = Hitchance.OutOfRange;
                    }
                }
            }

            if (input.CollidesWith != 0)
            {
                var colResult = CollisionSolver.CheckThickLineCollision(input.From, result.MousePosition, input.Radius, input.CollidesWith);

                if (colResult.IsColliding)
                {
                    result.CollisionPoint = colResult.CollisionPoint;

                    result.Hitchance = Hitchance.Collision;
                }
            }

            result.MousePosition = result.MousePosition.WorldToScreen();

            return result;
        }

        internal static PredictionOutput GetStandardPrediction(PredictionInput input)
        {
            var speed = input.Target.Velocity.Length();

            var xDir = input.Target.Velocity.X == 0 ? 0 : input.Target.Velocity.X > 0 ? 1 : -1;
            var yDir = input.Target.Velocity.Y == 0 ? 0 : input.Target.Velocity.Y > 0 ? 1 : -1;
            var dirVector = new Vector2(xDir, yDir);

            var extendedPos = input.Target.WorldPosition + dirVector;

            var result = GetPositionOnDirection(input, extendedPos, speed);

            return result;
        }

        internal static PredictionOutput GetPositionOnDirection(PredictionInput input, Vector2 extendedPosition, float targetSpeed)
        {
            if (input.Target.Velocity == default(Vector2))
            {
                return new PredictionOutput()
                {
                    Input = input,
                    TargetPosition = input.Target.WorldPosition,
                    MousePosition = input.Target.WorldPosition,
                    Hitchance = Hitchance.VeryHigh,
                };
            }

            if (Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
            {
                var distance = input.Delay * targetSpeed - input.Radius;

                var a = input.Target.WorldPosition;
                var b = extendedPosition;
                var vectorDist = Vector2.Distance(a, b);
                var direction = (b - a).Normalized;
                var unitPos = a + direction * distance;

                return new PredictionOutput()
                {
                    Input = input,
                    TargetPosition = unitPos,
                    MousePosition = unitPos,
                    Hitchance = Hitchance.High,
                };
            }

            if (Math.Abs(input.Speed - float.MaxValue) > float.Epsilon)
            {
                var distance = input.Delay * targetSpeed - input.Radius;

                var a = input.Target.WorldPosition;
                var b = extendedPosition;
                var time = Vector2.Distance(a, b) / targetSpeed;
                var direction = (b - a).Normalized;
                var c = a - targetSpeed * time * direction;

                var solution = Geometry.VectorMovementCollision(c, b, targetSpeed, input.From, input.Speed, time);
                var tSol = (float)solution[0];
                var pos = (Vector2)solution[1];

                if (pos != default(Vector2) && tSol >= time && tSol <= time * 2)
                {
                    var p = pos + input.Radius * direction;

                    return new PredictionOutput()
                    {
                        Input = input,
                        TargetPosition = p,
                        MousePosition = pos,
                        Hitchance = Hitchance.High,
                    };
                }
            }

            return new PredictionOutput()
            {
                Input = input,
                TargetPosition = extendedPosition,
                MousePosition = extendedPosition,
                Hitchance = Hitchance.Medium,
            };
        }
    }
}
