using CitizenFX.Core;
using System;

namespace vox.opticom
{
    public class server : BaseScript
    {
        public server()
        {
            EventHandlers["opticom:syncLights"] += new Action<int, bool>(OnSyncLights);
        }

        private void OnSyncLights(int trafficLight, bool isGreen)
        {
            TriggerClientEvent("opticom:syncLights", trafficLight, isGreen);
        }
    }
}