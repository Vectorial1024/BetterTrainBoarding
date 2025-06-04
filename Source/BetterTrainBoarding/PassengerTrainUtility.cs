using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BetterTrainBoarding.DataTypes;
using UnityEngine;
using static Vehicle;

namespace BetterTrainBoarding
{
    public class PassengerTrainUtility
    {
        public struct CompartmentInfo
        {
            public uint vehicleId;
            public int capacity;
            public int occupied;

            public CompartmentInfo(uint vehicleId, int capacity, int occupied)
            {
                this.vehicleId = vehicleId;
                this.capacity = capacity;
                this.occupied = occupied;
            }
        }

        public struct PassengerChoice
        {
            public ushort citizenID;
            public byte waitCounter;
            public ushort vehicleID;

            public PassengerChoice(ushort citizenID, byte waitCounter, ushort vehicleID)
            {
                this.citizenID = citizenID;
                this.waitCounter = waitCounter;
                this.vehicleID = vehicleID;
            }

            public static int ComparePriority(PassengerChoice a, PassengerChoice b)
            {
                // place the one with a higher waitCounter to the front;
                // note: waitCounter is about how long the passenger has been waiting, and it increases as time goes on until it reaches 255, where the passenger gives up waiting.
                return b.waitCounter - a.waitCounter;
            }
        }

        public static bool GetClosestTrailer(ushort vehicleID, Vector3 position, out ushort trailerID)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			float distanceToClosestTrailer = 1E+10f;
			trailerID = 0;
			int trailerIterationGuard = 0;
			while (vehicleID != 0)
			{
				Frame lastFrameData = instance.m_vehicles.m_buffer[vehicleID].GetLastFrameData();
				float currentDistance = Vector3.SqrMagnitude(position - lastFrameData.m_position);
				if (currentDistance < distanceToClosestTrailer)
				{
					distanceToClosestTrailer = currentDistance;
					trailerID = vehicleID;
				}
				vehicleID = instance.m_vehicles.m_buffer[vehicleID].m_trailingVehicle;
				if (++trailerIterationGuard > 16384)
				{
					CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return trailerID != 0;
		}

        public static List<ushort> GetBoardingRankedChoice(ushort vehicleID, Vector3 position)
        {
            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            List<ushort> rankedChoice = new List<ushort>();

            // ranked choice is ordered by distance to each trailer
            // it seems that c# does not have any convenient custom sorter...

            Dictionary<ushort, float> distances = new Dictionary<ushort, float>();
            int trailerIterationGuard = 0;
            while (vehicleID != 0)
            {
                Frame lastFrameData = vehicleManager.m_vehicles.m_buffer[vehicleID].GetLastFrameData();
                float currentDistance = Vector3.SqrMagnitude(position - lastFrameData.m_position);
                distances.Add(vehicleID, currentDistance);
                vehicleID = vehicleManager.m_vehicles.m_buffer[vehicleID].m_trailingVehicle;
                if (++trailerIterationGuard > 16384)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }

            // we do a simple insert sort
            while (distances.Count > 0)
            {
                // find min
                ushort minKey = 0;
                float minDistance = 1E+10f;
                foreach (ushort key in distances.Keys)
                {
                    float distance = distances[key];
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minKey = key;
                    }
                }

                // process
                rankedChoice.Add(minKey);
                distances.Remove(minKey);
            }

            return rankedChoice;
        }

        public static bool VehicleTrailerIsFree(ushort vehicleID, out uint citizenUnit)
        {
            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            citizenUnit = vehicleManager.m_vehicles.m_buffer[vehicleID].GetNotFullCitizenUnit(CitizenUnit.Flags.Vehicle);
            return citizenUnit != 0;
        }

