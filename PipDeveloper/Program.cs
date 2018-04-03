using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.Math;
using BattleRight.Core.Models;

using BattleRight.SDK;
using BattleRight.SDK.Events;
using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Models;
using BattleRight.SDK.UI.Values;

using PipDeveloper.Extensions;

namespace PipDeveloper
{
    class Program
    {
        private static Menu DevMenu = null;
        private static Player DevHero;

        private static Projectile LastProj = null;
        private static Vector2 LastProjPosition;

        private static Stopwatch ProjSpeedSW = new Stopwatch();
        private static float ProjSpeedDistance;

        static void Main(string[] args)
        {
            var _devMenu = new Menu("pipdevelopermenu", "DaPipex's Developer Helper");

            _devMenu.AddLabel("Projectiles");
            _devMenu.Add(new MenuCheckBox("proj.name", "Last Projectile Name", true));
            _devMenu.Add(new MenuCheckBox("proj.range", "Last Projectile Range", false));
            _devMenu.Add(new MenuCheckBox("proj.radius", "Last Projectile Radius", false));
            _devMenu.Add(new MenuCheckBox("proj.speed", "Last Projectile Speed", false));

            _devMenu.AddSeparator(10f);

            _devMenu.AddLabel("Misc");
            _devMenu.Add(new MenuCheckBox("misc.activeGOs", "Active GameObjects", false));
            _devMenu.Add(new MenuCheckBox("misc.activeGOs.distance", "    ^ Distance", false));
            _devMenu.Add(new MenuCheckBox("misc.mySpellRadius", "My Spell Radius", false));
            _devMenu.Add(new MenuCheckBox("misc.charName", "My charName", false));
            _devMenu.Add(new MenuCheckBox("misc.spellsNames", "My spells' names", false));
            _devMenu.Add(new MenuCheckBox("misc.healths", "My healths", false));
            _devMenu.Add(new MenuCheckBox("misc.buffNames", "My buff names", false));

            _devMenu.AddSeparator(10f);

            _devMenu.AddLabel("Drawings");
            _devMenu.Add(new MenuCheckBox("draw.customCircle", "Draw custom circle", true));
            _devMenu.Add(new MenuSlider("draw.customCircle.range", "    ^ Range", 9.5f, 10f, 0f));
            _devMenu.Add(new MenuCheckBox("draw.customCircle.increase", "    ^ Increase by 0.1", false));
            _devMenu.Add(new MenuCheckBox("draw.customCircle.decrease", "    ^ Decrease by 0.1", false));

            MainMenu.AddMenu(_devMenu);

            CustomEvents.Instance.OnUpdate += delegate
            {
                DevMenu = _devMenu;

                DevHero = EntitiesManager.LocalPlayer;

                OnUpdate();
            };

            CustomEvents.Instance.OnDraw += OnDraw;
        }

        private static void OnUpdate()
        {
            if (!Game.IsInGame)
            {
                return;
            }

            ProjectileDebug();
            MiscDebug();
        }

        private static void ProjectileDebug()
        {
            Projectile _lastProj = null;

            if (EntitiesManager.ActiveProjectiles.Any())
            {
                _lastProj = EntitiesManager.ActiveProjectiles.Where(x => x.TeamId == DevHero.TeamId).LastOrDefault();

                if (_lastProj != null && !_lastProj.IsSame(LastProj))
                {
                    if (DevMenu.GetBoolean("proj.name"))
                    {
                        Console.WriteLine("Name: " + _lastProj.ObjectName);
                    }

                    if (DevMenu.GetBoolean("proj.range"))
                    {
                        Console.WriteLine("Range: " + _lastProj.Range);
                    }

                    if (DevMenu.GetBoolean("proj.radius"))
                    {
                        Console.WriteLine("Radius: " + _lastProj.SpellCollisionRadius);
                    }

                    if (DevMenu.GetBoolean("proj.speed"))
                    {
                        ProjSpeedSW.Reset();
                        ProjSpeedSW.Start();

                        ProjSpeedDistance = Vector2.Distance(_lastProj.CalculatedEndPosition, _lastProj.StartPosition);
                    }
                }

            }

            LastProj = _lastProj;

            if (ProjSpeedSW.IsRunning && LastProj == null)
            {
                ProjSpeedSW.Stop();

                var time = ProjSpeedSW.Elapsed.TotalSeconds;
                var speed = ProjSpeedDistance / time;

                Console.WriteLine("Speed " + speed);
            }
        }

