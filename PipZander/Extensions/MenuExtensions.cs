using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Models;
using BattleRight.SDK.UI.Values;

namespace PipZander.Extensions
{
    public static class MenuExtensions
    {
        public static bool GetBoolean(this Menu menu, string menuItem)
        {
            var item = menu.Get<MenuCheckBox>(menuItem);

            if (item == null)
            {
                throw new Exception("GetBoolean: menuItem '" + menuItem + "' doesn't exist");
            }
            else
            {
                return item.CurrentValue;
            }
        }

        public static void SetBoolean(this Menu menu, string menuItem, bool value)
        {
            var item = menu.Get<MenuCheckBox>(menuItem);

            if (item == null)
            {
                throw new Exception("SetBoolean: menuItem '" + menuItem + "' doesn't exist");
            }
            else
            {
                item.CurrentValue = value;
            }
        }

        public static void SetSlider(this Menu menu, string menuItem, float value)
        {
            var item = menu.Get<MenuSlider>(menuItem);

            if (item == null)
            {
                throw new Exception("SetBoolean: menuItem '" + menuItem + "' doesn't exist");
            }
            else
            {
                item.CurrentValue = value;
            }
        }

        public static float GetSlider(this Menu menu, string menuItem)
        {
            var item = menu.Get<MenuSlider>(menuItem);

            if (item == null)
            {
                throw new Exception("GetSlider: menuItem '" + menuItem + "' doesn't exist");
            }
            else
            {
                return item.CurrentValue;
            }
        }
    }
}
