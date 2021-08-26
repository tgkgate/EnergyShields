using System;

namespace Cython.EnergyShields
{
	public enum ConfigType
	{
		None,
		LargeBlockSmallShield,
		LargeBlockLargeShield,
		SmallBlockSmallShield,
		SmallBlockLargeShield,
		Module,
		Damage,
		General,
		Effects,
		Alternative
	}
	
	public class ConfigurationMessage
	{
		public byte[] config;

		public ConfigType type = ConfigType.None;

		public ulong sender;
		
		public ConfigurationMessage ()
		{
		}
	}
}

