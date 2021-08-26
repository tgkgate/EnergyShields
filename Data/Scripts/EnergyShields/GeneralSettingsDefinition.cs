using System;

namespace Cython.EnergyShields
{
	public class GeneralSettingsDefinition
	{
		public bool GrindersIgnoreShields = true;
		public int StatusUpdateInterval = 60;
		public bool PercentageBasedDamageLeaking = false;
		public bool IgnoreCollision = false;

		public GeneralSettingsDefinition ()
		{
		}
	}
}

