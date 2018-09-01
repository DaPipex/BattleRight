using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.SDK;
using BattleRight.SDK.Enumeration;
using BattleRight.SDK.Events;
using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Models;
using BattleRight.SDK.UI.Values;

using PipAshka.Modes;

using AH = PipAshka.AbilityHandler;

namespace PipAshka.Utils
{
    internal static class AshkaMenu
    {
        internal static Menu RootMenu;
        internal static Menu KeysMenu, ComboMenu, KSMenu, DrawingsMenu;

        internal static void LoadMenu()
        {
            RootMenu = new Menu("pipashkamenu", "DaPip's Ashka");

            KeysMenu = new Menu("keysmenu", "Keys", true);
            KeysMenu.Add(new MenuKeybind("combo", "Combo", UnityEngine.KeyCode.LeftControl, false, false));
            RootMenu.Add(KeysMenu);

            ComboMenu = new Menu("combomenu", "Combo", true);
            ComboMenu.Add(new MenuCheckBox("invisible", "Attack invisible targets", true));
            ComboMenu.Add(new MenuCheckBox("interrupt", "Interrupt M1 or M2 when target gets out of range", false));
            ComboMenu.Add(new MenuCheckBox("useM1", "Use M1 (Fireball)", true));
            ComboMenu.Add(new MenuCheckBox("useM1.priority", "    ^ Take priority over M2 and Q when M1 is enhanced", true));
            ComboMenu.Add(new MenuCheckBox("useM2", "Use M2 (Fire Storm)", true));
            ComboMenu.Add(new MenuSlider("useM2.safeRange", "    ^ Safe Range", 3f, AH.M2.Range - 1f, 0f));
            ComboMenu.Add(new MenuCheckBox("useSpace", "Use Space (Searing Flight) when enemies are too close", true));
            ComboMenu.Add(new MenuCheckBox("useQ", "Use Q (Flamestrike)", true));
            ComboMenu.Add(new MenuCheckBox("useQ.chainCC", "    ^ Only on CC", false));
            ComboMenu.Add(new MenuCheckBox("useE.condemn", "Use E (Molten Fist) to knock enemy into a wall (if Knockout is enabled)", true));
            ComboMenu.Add(new MenuCheckBox("useE.near", "Use E (Molten Fist) to push enemies if they are too close", true));
            ComboMenu.Add(new MenuSlider("useE.near.safeRange", "    ^ Safe Range", 2f, AH.E.Range - 1f, 0f));
            //ComboMenu.Add(new MenuCheckBox("useR.harass", "Use R (Firewall) to upgrade M1 projectiles (if conflagration is enabled)", false));
            //ComboMenu.Add(new MenuIntSlider("useR.harass.energy", "    ^ Min energy bars", 3, 4, 1));
            ComboMenu.Add(new MenuCheckBox("useEX1", "Use EX1 (Searing Fire) when enemies are too close", false));
            ComboMenu.Add(new MenuIntSlider("useEX1.energy", "    ^ Min energy bars", 3, 4, 2));
            ComboMenu.Add(new MenuSlider("useEX1.minHP", "    ^ Min health %", 60f, 100f, 0f));
            ComboMenu.Add(new MenuCheckBox("useEX2.enemies", "Use EX2 (Molten Chains) when X enemies are in range", false));
            ComboMenu.Add(new MenuIntSlider("useEX2.enemies.count", "    ^ X >=", 2, 3, 1));
            ComboMenu.Add(new MenuCheckBox("useEX2.panic", "Use EX2 (Molten Chains) when HP% is too low and there's an enemy near", false));
            ComboMenu.Add(new MenuSlider("useEX2.panic.minHP", "    ^ Min health %", 35f, 100f, 0f));
            ComboMenu.Add(new MenuIntSlider("useEX2.energy", "Min energy bars to use any of EX2 modes", 2, 4, 1));
            ComboMenu.Add(new MenuCheckBox("useF", "Use F (Infernal Scorch)", false));
            RootMenu.Add(ComboMenu);

            KSMenu = new Menu("ksmenu", "Killsteal", true);
            //KSMenu.Add(new MenuCheckBox("useF", "Use F (Infernal Scorch) to killsteal", true));
            RootMenu.Add(KSMenu);

            DrawingsMenu = new Menu("drawingsmenu", "Drawings", true);
            DrawingsMenu.Add(new MenuCheckBox("disableAll", "Disable all drawings", false));
            DrawingsMenu.AddSeparator(10f);
            DrawingsMenu.Add(new MenuCheckBox("rangeM1", "Draw M1 (Fireball) Range", false));
            DrawingsMenu.Add(new MenuCheckBox("rangeM2", "Draw M2 (Fire Storm) Range", true));
            DrawingsMenu.Add(new MenuCheckBox("rangeM2.safeRange", "Draw M2 (Fire Storm) Safe Range", false));
            DrawingsMenu.Add(new MenuCheckBox("rangeF", "Draw F (Infernal Scorch) Range", true));
            RootMenu.Add(DrawingsMenu);

            MainMenu.AddMenu(RootMenu);
        }
    }
}
