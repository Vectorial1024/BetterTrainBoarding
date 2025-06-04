using ColossalFramework;

namespace BetterTrainBoarding.DataTypes
{
    public class VehicleOccupancyInfo
    {
        public ushort VehicleID { get; private set; }

        public int Occupancy { get; private set; }

        public int Capacity { get; private set; }

        public VehicleOccupancyInfo(ushort vehicleID)
        {
            VehicleID = vehicleID;

            // load the relevant stats from the global table for convenience
            var vehicleManager = Singleton<VehicleManager>.instance;
            var citizenManager = Singleton<CitizenManager>.instance;

            var vehicleInfo = vehicleManager.m_vehicles.m_buffer[vehicleID];
            Occupancy = vehicleInfo.m_transferSize;

            // iterate the list to find capacity
            var currentCitizenUnit = vehicleInfo.m_citizenUnits;
            var citizenUnitCount = 0;
            while (currentCitizenUnit != 0)
            {
                ++citizenUnitCount;
                currentCitizenUnit = citizenManager.m_units.m_buffer[currentCitizenUnit].m_nextUnit;
            }

            Capacity = citizenUnitCount * 5;
        }
    }
}
