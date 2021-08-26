using System;

namespace Cython.EnergyShields
{
	public class ShieldMessage
	{
		public long entityID;
		public float value;

		public ShieldMessage (long entityID, float value)
		{
			this.entityID = entityID;
			this.value = value;
		}
		
		public ShieldMessage ()
		{
		}
	}
}

