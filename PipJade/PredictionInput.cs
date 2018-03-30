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

namespace NewPrediction
{
    public class PredictionInput
    {
        #region Fields

        public float Speed;

        public float Radius;

        public float Delay;

        public float Range;

        public Player Target;

        public SkillType SkillType = SkillType.Line;

        public CollisionFlags CollidesWith = CollisionFlags.Bush | CollisionFlags.NPCBlocker | CollisionFlags.InvisWalls;

        private Vector2 _from;

        #endregion

        #region Properties

        public Vector2 From
        {
            get
            {
                return this._from != default(Vector2) ? this._from : EntitiesManager.LocalPlayer.WorldPosition;
            }
            set
            {
                this._from = value;
            }
        }

        #endregion

        #region Methods

        public PredictionInput(
            Vector2 from,
            Player target, 
            float speed, 
            float range, 
            float delay, 
            float radius, 
            SkillType skillType = SkillType.Line, 
            CollisionFlags collidesWith = CollisionFlags.Bush | CollisionFlags.NPCBlocker | CollisionFlags.InvisWalls)
        {
            From = from;
            Target = target;
            Speed = speed;
            Range = range;
            Delay = delay;
            Radius = radius;
            SkillType = skillType;
            CollidesWith = collidesWith;
        }

        #endregion
    }
}