        public static void AnalyzeTrain(ushort vehicleID, out bool isFull, out List<CompartmentInfo> analysis)
        {
            // assuming that the list is valid
            // sourced from Vehicle::GetClosestFreeTrailer
            VehicleManager instance = Singleton<VehicleManager>.instance;
            CitizenManager cmInstance = Singleton<CitizenManager>.instance;
            // traverse the linked list of train vehicles
            int fullCapacity = 0;
            isFull = true;
            analysis = new List<CompartmentInfo>();
            int loopVehiclesCount = 0;
            while (vehicleID != 0)
            {
                Vehicle vehicleInstance = instance.m_vehicles.m_buffer[vehicleID];
                int currentContains = vehicleInstance.m_transferSize;
                int capacity = 0;
                // trick: because each citizen unit contains 5 citizen, we can ccunt the number of citizen units to calculate full capacity.
                uint nextCitizenUnit = vehicleInstance.m_citizenUnits;
                int loopCitizenCount = 0;
                while (nextCitizenUnit != 0)
                {
                    capacity++;
                    nextCitizenUnit = cmInstance.m_units.m_buffer[nextCitizenUnit].m_nextUnit;
                    if (++loopCitizenCount > 524288)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
                capacity *= 5;
                uint notFullCitizenUnit = vehicleInstance.GetNotFullCitizenUnit(CitizenUnit.Flags.Vehicle);
                if (notFullCitizenUnit != 0)
                {
                    isFull = false;
                }
                fullCapacity += capacity;
                CompartmentInfo info = new CompartmentInfo(vehicleID, capacity, currentContains);
                analysis.Add(info);
                // iterate next vehicle
                vehicleID = instance.m_vehicles.m_buffer[vehicleID].m_trailingVehicle;
                if (++loopVehiclesCount > 16384)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            // iteration complete
        }

        /**
         * May be explained with more docs/pics when I have the time to do so.
         */
        public static void SensiblyLoadPassengers(ushort vehicleID, ushort currentStop, ushort nextStop, List<CompartmentInfo> analysis)
        {
            if (currentStop == 0 || nextStop == 0)
            {
                return;
            }
            CitizenManager instance = Singleton<CitizenManager>.instance;
            VehicleManager instance2 = Singleton<VehicleManager>.instance;
            NetManager instance3 = Singleton<NetManager>.instance;
            Vector3 position = instance3.m_nodes.m_buffer[currentStop].m_position;
            Vector3 position2 = instance3.m_nodes.m_buffer[nextStop].m_position;
            instance3.m_nodes.m_buffer[currentStop].m_maxWaitTime = 0;
            int num = instance3.m_nodes.m_buffer[currentStop].m_tempCounter;
            int capXLower = Mathf.Max((int)((position.x - 64f) / 8f + 1080f), 0);
            int capZLower = Mathf.Max((int)((position.z - 64f) / 8f + 1080f), 0);
            int capXUpper = Mathf.Min((int)((position.x + 64f) / 8f + 1080f), 2159);
            int capZUpper = Mathf.Min((int)((position.z + 64f) / 8f + 1080f), 2159);

            // find out the mapping of (cim -> nearest compartment group by compartment)
            // each element in this will correspond to the analysis list by index
            List<List<ushort>> mappingList = new List<List<ushort>>();

            // new approach:
            // find out the mapping of (cim -> ranked choice of nearest compartment order by distance)
            // we iterate this many times so that we process 1st priority, and then 2nd priority, ... etc
            Dictionary<int, List<PassengerChoice>> rankedChoiceDict = new Dictionary<int, List<PassengerChoice>>();
            // but because of this, we also need to remember which CIMs have already boarded the train
            HashSet<ushort> pendingPassengers = new HashSet<ushort>();
            HashSet<ushort> boardedPassengers = new HashSet<ushort>();

            for (int i = 0; i < analysis.Count; i++)
            {
                rankedChoiceDict.Add(i, new List<PassengerChoice>());
            }

            for (int i = capZLower; i <= capZUpper; i++)
            {
                for (int j = capXLower; j <= capXUpper; j++)
                {
                    ushort citizenGridID = instance.m_citizenGrid[i * 2160 + j];
                    int num7 = 0;
                    while (citizenGridID != 0)
                    {
                        ushort nextGridInstance = instance.m_instances.m_buffer[citizenGridID].m_nextGridInstance;
                        if ((instance.m_instances.m_buffer[citizenGridID].m_flags & CitizenInstance.Flags.WaitingTransport) != 0)
                        {
                            Vector3 vector = instance.m_instances.m_buffer[citizenGridID].m_targetPos;
                            float num8 = Vector3.SqrMagnitude(vector - position);
                            if (num8 < 4096f && true)
                            {
                                CitizenInstance citizenInstance = instance.m_instances.m_buffer[citizenGridID];
                                CitizenInfo info = citizenInstance.Info;
                                if (info.m_citizenAI.TransportArriveAtSource(citizenGridID, ref instance.m_instances.m_buffer[citizenGridID], position, position2))
                                {
                                    // cim will want to board this
                                    pendingPassengers.Add(citizenGridID);
                                    byte waitCounter = citizenInstance.m_waitCounter;
                                    List<ushort> rankedChoice = GetBoardingRankedChoice(vehicleID, vector);
                                    for (int rank = 0; rank < rankedChoice.Count; rank++)
                                    {
                                        ushort pickedTrailerID = rankedChoice[rank];
                                        PassengerChoice choice = new PassengerChoice(citizenGridID, waitCounter, pickedTrailerID);
                                        rankedChoiceDict[rank].Add(choice);
                                    }
                                }
                            }
                        }
                        citizenGridID = nextGridInstance;
                        if (++num7 > 65536)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }

            // analyze the list
            /*
            Debug.Log("Printing analysis result for checking:");
            foreach (CompartmentInfo compInfoLoop in analysis)
            {
                Debug.Log("" + compInfoLoop.vehicleId + " " + compInfoLoop.capacity + " " + compInfoLoop.occupied);
            }
            */

            // begin processing the ranked choices
            foreach (int rank in rankedChoiceDict.Keys)
            {
                // can we skip this?
                if (pendingPassengers.Count == 0)
                {
                    // no one is waiting
                    break;
                }
                bool isFull;
                AnalyzeTrain(vehicleID, out isFull, out _);
                if (isFull)
                {
                    // train is full already
                    break;
                }

                // process this rank
                List<PassengerChoice> choicesInThisRank = rankedChoiceDict[rank];
                // sort it first, so that passengers who have waited too long can board first
                choicesInThisRank.Sort(PassengerChoice.ComparePriority);
                foreach (PassengerChoice choice in choicesInThisRank)
                {
                    ushort citizenID = choice.citizenID;
                    if (!pendingPassengers.Contains(citizenID))
                    {
                        // this passenger is not waiting; perhaps they already got onto the train
                        continue;
                    }
                    // this passenger is waiting; can their wish be fulfilled?
                    ushort trailerID = choice.vehicleID;
                    uint citizenUnitID;
                    if (VehicleTrailerIsFree(trailerID, out citizenUnitID))
                    {
                        // wish fulfilled.
                        CitizenInfo info = instance.m_instances.m_buffer[citizenID].Info;
                        if (info.m_citizenAI.SetCurrentVehicle(citizenID, ref instance.m_instances.m_buffer[citizenID], trailerID, citizenUnitID, position))
                        {
                            // cim can enter the vehicle
                            // Debug.Log("Accept");
                            num++;
                            instance2.m_vehicles.m_buffer[trailerID].m_transferSize++;
                            pendingPassengers.Remove(citizenID);
                        }
                    }
                }
                // everything is processed; we give it to the next iteration to check whether we need to continue.
            }

            // finalize
            instance3.m_nodes.m_buffer[currentStop].m_tempCounter = (ushort)Mathf.Min(num, 65535);
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
            var sortedPaxList = paxStatus.SortedPassengers;
            var paxCount = sortedPaxList.Count;
            var paxRankedChoice = new PassengerChoice[maxRank, paxCount];
            var currentPaxIndex = 0;
            foreach (var paxInfo in sortedPaxList)
            {
                // find nth closest vehicle
                var paxPosition = paxInfo.Position;
                // since the actual order of the free compartments list does not matter, we can use it to conveniently "sort by distance to passenger"
                var sortedVehicles = freeVehiclesList.OrderBy((item) => Vector3.Distance(paxPosition, item.Position));
                var rank = 0;
                foreach (var vehicle in sortedVehicles)
                {
                    paxRankedChoice[rank, currentPaxIndex] = new PassengerChoice(paxInfo.CitizenID, paxInfo.WaitCounter, vehicle.VehicleID);
                    ++rank;
                }
                ++currentPaxIndex;
            }

            // ranked choices ready; process them!
            var instance3 = Singleton<NetManager>.instance;
            var num = instance3.m_nodes.m_buffer[currentStop].m_tempCounter;
            ref var vehicleBuffer = ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            var citizenManager = Singleton<CitizenManager>.instance;
            var freeSpaceRemaining = trainStatus.FreeCapacity;
            for (var currentRank = 0; currentRank < maxRank; currentRank++)
            {
                for (currentPaxIndex = 0; currentPaxIndex < paxCount; currentPaxIndex++)
                {
                    var currentRankedChoice = paxRankedChoice[currentRank, currentPaxIndex];
                    // directly check whether the vehicle still has space
                    var freeCitUnitID = vehicleBuffer[vehicleID].GetNotFullCitizenUnit(CitizenUnit.Flags.Vehicle);
                    if (freeCitUnitID == 0)
                    {
                        // nope
                        continue;
                    }
                    // has space; try assigning the citizen
                    var citizenID = currentRankedChoice.citizenID;
                    var chosenVehicleID = currentRankedChoice.vehicleID;
                    var citizenInfo = citizenManager.m_instances.m_buffer[citizenID].Info;
                    if (!citizenInfo.m_citizenAI.SetCurrentVehicle(citizenID, ref citizenManager.m_instances.m_buffer[citizenID], chosenVehicleID, freeCitUnitID, paxStatus.CurrentStopPosition))
                    {
                        // somehow couldn't do it; try next
                        continue;
                    }
                    // successful assignment
                    num++;
                    vehicleBuffer[chosenVehicleID].m_transferSize++;
                    freeSpaceRemaining--;
                    if (freeSpaceRemaining <= 0)
                    {
                        // vehicle full; stop!
                        break;
                    }
                }
                if (freeSpaceRemaining <= 0)
                {
                    // vehicle full; stop!
                    break;
                }
            }

            // finalize the stuff
            instance3.m_nodes.m_buffer[currentStop].m_tempCounter = (ushort)Mathf.Min(num, 65535);
        }
    }
}
