using HarmonyLib;
using System.Reflection;

namespace BetterTrainBoarding
{
    internal class PatchController
    {
        public static string HarmonyModID => "com.vectorial1024.cities.btb";

        /*
         * The "singleton" design is pretty straight-forward.
         */

        private static Harmony harmony;

        public static Harmony GetHarmonyInstance()
        {
            if (harmony == null)
            {
                harmony = new Harmony(HarmonyModID);
            }

            return harmony;
        }

        public static void Activate()
        {
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
        }

        public const string ExpressBusServicesHarmonyID = "com.vectorial1024.cities.ebs";
    }
}
