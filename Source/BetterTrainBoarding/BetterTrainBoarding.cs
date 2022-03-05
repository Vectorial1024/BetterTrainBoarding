using CitiesHarmony.API;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterTrainBoarding
{
    public class BetterTrainBoarding : LoadingExtensionBase, IUserMod
    {
        public virtual string Name
        {
            get
            {
                return "Better Train Boarding";
            }
        }

        public virtual string Description
        {
            get
            {
                return "Unlock the peak efficiency of trains.";
            }
        }

        /// <summary>
        /// Executed whenever a level completes its loading process.
        /// This mod the activates and patches the game using Hramony library.
        /// </summary>
        /// <param name="mode">The loading mode.</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            /*
             * This function can still be called when loading up the asset editor,
             * so we have to check where we are right now.
             */

            switch (mode)
            {
                case LoadMode.LoadGame:
                case LoadMode.NewGame:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                    break;

                default:
                    return;
            }

            UnifyHarmonyVersions();
            PatchController.Activate();
        }

        /// <summary>
        /// Executed whenever a map is being unloaded.
        /// This mod then undoes the changes using the Harmony library.
        /// </summary>
        public override void OnLevelUnloading()
        {
            UnifyHarmonyVersions();
            PatchController.Deactivate();
        }

        private void UnifyHarmonyVersions()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                // this code will redirect our Harmony 2.x version to the authoritative version stipulated by CitiesHarmony
                // I will make it such that the game will throw hard error if Harmony is not found,
                // as per my usual software deployment style
                // the user will have to subscribe to Harmony by themselves. I am not their parent anyways.
                // so this block will have to be empty.

                // I mean, why do I even HAVE to use CitiesHarmony? I only wanted HarmonyLib, that's all.
                // CitiesHarmony is a benign malware, and here I shall explain why:
                // Benign: the benefits of CitiesHarmony can be found trivially for anyone who has worked in programming before; but:
                // Malware: CitiesHarmony can be installed and just "appear" without the user's direct consent
                // It is as if the makers of CitiesHarmony assume that everyone is dumb enough to instate the necessity of non-dependency auto-subscribe.
                // This is inhumane and disrespectful.
                // To me, auto-subscribe is acceptable only when the user explicitly wills it, or The Dependency Resolver resolves it.
                // But we literally do NOT have any dependency resolution system in CSL. Nor did CitiesHarmony provide one.
                // Such stlye of governance should be familiar to those living in difficult places and harsh times,
                // especially when the makers are publicly claiming to be victims of a gaslighting campaign (which is true context-free; the focus being context-free).
            }
        }
    }
}
