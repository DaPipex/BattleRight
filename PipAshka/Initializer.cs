using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BattleRight.Core;

using BattleRight.Sandbox;

using PipAshka.Utils;

namespace PipAshka
{
    public class Initializer : IAddon
    {
        public void OnInit()
        {
            AbilityHandler.Initialize();
            AshkaMenu.LoadMenu();
            EventsManager.Initialize();
        }

        public void OnUnload()
        {

        }
    }
}
