using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core.Math;

namespace NewPrediction
{
    public static class Geometry
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
