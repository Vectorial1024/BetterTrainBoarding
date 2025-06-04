using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace BetterTrainBoarding.DataTypes
{
    public class PassengerWaitingInfo
    {
        public ushort CurrentStopID { get; private set; }

        public Vector3 CurrentStopPosition { get; private set; }

        public ushort NextStopID { get; private set; }

        public Vector3 NextStopPosition { get; private set; }

        private Dictionary<ushort, Vector3> _waitingPaxDict = new Dictionary<ushort, Vector3>();

        public PassengerWaitingInfo(ushort currentStopID, ushort nextStopID)
        {
            var networkManager = Singleton<NetManager>.instance;

            CurrentStopID = currentStopID;
            CurrentStopPosition = networkManager.m_nodes.m_buffer[currentStopID].m_position;
            NextStopID = nextStopID;
            NextStopPosition = networkManager.m_nodes.m_buffer[nextStopID].m_position;

            FindWaitingPassengers();
        }

        private void FindWaitingPassengers()
        {
            // stop position
            var position = CurrentStopPosition;
            int capXLower = Mathf.Max((int)((position.x - 64f) / 8f + 1080f), 0);
            int capZLower = Mathf.Max((int)((position.z - 64f) / 8f + 1080f), 0);
            int capXUpper = Mathf.Min((int)((position.x + 64f) / 8f + 1080f), 2159);
            int capZUpper = Mathf.Min((int)((position.z + 64f) / 8f + 1080f), 2159);

            // find citizens waiting at the stop
            var citizenManager = Singleton<CitizenManager>.instance;
            for (var i = capZLower; i <= capZUpper; i++)
            {
                for (var j = capXLower; j <= capXUpper; j++)
                {
                    ushort currentCitizenID = citizenManager.m_citizenGrid[i * 2160 + j];
                    int num7 = 0;
                    while (currentCitizenID != 0)
                    {
                        ref var currentCitizenInstance = ref citizenManager.m_instances.m_buffer[currentCitizenID];
                        ushort nextGridInstance = currentCitizenInstance.m_nextGridInstance;
                        if ((currentCitizenInstance.m_flags & CitizenInstance.Flags.WaitingTransport) != 0)
                        {
                            Vector3 vector = currentCitizenInstance.m_targetPos;
                            if (Vector3.SqrMagnitude(vector - position) < 4096f)
                            {
                                // within range; will this citizen ever board the vehicle?
                                var citizenInfo = currentCitizenInstance.Info;
                                if (citizenInfo.m_citizenAI.TransportArriveAtSource(currentCitizenID, ref currentCitizenInstance, CurrentStopPosition, NextStopPosition))
                                {
                                    // will board; remember this citizen for later!
                                    _waitingPaxDict.Add(currentCitizenID, vector);
                                }
                            }
                        }
                        currentCitizenID = nextGridInstance;
                    }
                }
            }
        }
    }
}
