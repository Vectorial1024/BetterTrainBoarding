using ColossalFramework;
using System.Collections.Generic;
using System.Linq;
using BetterTrainBoarding.DataTypes;
using UnityEngine;

namespace BetterTrainBoarding
{
    public static class PassengerTrainUtility
    {
        public struct PassengerChoice
        {
            public readonly ushort CitizenID;
            public readonly ushort VehicleID;

            public PassengerChoice(ushort citizenID, ushort vehicleID)
            {
                CitizenID = citizenID;
                VehicleID = vehicleID;
            }
        }

        public static void HandleBetterBoarding(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            // the one-stop replacement to LoadPassengers
            if (currentStop == 0 || nextStop == 0)
            {
                return;
            }

            var trainStatus = new TrainOccupancyInfo(vehicleID);
            var paxStatus = new PassengerWaitingInfo(currentStop, nextStop);
            /*
             * generate matches here! the flow is:
             * each passenger indicates their vehicle choice, closest vehicle first
             * then, process the first choice;
             * then, process the second choice;
             * ...
             * do this until one of:
             * - all are boarded
             * - train is full
             * best is if we can somehow know this as soon as possible to stop unnecessary iteration
             */

            // prepare ranked choices
            var freeVehiclesList = trainStatus.FreeCompartments;
            var maxRank = freeVehiclesList.Count;
            if (maxRank == 0)
            {
                // no free vehicles; simply stop
                return;
            }
            var sortedPaxList = paxStatus.SortedPassengers;
            var paxCount = sortedPaxList.Count;
            var freeCapacity = trainStatus.FreeCapacity;
            if (maxRank == 1 && paxCount > freeCapacity)
            {
                // optimization: if there is only 1 possible vehicle, and there are too many passengers
                // then we can simply look at the first k passengers, where k = free space remaining
                sortedPaxList = sortedPaxList.GetRange(0, freeCapacity);
            }
            var paxRankedChoice = new PassengerChoice[maxRank, paxCount];
            var currentPaxIndex = 0;
            // var debugString = new StringBuilder();
            foreach (var paxInfo in sortedPaxList)
            {
                // find nth closest vehicle
                var paxPosition = paxInfo.Position;
                // since the actual order of the free compartments list does not matter, we can use it to conveniently "sort by distance to passenger"
                var sortedVehicles = freeVehiclesList.OrderBy((item) => Vector3.Distance(paxPosition, item.Position));
                var rank = 0;
                foreach (var vehicle in sortedVehicles)
                {
                    paxRankedChoice[rank, currentPaxIndex] = new PassengerChoice(paxInfo.CitizenID, vehicle.VehicleID);
                    // debugString.AppendLine($"Pax {paxInfo.CitizenID} rank {rank} picks vehicle {vehicle.VehicleID}");
                    ++rank;
                }
                ++currentPaxIndex;
            }
            // Debug.LogError(debugString.ToString());

            // ranked choices ready; process them!
            var instance3 = Singleton<NetManager>.instance;
            var num = instance3.m_nodes.m_buffer[currentStop].m_tempCounter;
            ProcessRankedChoices(paxRankedChoice, paxStatus.CurrentStopPosition, freeCapacity, ref num);

            // finalize the stuff
            instance3.m_nodes.m_buffer[currentStop].m_tempCounter = (ushort)Mathf.Min(num, 65535);
        }

        private static void ProcessRankedChoices(PassengerChoice[,] paxRankedChoice, Vector3 stopPosition, int freeSpaceRemaining, ref ushort serviceCount)
        {
            ref var vehicleBuffer = ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            var citizenManager = Singleton<CitizenManager>.instance;
            var maxRank = paxRankedChoice.GetLength(0);
            var paxCount = paxRankedChoice.GetLength(1);
            var boardedPaxIDs = new HashSet<ushort>();
            for (var currentRank = 0; currentRank < maxRank; currentRank++)
            {
                for (var currentPaxIndex = 0; currentPaxIndex < paxCount; currentPaxIndex++)
                {
                    var currentRankedChoice = paxRankedChoice[currentRank, currentPaxIndex];
                    // check whether we need to do this
                    var citizenID = currentRankedChoice.CitizenID;
                    if (boardedPaxIDs.Contains(citizenID))
                    {
                        // already boarded; skip
                        continue;
                    }
                    // directly check whether the vehicle still has space
                    var chosenVehicleID = currentRankedChoice.VehicleID;
                    var freeCitUnitID = vehicleBuffer[chosenVehicleID].GetNotFullCitizenUnit(CitizenUnit.Flags.Vehicle);
                    if (freeCitUnitID == 0)
                    {
                        // nope
                        continue;
                    }
                    // has space; try assigning the citizen
                    ref var citizenInstance = ref citizenManager.m_instances.m_buffer[citizenID];
                    var citizenInfo = citizenInstance.Info;
                    if (!citizenInfo.m_citizenAI.SetCurrentVehicle(citizenID, ref citizenInstance, chosenVehicleID, freeCitUnitID, stopPosition))
                    {
                        // somehow couldn't do it; try next
                        continue;
                    }
                    // successful assignment
                    serviceCount++;
                    vehicleBuffer[chosenVehicleID].m_transferSize++;
                    boardedPaxIDs.Add(citizenID);
                    freeSpaceRemaining--;
                    if (freeSpaceRemaining <= 0)
                    {
                        // vehicle full; stop!
                        return;
                    }
                }
            }
        }
    }
}
