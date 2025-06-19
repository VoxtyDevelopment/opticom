using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace vox.opticom
{
    public class client : BaseScript
    {
        private readonly Dictionary<int, int> lightTimers = new Dictionary<int, int>();
        private readonly Dictionary<string, int> debugCooldowns = new Dictionary<string, int>();
        public List<uint> TrafficLightObjects { get; set; } = new List<uint> { 0x3e2b73a4, 0x336e5e2a, 0xd8eba922, 0xd4729f50, 0x272244b2, 0x33986eae, 0x2323cdc5, 0xd8eba922, 0x53278b05 };


        public client()
        {
            Tick += OnTick;
        }

        private bool IsWhitelistedVehicle(int vehicle)
        {
            if (!ConfigLoader.UseWhitelist) return true;
            uint modelHash = (uint)API.GetEntityModel(vehicle);
            string modelName = API.GetDisplayNameFromVehicleModel(modelHash);
            return ConfigLoader.WhitelistedVehicles.Contains(modelName);
        }

        private void SetLightGreen(int light)
        {
            API.SetEntityTrafficlightOverride(light, 0);
            StopNearbyTraffic(light);

            if (!lightTimers.ContainsKey(light))
            {
                lightTimers[light] = API.GetGameTimer() + 5000;
            }

            Vector3 coords = API.GetEntityCoords(light, true);
            uint streetHash = 0, crossingHash = 0;
            API.GetStreetNameAtCoord(coords.X, coords.Y, coords.Z, ref streetHash, ref crossingHash);
            string streetName = API.GetStreetNameFromHashKey(streetHash);
            string crossingName = crossingHash != 0 ? API.GetStreetNameFromHashKey(crossingHash) : null;

            string streetInfo = crossingName != null ? $"{streetName} & {crossingName}" : streetName;
            string msg = $"Traffic light set to green at {streetInfo}";
            DebugPrint(msg, light);
        }

        private void ResetTrafficLight(int light)
        {
            API.SetEntityTrafficlightOverride(light, -1);
            lightTimers.Remove(light);
        }

        private void SyncLight(int light, bool isGreen)
        {
            TriggerServerEvent("opticom:syncLights", light, isGreen);
        }

        private void DebugPrint(string msg, int? id = null)
        {
            if (!ConfigLoader.Debug) return;
            int currentTime = API.GetGameTimer();
            string key = id?.ToString() ?? "global";

            if (!debugCooldowns.ContainsKey(key) || currentTime - debugCooldowns[key] > 5000)
            {
                Debug.WriteLine($"[Opticom Debug] {msg}");
                debugCooldowns[key] = currentTime;
            }
        }

        private Vector3 GetForwardPosition(Vector3 position, float heading, float distance)
        {
            float rad = (float)(heading * Math.PI / 180.0);
            return new Vector3(
                position.X - distance * (float)Math.Sin(rad),
                position.Y + distance * (float)Math.Cos(rad),
                position.Z
            );
        }

        private void StopNearbyTraffic(int light)
        {
            Vector3 lightPos = API.GetEntityCoords(light, true);
            float radius = 50.0f;
            Dictionary<int, bool> vehicles = new Dictionary<int, bool>();

            for (int i = 0; i < 50; i++)
            {
                Vector3 offset = API.GetOffsetFromEntityInWorldCoords(light,
                    API.GetRandomIntInRange(-(int)radius, (int)radius),
                    API.GetRandomIntInRange(-(int)radius, (int)radius),
                    0.0f
                );
                int veh = API.GetClosestVehicle(offset.X, offset.Y, offset.Z, 10.0f, 0, 70);
                if (veh != 0 && !vehicles.ContainsKey(veh))
                {
                    int ped = API.GetPedInVehicleSeat(veh, -1);
                    if (ped != 0 && !API.IsPedAPlayer(ped))
                    {
                        API.TaskVehicleTempAction(ped, veh, 27, 5000);
                        vehicles[veh] = true;
                    }
                }
            }
        }

        private async Task OnTick()
        {
            int player = API.GetPlayerPed(-1);

            int currentTime = API.GetGameTimer();
            List<int> lightsToReset = new List<int>();
            foreach (var timer in lightTimers)
            {
                if (currentTime >= timer.Value)
                {
                    lightsToReset.Add(timer.Key);
                }
            }

            foreach (var light in lightsToReset)
            {
                ResetTrafficLight(light);
            }

            if (API.IsPedInAnyVehicle(player, false))
            {
                int vehicle = API.GetVehiclePedIsIn(player, false);

                if (API.IsVehicleSirenOn(vehicle) && IsWhitelistedVehicle(vehicle))
                {
                    Vector3 pos = API.GetEntityCoords(player, true);
                    float heading = API.GetEntityHeading(player);
                    int foundLight = 0;
                    int pollDelay = 50;

                    for (float dist = 30.0f; dist >= 5.0f; dist -= 10.0f)
                    {
                        await Delay(pollDelay);

                        Vector3 checkPos = GetForwardPosition(pos, heading, dist);

                        foreach (uint objectType in TrafficLightObjects)
                        {
                            int light = API.GetClosestObjectOfType(checkPos.X, checkPos.Y, checkPos.Z, 20.0f, objectType, false, false, false);

                            if (light != 0)
                            {
                                float lightHeading = API.GetEntityHeading(light);
                                float headingDiff = Math.Abs(heading - lightHeading);

                                if (headingDiff < 40.0f || headingDiff > 320.0f)
                                {
                                    SetLightGreen(light);
                                    SyncLight(light, true);
                                    foundLight = light;
                                    break;
                                }
                            }
                        }

                        if (foundLight != 0)
                        {
                            float ratio = (dist - 5.0f) / (30.0f - 5.0f);
                            pollDelay = Math.Max(50, (int)Math.Floor(50 - ratio * (50 - 50)));
                            break;
                        }
                    }
                }
                else
                {
                    await Delay(1000);
                }
            }
            else
            {
                await Delay(1000);
            }
        }
    }
}