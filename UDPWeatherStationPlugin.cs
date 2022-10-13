using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using MissionPlanner.Controls.PreFlight;
using MissionPlanner.Controls;
using System.Linq;

namespace MissionPlanner.UDPWeatherStation
{
    public class UDPWeatherStationPlugin : MissionPlanner.Plugin.Plugin
    {
        public override string Name
        {
            get { return "UDP Weather Station"; }
        }

        public override string Version
        {
            get { return "0.1"; }
        }

        public override string Author
        {
            get { return "Daniel Szilagyi (LEGIONBOSS)"; }
        }

        //[DebuggerHidden]
        public override bool Init()
		//Init called when the plugin dll is loaded
        {
            loopratehz = 1;  //Loop runs every second (The value is in Hertz, so 2 means every 500ms, 0.1f means every 10 second...) 

            return true;	 // If it is false then plugin will not load
        }

        public override bool Loaded()
		//Loaded called after the plugin dll successfully loaded
        {
            return true;     //If it is false plugin will not start (loop will not called)
        }

        public override bool Loop()
		//Loop is called in regular intervalls (set by loopratehz)
        {

            return true;	//Return value is not used
        }

        public override bool Exit()
		//Exit called when plugin is terminated (usually when Mission Planner is exiting)
        {
            return true;	//Return value is not used
        }
    }
}
