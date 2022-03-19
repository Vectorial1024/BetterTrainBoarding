using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            public uint citizenID;
            public uint vehicleID;

            public PassengerChoice(uint citizenID, uint vehicleID)
            {
                this.citizenID = citizenID;
                this.vehicleID = vehicleID;
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
            foreach (CompartmentInfo info in analysis)
            {
                mappingList.Add(new List<ushort>());
            }
            Dictionary<uint, int> reverseCache = new Dictionary<uint, int>();
            for (int i = 0; i < analysis.Count; i++)
            {
                reverseCache.Add(analysis[i].vehicleId, i);
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
                                CitizenInfo info = instance.m_instances.m_buffer[citizenGridID].Info;
                                if (info.m_citizenAI.TransportArriveAtSource(citizenGridID, ref instance.m_instances.m_buffer[citizenGridID], position, position2))
                                {
                                    // cim will want to board this
                                    if (PassengerTrainUtility.GetClosestTrailer(vehicleID, vector, out ushort trailerID))
                                    {
                                        // I assert that this exists
                                        mappingList[reverseCache[trailerID]].Add(citizenGridID);
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

            // all citizens are now assigned to their own closest compartment.
            // now, let them board the train!
            int currentCompartmentDistance = 0;
            while (true)
            {
                // what mappings to try?
                // fr safety, we will NOT do Tuples
                List<List<int>> combosToTry = new List<List<int>>();
                if (currentCompartmentDistance == 0)
                {
                    for (int i = 0; i < mappingList.Count; i++)
                    {
                        combosToTry.Add(new List<int>() { i, i });
                    }
                }
                else
                {
                    for (int i = 0; i < mappingList.Count; i++)
                    {
                        int platformSegment = i - currentCompartmentDistance;
                        if (platformSegment >= 0 && platformSegment < mappingList.Count)
                        {
                            combosToTry.Add(new List<int>() { i, platformSegment });
                        }
                        platformSegment = i + currentCompartmentDistance;
                        if (platformSegment >= 0 && platformSegment < mappingList.Count)
                        {
                            combosToTry.Add(new List<int>() { i, platformSegment });
                        }
                    }
                }
                foreach (List<int> combo in combosToTry)
                {
                    int trailerIndex = combo[0];
                    int platformSegmentIndex = combo[1];
                    CompartmentInfo compInfo = analysis[trailerIndex];
                    // Debug.Log("Trailer against Platform " + trailerIndex + " " + platformSegmentIndex);

                    // for each such combination...
                    // try to put the cims into said compartment
                    // we load the citizen at index 0 into the trailers until we cannot
                    int nextCitizenIndex = 0;
                    while (true)
                    {
                        if (mappingList[platformSegmentIndex].Count == 0)
                        {
                            // until we cannot: no cims remaining
                            // Debug.Log("No passenger; break");
                            break;
                        }
                        if (nextCitizenIndex >= mappingList[platformSegmentIndex].Count)
                        {
                            // until we cannot: cannot iterate (explained below)
                            // Debug.Log("No passenger (uniterable); break");
                            break;
                        }
                        ushort citizenID = mappingList[platformSegmentIndex][nextCitizenIndex];
                        CitizenInfo info = instance.m_instances.m_buffer[citizenID].Info;
                        Vector3 cimPos = instance.m_instances.m_buffer[citizenID].m_targetPos;
                        if (Vehicle.GetClosestFreeTrailer(vehicleID, cimPos, out ushort trailerID, out uint unitID))
                        {
                            if (trailerID != compInfo.vehicleId)
                            {
                                // until we cannot: the trailer is full.

                                // but hold it! supposedly this aims to check "trailer is full (have space elsewhere)"
                                // however for distance > 0, it does not guarantee that the iteartion is "convergent".
                                // this effect is seen more obviously for the center-most compartment:
                                // when we want to check whether passengers can go to the front most compartment, we may iterate to a passenger whose closest compartment is at the back-most
                                // and vice versa; this produces a zig-zag iteration pattern
                                // and hence results in lower-than-optimal loading for the affected metro-train.
                                // Debug.Log("Trailer full (have space on other trailers); seek next");
                                nextCitizenIndex++;
                                continue;
                            }
                            if (info.m_citizenAI.SetCurrentVehicle(citizenID, ref instance.m_instances.m_buffer[citizenID], trailerID, unitID, position))
                            {
                                // cim can enter the vehicle
                                // Debug.Log("Accept");
                                num++;
                                instance2.m_vehicles.m_buffer[trailerID].m_transferSize++;
                                mappingList[platformSegmentIndex].RemoveAt(nextCitizenIndex);
                            }
                        }
                        else
                        {
                            // 
                            // Debug.Log("Trailer full (no space in train); break;");
                            break;
                        }
                    }

                    // now, all cims are inserted into the compartments according to the "mapping distance" that we declared
                }
                // can we continue with more mapping?

                // 1. is the train full? train full = stop now.
                List<CompartmentInfo> innerAnalysis;
                bool isFull;
                AnalyzeTrain(vehicleID, out isFull, out innerAnalysis);
                if (isFull)
                {
                    break;
                }
                // train is not yet full
                // 2. do we have anyone left waiting? no one waiting = stop now
                bool hasCim = false;
                foreach (List<ushort> waitList in mappingList)
                {
                    if (waitList.Count > 0)
                    {
                        hasCim = true;
                        break;
                    }
                }
                if (!hasCim)
                {
                    break;
                }
                // 3. have we exhausted all combinations?
                if (currentCompartmentDistance >= mappingList.Count)
                {
                    break;
                }
                // more to try; move on to the next distance!
                currentCompartmentDistance++;
            }

            // finalize
            instance3.m_nodes.m_buffer[currentStop].m_tempCounter = (ushort)Mathf.Min(num, 65535);
        }
    }
}
