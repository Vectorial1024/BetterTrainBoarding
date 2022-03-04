using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static Vehicle;

namespace BetterTrainBoarding
{
    internal class PassengerTrainUtility
    {
		public static bool GetClosestTrailer(ushort vehicleID, Vector3 position, out ushort trailerID)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			float num = 1E+10f;
			trailerID = 0;
			int num2 = 0;
			while (vehicleID != 0)
			{
				Frame lastFrameData = instance.m_vehicles.m_buffer[vehicleID].GetLastFrameData();
				float num3 = Vector3.SqrMagnitude(position - lastFrameData.m_position);
				if (num3 < num)
				{
					num = num3;
					trailerID = vehicleID;
				}
				vehicleID = instance.m_vehicles.m_buffer[vehicleID].m_trailingVehicle;
				if (++num2 > 16384)
				{
					CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return trailerID != 0;
		}
	}
}
