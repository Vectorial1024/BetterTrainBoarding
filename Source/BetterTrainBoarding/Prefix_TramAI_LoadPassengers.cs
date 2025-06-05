using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterTrainBoarding
{
    [HarmonyPatch(typeof(TramAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // Need to execute after IPT2; and IPT2 did not specify Priority => Priority = Normal
    [HarmonyPriority(Priority.LowerThanNormal)]
    public class Prefix_TramAI_LoadPassengers
    {
        [HarmonyPrefix]
        public static bool LoadPassengersBetter(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            PassengerTrainUtility.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
            return false;
        }
    }
}
