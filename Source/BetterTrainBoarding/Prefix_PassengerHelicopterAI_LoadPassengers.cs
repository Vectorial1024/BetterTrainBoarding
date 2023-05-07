using HarmonyLib;

namespace BetterTrainBoarding
{
    [HarmonyPatch(typeof(PassengerHelicopterAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // need to execute after our other mod, Express Bus Services
    [HarmonyAfter(new string[] { PatchController.ExpressBusServicesHarmonyID })]
    public class Prefix_PassengerHelicopterAI_LoadPassengers
    {
        [HarmonyPrefix]
        public static bool PreFix(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            return CommonActionBetterBoarding.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
        }
    }
}
