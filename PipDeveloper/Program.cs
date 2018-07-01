using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.Core.GameObjects.Models;
using BattleRight.Core.Math;
using BattleRight.Core.Models;
using BattleRight.Helper;
using BattleRight.Sandbox;
using BattleRight.SDK;
using BattleRight.SDK.Events;
using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Models;
using BattleRight.SDK.UI.Values;

using PipDeveloper.Extensions;
using PipLibrary.Extensions;

namespace PipDeveloper
{
    class Program : IAddon
    {
        private static Menu _devMenu;
        private static Character DevHero;

        private static Projectile LastProj = null;
        private static Vector2 LastProjPosition;

        private static Stopwatch ProjSpeedSW = new Stopwatch();
        private static Stopwatch CustomSW = new Stopwatch();
        private static float ProjSpeedDistance;

        //foreach (var aGO in aGOs)
        //        {
        //            string distance = string.Empty;
        //            if (_devMenu.GetBoolean("misc.activeGOs.distance"))
        //            {
        //                distance = Vector2.Distance(DevHero.MapObject.Position, aGO.Get<MapGameObject>().Position).ToString();
        //            }

        //            Console.WriteLine(aGO.ObjectName + (string.IsNullOrEmpty(distance) ? string.Empty : (" - Distance: " + distance)));
        //        }

        public void OnUnload()
        {

        }

        public void OnInit()
        {
            _devMenu = new Menu("pipdevelopermenu", "DaPip's Developer Helper");

            _devMenu.AddLabel("Projectiles");
            _devMenu.Add(new MenuCheckBox("proj.name", "Last Projectile Name", false));
            _devMenu.Add(new MenuCheckBox("proj.range", "Last Projectile Range", false));
            _devMenu.Add(new MenuCheckBox("proj.radius", "Last Projectile Radius", false));
            _devMenu.Add(new MenuCheckBox("proj.speed", "Last Projectile Speed", false));

            //_devMenu.AddSeparator(10f);

            //_devMenu.AddLabel("Abilities");
            //_devMenu.Add(new MenuCheckBox("skill.infoM1", "Display M1 info", false));
            //_devMenu.Add(new MenuCheckBox("skill.infoM2", "Display M2 info", false));
            //_devMenu.Add(new MenuCheckBox("skill.infoSpace", "Display Space info", false));
            //_devMenu.Add(new MenuCheckBox("skill.infoQ", "Display Q info", false));
            //_devMenu.Add(new MenuCheckBox("skill.infoE", "Display E info", false));
            //_devMenu.Add(new MenuCheckBox("skill.infoR", "Display R info", false));
            //_devMenu.Add(new MenuCheckBox("skill.infoF", "Display F info", false));
            //_devMenu.Add(new MenuCheckBox("skill.infoEX1", "Display EX1 info", false));
            //_devMenu.Add(new MenuCheckBox("skill.infoEX2", "Display EX2 info", false));

            _devMenu.AddSeparator(10f);

            _devMenu.AddLabel("GameObjects");
            _devMenu.Add(new MenuCheckBox("go.allNames", "Print all current objects", false));

            _devMenu.AddSeparator(10f);

            _devMenu.AddLabel("Misc");
            _devMenu.Add(new MenuCheckBox("misc.mySpellRadius", "My Spell Radius", false));
            _devMenu.Add(new MenuCheckBox("misc.charName", "My charName", false));
            _devMenu.Add(new MenuCheckBox("misc.spellsNames", "My spells' names", false));
            _devMenu.Add(new MenuCheckBox("misc.healths", "My healths", false));
            _devMenu.Add(new MenuCheckBox("misc.buffNames", "My buff names", false));

            _devMenu.AddSeparator(10f);

            _devMenu.AddLabel("Object create/destroy");
            _devMenu.Add(new MenuCheckBox("obj.create", "Print info of objects created", false));
            _devMenu.Add(new MenuCheckBox("obj.destroy", "Print info of objects destroyed", false));

            _devMenu.AddSeparator(10f);

            _devMenu.AddLabel("Drawings");
            _devMenu.Add(new MenuCheckBox("draw.customCircle", "Draw custom circle", true));
            _devMenu.Add(new MenuSlider("draw.customCircle.range", "    ^ Range", 9.5f, 10f, 0f));
            _devMenu.Add(new MenuCheckBox("draw.customCircle.increase", "    ^ Increase by 0.1", false));
            _devMenu.Add(new MenuCheckBox("draw.customCircle.decrease", "    ^ Decrease by 0.1", false));

            _devMenu.AddSeparator(10f);

            _devMenu.AddLabel("Special Debug");
            _devMenu.Add(new MenuCheckBox("debug.stw", "Screen to World Test", false));
            _devMenu.Add(new MenuSlider("debug.stw.xSlider", "X", 960f, 1920f, 0f));
            _devMenu.Add(new MenuSlider("debug.stw.ySlider", "Y", 540f, 1080f, 0f));
            //_devMenu.Add(new MenuCheckBox("debug.stw.cameraInfo", "Print camera info", false));
            _devMenu.Add(new MenuCheckBox("debug.stw.ray.useSliders", "Use X and Y Sliders instead of mouse pos", false));
            _devMenu.Add(new MenuKeybind("debug.keybind", "Keybind Test", UnityEngine.KeyCode.T, false, false));
            _devMenu.Add(new MenuKeybind("debug.keybind.toggle", "Toggle Keybind Test", UnityEngine.KeyCode.G, false, true));

            MainMenu.AddMenu(_devMenu);

            Game.OnUpdate += OnUpdate;
            Game.OnDraw += OnDraw;
            InGameObject.OnCreate += OnCreate;
            InGameObject.OnDestroy += OnDestroy;
        }

