using HarmonyLib;

namespace BetterTrainBoarding
{
    [HarmonyPatch(typeof(PassengerFerryAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // need to execute after our other mod, Express Bus Services
    [HarmonyAfter(new string[] { PatchController.ExpressBusServicesHarmonyID })]
    public class Prefix_PassengerFerryAI_LoadPassengers
    {
        [HarmonyPrefix]
        public static bool LoadPassengersBetter(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            PassengerTrainUtility.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
            return false;
        }
    }
}