        private static void MiscDebug()
        {
            if (DevMenu.GetBoolean("misc.activeGOs"))
            {
                var aGOs = EntitiesManager.ActiveGameObjects;
                foreach (var aGO in aGOs)
                {
                    string distance = string.Empty;
                    if (DevMenu.GetBoolean("misc.activeGOs.distance"))
                    {
                        distance = Vector2.Distance(EntitiesManager.LocalPlayer.WorldPosition, aGO.WorldPosition).ToString();
                    }

                    Console.WriteLine(aGO.ObjectName + (string.IsNullOrEmpty(distance) ? string.Empty : (" - Distance: " + distance)));
                }

                DevMenu.SetBoolean("misc.activeGOs", false);
            }

            if (DevMenu.GetBoolean("misc.mySpellRadius"))
            {
                Console.WriteLine("Spell Collision Radius: " + DevHero.SpellCollisionRadius);

                DevMenu.SetBoolean("misc.mySpellRadius", false);
            }

            if (DevMenu.GetBoolean("misc.charName"))
            {
                Console.WriteLine("My charName is: " + DevHero.CharName);

                DevMenu.SetBoolean("misc.charName", false);
            }

            if (DevMenu.GetBoolean("misc.spellsNames"))
            {
                foreach (var aHud in LocalPlayer.AbilitesHud)
                {
                    Console.WriteLine("Slot: " + aHud.SlotIndex + " - Name: " + aHud.Name);

                    DevMenu.SetBoolean("misc.spellsNames", false);
                }
            }

            if (DevMenu.GetBoolean("misc.healths"))
            {
                Console.WriteLine("Health: " + DevHero.Health);
                Console.WriteLine("MaxHealth: " + DevHero.MaxHealth);
                Console.WriteLine("RecoveryHealth: " + DevHero.RecoveryHealth);
                Console.WriteLine("MaxRecoveryHealth: " + DevHero.MaxRecoveryHealth);
                Console.WriteLine("CriticalHealth: " + DevHero.CriticalHealth);
                Console.WriteLine("MaxCriticalHealth: " + DevHero.MaxCriticalHealth);

                DevMenu.SetBoolean("misc.healths", false);
            }

            if (DevMenu.GetBoolean("misc.buffNames"))
            {
                if (DevHero.Buffs.Any())
                {
                    foreach (var buff in DevHero.Buffs)
                    {
                        Console.WriteLine(buff.ObjectName);
                    }
                }
                else
                {
                    Console.WriteLine("No buff detected on your Player");
                }

                DevMenu.SetBoolean("misc.buffNames", false);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (DevMenu.GetBoolean("draw.customCircle.increase"))
            {
                DevMenu.SetSlider("draw.customCircle.range", DevMenu.GetSlider("draw.customCircle.range") + 0.1f);
                DevMenu.SetBoolean("draw.customCircle.increase", false);
            }

            if (DevMenu.GetBoolean("draw.customCircle.decrease"))
            {
                DevMenu.SetSlider("draw.customCircle.range", DevMenu.GetSlider("draw.customCircle.range") - 0.1f);
                DevMenu.SetBoolean("draw.customCircle.decrease", false);
            }

            if (DevMenu.GetBoolean("draw.customCircle"))
            {
                var range = DevMenu.GetSlider("draw.customCircle.range");
                Drawing.DrawCircle(EntitiesManager.LocalPlayer.WorldPosition, range, UnityEngine.Color.green);
            }
        }
    }
}
