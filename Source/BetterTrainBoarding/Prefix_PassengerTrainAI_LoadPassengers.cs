using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BetterTrainBoarding
{
    [HarmonyPatch(typeof(PassengerTrainAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // Need to execute after IPT2; and IPT2 did not specify Priority => Priority = Normal
    [HarmonyPriority(Priority.LowerThanNormal)]
    public class Prefix_PassengerTrainAI_LoadPassengers
    {

        [HarmonyPrefix]
        public static bool PreFix(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            // BusPickDropLookupTable.DetermineIfBusShouldDepart(ref __result, vehicleID, ref vehicleData);
            bool isFull;
            List<PassengerTrainUtility.CompartmentInfo> analysis;
            PassengerTrainUtility.AnalyzeTrain(vehicleID, out isFull, out analysis);
            if (!isFull)
            {
                // this gets triggered 99% of the time, but eh who cares.
                PassengerTrainUtility.SensiblyLoadPassengers(vehicleID, currentStop, nextStop, analysis);
                return false;
            }
            return true;
        }
    }
}
