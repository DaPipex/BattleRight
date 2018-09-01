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
using PipAshka.Modes;
using PipAshka.Utils;

namespace PipAshka
{
    internal static class EventsManager
    {
        private static bool _didInit = false;

        internal static void Initialize()
        {
            Game.OnMatchStart += OnMatchStart;
            Game.OnMatchEnd += OnMatchEnd;
        }

        private static void OnMatchStart(EventArgs args)
        {
            if (Utilities.Hero == null || !Utilities.Hero.CharName.Equals(Utilities.HeroName))
            {
                return;
            }

            SubscribeEvents();

            if (Game.CurrentMatchState == MatchState.InRound)
            {
                BattleriteManager.Update();
            }

            _didInit = true;
        }

        private static void OnMatchEnd(EventArgs args)
        {
            if (_didInit)
            {
                UnsubscribeEvents();

                _didInit = false;
            }
        }

        private static void SubscribeEvents()
        {
            Game.OnUpdate += MainLogic.OnUpdate;
            Game.OnDraw += MainLogic.OnDraw;
            Game.OnMatchStateUpdate += MainLogic.OnMatchStateUpdate;
        }

        private static void UnsubscribeEvents()
        {
            Game.OnUpdate -= MainLogic.OnUpdate;
            Game.OnDraw -= MainLogic.OnDraw;
            Game.OnMatchStateUpdate -= MainLogic.OnMatchStateUpdate;
        }
    }
}
