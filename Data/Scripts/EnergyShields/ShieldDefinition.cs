using System;

namespace Cython.EnergyShields
{
	public class ShieldDefinition
	{
		public float Points = 10000f;
		public float RechargeAtPeak = 1f;
		public float PowerConsumption = 6.5f;

		public ShieldDefinition (float points, float rechargeAtPeak, float powerConsumption)
		{
			Points = points;
			RechargeAtPeak = rechargeAtPeak;
			PowerConsumption = powerConsumption;
		}

		public ShieldDefinition ()
		{
		}
	}
}

