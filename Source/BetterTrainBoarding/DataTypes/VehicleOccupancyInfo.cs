using System;
using ColossalFramework;
using UnityEngine;

namespace BetterTrainBoarding.DataTypes
{
    public class VehicleOccupancyInfo
    {
        public ushort VehicleID { get; }

        public int Occupancy { get; }

        public int ActualCapacity { get; }

        public Vector3 Position { get; }

        public bool IsFull => Occupancy >= ActualCapacity;

        public VehicleOccupancyInfo(ushort vehicleID)
        {
            VehicleID = vehicleID;

            // load the relevant stats from the global table for convenience
            var vehicleManager = Singleton<VehicleManager>.instance;
            var citizenManager = Singleton<CitizenManager>.instance;

            var vehicleInstance = vehicleManager.m_vehicles.m_buffer[vehicleID];
            Position = vehicleManager.m_vehicles.m_buffer[vehicleID].GetLastFrameData().m_position;
            Occupancy = vehicleInstance.m_transferSize;

            // iterate the list to find actual capacity
            var currentCitizenUnit = vehicleInstance.m_citizenUnits;
            var citizenUnitCount = 0;
            while (currentCitizenUnit != 0)
            {
                ++citizenUnitCount;
                currentCitizenUnit = citizenManager.m_units.m_buffer[currentCitizenUnit].m_nextUnit;
            }

            // we do this so we can catch potential edge case of not having enough citizen units, while maintaining asset-stats correctness
            var nominalCapacity = vehicleInstance.Info.m_vehicleAI.GetPassengerCapacity(false);
            ActualCapacity = Math.Min(citizenUnitCount * 5, nominalCapacity);
        }
    }
}