        private static void PrintAbilityInfo(AbilitySlot slot)
        {
            var hudData = LocalPlayer.GetAbilityHudData(slot);
            var ability = DevHero.AbilitySystem.GetAbility(hudData.SlotIndex);
        }

        private static void OnCreate(InGameObject inGameObject)
        {
            if (_devMenu.GetBoolean("obj.create"))
            {
                Console.WriteLine(inGameObject.ObjectName + " of type " + inGameObject.GetType().ToString() + " created");
            }

            var proj = inGameObject as Projectile;
            if (proj != null && proj.BaseObject.TeamId == DevHero.BaseObject.TeamId)
            {
                if (_devMenu.GetBoolean("proj.name"))
                {
                    Console.WriteLine("Name: " + proj.ObjectName);
                }

                if (_devMenu.GetBoolean("proj.range"))
                {
                    Console.WriteLine("Range: " + proj.Range);
                }

                if (_devMenu.GetBoolean("proj.radius"))
                {
                    Console.WriteLine("Radius: " + proj.Radius);
                }

                if (_devMenu.GetBoolean("proj.speed"))
                {
                    if (LastProj == null)
                    {
                        LastProj = proj;

                        ProjSpeedSW.Reset();
                        ProjSpeedSW.Start();
                    }
                }
            }
        }

        private static void OnDestroy(InGameObject inGameObject)
        {
            if (_devMenu.GetBoolean("obj.destroy"))
            { 
                Console.WriteLine(inGameObject.ObjectName + " of type " + inGameObject.GetType().ToString() + " destroyed");
            }

            var proj = inGameObject as Projectile;
            if (proj != null && proj.BaseObject.TeamId == DevHero.BaseObject.TeamId)
            {
                if (ProjSpeedSW.IsRunning && proj.IsSame(LastProj))
                {
                    ProjSpeedSW.Stop();

                    LastProj = null;

                    var distance = proj.CalculatedEndPosition.Distance(proj.StartPosition);
                    var time = ProjSpeedSW.Elapsed.TotalSeconds;

                    var speed = distance / time;
                    Console.WriteLine("Speed: " + speed);
                }
            }
        }

        private static void OnMatchStart(EventArgs args)
        {

        }

        private static void OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            DevHero = EntitiesManager.LocalPlayer;

            //ProjectileDebug();
            MiscDebug();
            SpecialDebug();
        }

