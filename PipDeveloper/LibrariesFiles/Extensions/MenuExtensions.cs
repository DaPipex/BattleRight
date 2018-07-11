using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Models;
using BattleRight.SDK.UI.Values;

namespace PipLibrary.Extensions
{
    public static class MenuExtensions
    {
        public static bool GetBoolean(this Menu menu, string menuItem)
        {
            var item = menu.Get<MenuCheckBox>(menuItem);

            if (item == null)
            {
                throw new Exception("GetBoolean: menuItem '" + menuItem + "' doesn't exist in " + menu.Name);
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
                throw new Exception("SetBoolean: menuItem '" + menuItem + "' doesn't exist in " + menu.Name);
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
                throw new Exception("GetSlider: menuItem '" + menuItem + "' doesn't exist in " + menu.Name);
            }
            else
            {
                return item.CurrentValue;
            }
        }

        public static void SetSlider(this Menu menu, string menuItem, float value)
        {
            var item = menu.Get<MenuSlider>(menuItem);

            if (item == null)
            {
                throw new Exception("SetSlider: menuItem '" + menuItem + "' doesn't exist in " + menu.Name);
            }
            else
            {
                item.CurrentValue = value;
            }
        }

        public static bool GetKeybind(this Menu menu, string menuItem)
        {
            var item = menu.Get<MenuKeybind>(menuItem);

            if (item == null)
            {
                throw new Exception("GetKeybind: menuItem '" + menuItem + "' doesn't exist in " + menu.Name);
            }
            else
            {
                return item.CurrentValue;
            }
        }

        public static int GetIntSlider(this Menu menu, string menuItem)
        {
            var item = menu.Get<MenuIntSlider>(menuItem);

            if (item == null)
            {
                throw new Exception("GetIntSlider: menuItem '" + menuItem + "' doesn't exist in " + menu.Name);
            }
            else
            {
                return item.CurrentValue;
            }
        }

        public static void SetIntSlider(this Menu menu, string menuItem, int value)
        {
            var item = menu.Get<MenuIntSlider>(menuItem);

            if (item == null)
            {
                throw new Exception("SetIntSlider: menuItem '" + menuItem + "' doesn't exist in " + menu.Name);
            }
            else
            {
                item.CurrentValue = value;
            }
        }

        public static Menu GetSubmenu(this Menu menu, string submenuName)
        {
            var submenu = menu.Get<Menu>(submenuName);

            if (submenu == null)
            {
                throw new Exception("GetSubmenu: submenu '" + submenuName + "' doesn't exist in " + menu.Name);
            }
            else
            {
                return submenu;
            }
        }
    }
}
