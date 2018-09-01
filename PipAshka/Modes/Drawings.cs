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

namespace PipAshka.Modes
{
    internal static class Drawings
    {
        private static Menu ComboMenu => AshkaMenu.ComboMenu;
        private static Menu DrawingsMenu => AshkaMenu.DrawingsMenu;

        private static string LastAbility => MainLogic.LastAbilityFired != null
            ? Enum.GetName(typeof(AbilitySlot), MainLogic.LastAbilityFired)
            : "None";

        private static readonly UnityEngine.Color RangeColor =
            new UnityEngine.Color(255f / 255f, 35f / 255f, 10f / 255f);
        private static readonly UnityEngine.Color SafeRangeColor =
            new UnityEngine.Color(255f / 255f, 122f / 255f, 23f / 255f);

        internal static void Draw()
        {
            if (DrawingsMenu.GetBoolean("disableAll"))
            {
                return;
            }

            var myPos = Utilities.Hero.MapObject.Position;

            //var screenCenterW = UnityEngine.Screen.width / 2;
            //var screenCenterH = UnityEngine.Screen.height / 2;
            //var centerVector = new Vector2(screenCenterW, screenCenterH);
            //Drawing.DrawString(centerVector, "Last ability: " + LastAbility, UnityEngine.Color.yellow,
            //    ViewSpace.ScreenSpacePixels);

            if (DrawingsMenu.GetBoolean("rangeM1"))
            {
                Drawing.DrawCircle(myPos, AH.M1.Range, RangeColor);
            }

            if (DrawingsMenu.GetBoolean("rangeM2"))
            {
                Drawing.DrawCircle(myPos, AH.M2.Range, RangeColor);
            }

            if (DrawingsMenu.GetBoolean("rangeM2.safeRange"))
            {
                var safeRange = ComboMenu.GetSlider("useM2.safeRange");
                Drawing.DrawCircle(myPos, safeRange, SafeRangeColor);
            }

            if (DrawingsMenu.GetBoolean("rangeF"))
            {
                Drawing.DrawCircle(myPos, AH.F.Range, RangeColor);
            }
        }
    }
}
