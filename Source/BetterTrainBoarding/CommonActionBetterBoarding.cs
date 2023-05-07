using System.Collections.Generic;

namespace BetterTrainBoarding
{
    public class CommonActionBetterBoarding
    {
        public static bool HandleBetterBoarding(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
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
