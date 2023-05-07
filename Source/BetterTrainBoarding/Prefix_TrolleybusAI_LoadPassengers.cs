using HarmonyLib;
using System.Collections.Generic;

namespace BetterTrainBoarding
{
    [HarmonyPatch(typeof(BusAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // need to execute after our other mod, Express Bus Services
    [HarmonyAfter(new string[] { PatchController.ExpressBusServicesHarmonyID })]
    public class Prefix_TrolleybusAI_LoadPassengers
    {
        [HarmonyPrefix]
        public static bool PreFix(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
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
