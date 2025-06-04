using ColossalFramework;

namespace BetterTrainBoarding.DataTypes
{
    public class VehicleOccupancyInfo
    {
        public ushort VehicleID { get; private set; }

        public int Occupancy { get; }

        public int Capacity { get; }

        public bool VehicleIsFull => Occupancy >= Capacity;

        public VehicleOccupancyInfo(ushort vehicleID)
        {
            VehicleID = vehicleID;

            // load the relevant stats from the global table for convenience
            var vehicleManager = Singleton<VehicleManager>.instance;

            var vehicleInstance = vehicleManager.m_vehicles.m_buffer[vehicleID];
            Occupancy = vehicleInstance.m_transferSize;
            Capacity = vehicleInstance.Info.m_vehicleAI.GetPassengerCapacity(false);
        }
    }
}
