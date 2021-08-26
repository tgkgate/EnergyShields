using System;

namespace Cython.EnergyShields
{
	
	public class Configuration
	{
		public ShieldDefinition LargeBlockSmallGenerator = new ShieldDefinition(10000f, 1f, 6.5f);
		public ShieldDefinition LargeBlockLargeGenerator = new ShieldDefinition(200000f, 20f, 130f);
		public ShieldDefinition SmallBlockSmallGenerator = new ShieldDefinition(350f, 0.1f, 0.35f);
		public ShieldDefinition SmallBlockLargeGenerator = new ShieldDefinition(10000f, 3.0f, 10.5f);

		public ModuleDefinition UpgradeModuleMultiplier = new ModuleDefinition();

		public DamageMultiplierDefinition DamageMultipliers = new DamageMultiplierDefinition();

		public GeneralSettingsDefinition GeneralSettings = new GeneralSettingsDefinition();

		public EffectDefinition Effects = new EffectDefinition(); 

		public AlternativeRechargeModeDefinition AlternativeRechargeMode = new AlternativeRechargeModeDefinition();



		public Configuration ()
		{
		}

		public Configuration (bool empty = true)
		{
			Configuration newConfiguration = new Configuration();


		}
	}
}