        private static void ProjectileDebug()
        {
            Projectile _lastProj = null;

            if (EntitiesManager.ActiveProjectiles.Any())
            {
                //Console.WriteLine("ActiveProjectile(s) found");
                //Console.WriteLine("Last proj teamID: " + EntitiesManager.ActiveProjectiles.LastOrDefault().TeamId);
                //Console.WriteLine("Hero teamID: " + DevHero.TeamId);

                _lastProj = EntitiesManager.ActiveProjectiles.Where(x => x.BaseObject.TeamId == DevHero.BaseObject.TeamId).LastOrDefault();

                //if (_lastProj == null)
                //{
                //    Console.WriteLine("No projectile found wtf");
                //}
                //else
                //{
                //    Console.WriteLine("Proj of name: " + _lastProj.ObjectName + " found");
                //}

                if (_lastProj != null && !_lastProj.IsSame(LastProj))
                {
                    if (_devMenu.GetBoolean("proj.name"))
                    {
                        Console.WriteLine("Name: " + _lastProj.ObjectName);
                    }

                    if (_devMenu.GetBoolean("proj.range"))
                    {
                        Console.WriteLine("Range: " + _lastProj.Range);
                    }

                    if (_devMenu.GetBoolean("proj.radius"))
                    {
                        Console.WriteLine("Radius: " + _lastProj.Radius);
                    }

                    if (_devMenu.GetBoolean("proj.speed"))
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
            if (_devMenu.GetBoolean("go.allNames"))
            {
                foreach (var obj in EntitiesManager.InGameObjects)
                {
                    Console.WriteLine(obj.ObjectName);

                    foreach (var basetype in obj.GetBaseTypes())
                    {
                        Console.WriteLine("Basetype: " + basetype);
                    }
                }

                _devMenu.SetBoolean("go.allNames", false);
            }

            if (_devMenu.GetBoolean("misc.mySpellRadius"))
            {
                Console.WriteLine("Spell Collision Radius: " + DevHero.SpellCollision.SpellCollisionRadius);

                _devMenu.SetBoolean("misc.mySpellRadius", false);
            }

            if (_devMenu.GetBoolean("misc.charName"))
            {
                Console.WriteLine("My charName is: " + DevHero.CharName);

                _devMenu.SetBoolean("misc.charName", false);
            }

            if (_devMenu.GetBoolean("misc.spellsNames"))
            {
                foreach (var aHud in LocalPlayer.AbilitiesHud)
                {
                    Console.WriteLine("Slot: " + aHud.SlotIndex + " - Name: " + aHud.Name);

                    _devMenu.SetBoolean("misc.spellsNames", false);
                }
            }

            if (_devMenu.GetBoolean("misc.healths"))
            {
                Console.WriteLine("Health: " + DevHero.Living.Health);
                Console.WriteLine("MaxHealth: " + DevHero.Living.MaxHealth);
                Console.WriteLine("RecoveryHealth: " + DevHero.Living.RecoveryHealth);
                Console.WriteLine("MaxRecoveryHealth: " + DevHero.Living.MaxRecoveryHealth);

                _devMenu.SetBoolean("misc.healths", false);
            }

            if (_devMenu.GetBoolean("misc.buffNames"))
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

                _devMenu.SetBoolean("misc.buffNames", false);
            }
        }

        private static void SpecialDebug()
        {
            //if (_devMenu.GetBoolean("debug.stw.cameraInfo"))
            //{
            //    UnityEngine.Camera cam = UnityEngine.Camera.main;

            //    Console.WriteLine("Position: " + cam.transform.position.ToString());
            //    Console.WriteLine("Rotation: " + cam.transform.rotation.ToString());
            //    Console.WriteLine("Is orthographic: " + cam.orthographic);
            //    Console.WriteLine("Name: " + cam.name);
            //    Console.WriteLine(string.Empty);

            //    _devMenu.SetBoolean("debug.stw.cameraInfo", false);
            //}
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            if (_devMenu.GetBoolean("draw.customCircle.increase"))
            {
                _devMenu.SetSlider("draw.customCircle.range", _devMenu.GetSlider("draw.customCircle.range") + 0.1f);
                _devMenu.SetBoolean("draw.customCircle.increase", false);
            }

            if (_devMenu.GetBoolean("draw.customCircle.decrease"))
            {
                _devMenu.SetSlider("draw.customCircle.range", _devMenu.GetSlider("draw.customCircle.range") - 0.1f);
                _devMenu.SetBoolean("draw.customCircle.decrease", false);
            }

            if (_devMenu.GetBoolean("draw.customCircle"))
            {
                var range = _devMenu.GetSlider("draw.customCircle.range");
                Drawing.DrawCircle(DevHero.MapObject.Position, range, UnityEngine.Color.green);
            }

            if (_devMenu.GetBoolean("debug.stw"))
            {
                UnityEngine.Camera cam = UnityEngine.Camera.main;

                var sliderX = _devMenu.GetSlider("debug.stw.xSlider");
                var sliderY = _devMenu.GetSlider("debug.stw.ySlider");

                var useSliders = _devMenu.GetBoolean("debug.stw.ray.useSliders");
                UnityEngine.Ray ray = cam.ScreenPointToRay(useSliders ? new UnityEngine.Vector3(sliderX, sliderY) : UnityEngine.Input.mousePosition);
                UnityEngine.Plane plane = new UnityEngine.Plane(UnityEngine.Vector3.up, UnityEngine.Vector3.zero);

                float d;

                if (plane.Raycast(ray, out d))
                {
                    var drawPos = new Vector2(ray.GetPoint(d).x, ray.GetPoint(d).z);

                    Drawing.DrawCircle(drawPos, 2.5f, UnityEngine.Color.red);
                    Drawing.DrawString(drawPos, "C", UnityEngine.Color.cyan);
                }
            }

            if (_devMenu.Get<MenuKeybind>("debug.keybind").CurrentValue)
            {
                Drawing.DrawCircle(DevHero.MapObject.Position, 2f, UnityEngine.Color.yellow);
            }

            if (_devMenu.Get<MenuKeybind>("debug.keybind.toggle").CurrentValue)
            {
                Drawing.DrawCircle(DevHero.MapObject.Position, 3f, UnityEngine.Color.magenta);
            }
        }
    }
}
