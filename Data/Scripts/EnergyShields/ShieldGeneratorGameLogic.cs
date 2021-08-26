using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Common.Utils;
using VRageMath;
using VRage;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRage.Game.Entity.UseObject;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Voxels;

namespace Cython.EnergyShields
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Refinery), false, "LargeShipSmallShieldGeneratorBase", "SmallShipSmallShieldGeneratorBase", "SmallShipMicroShieldGeneratorBase", "LargeShipLargeShieldGeneratorBase")]
	public class ShieldGeneratorGameLogic : MyGameLogicComponent
	{
		enum ShieldType
		{
			LargeBlockSmall,
			LargeBlockLarge,
			SmallBlockSmall,
			SmallBlockLarge
		}

		MyObjectBuilder_EntityBase m_objectBuilder;

		MyResourceSinkComponent m_resourceSink;

		ShieldGeneratorStorageData saveFileShieldMessage = new ShieldGeneratorStorageData();

		long m_ticks = 0;
		public long m_ticksUntilRecharge = 0;

		public float m_currentShieldPoints = 0f;
		public float m_maximumShieldPoints = 0f;

		float m_rechargeMultiplier = 0f;
		float m_pointsToRecharge = 0f;
		float m_currentPowerConsumption = 0f;

		public Queue<Vector3D> LastGridPositions = new Queue<Vector3D>();

		float m_setPowerConsumption = 0f;

		ShieldType m_shieldType;

		bool m_closed = false;
		bool m_init = false;

		readonly int m_overchargeTimeout = 720;
		int m_overchargedTicks = 0;

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			m_objectBuilder = objectBuilder;
			Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
			NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

			m_shieldType = getShieldType();

			IMyCubeBlock cubeBlock = Entity as IMyCubeBlock;


			cubeBlock.AddUpgradeValue("ShieldPoints", 1f);
			cubeBlock.AddUpgradeValue("ShieldRecharge", 1f);
			cubeBlock.AddUpgradeValue("PowerConsumption", 1f);

			Sandbox.ModAPI.IMyTerminalBlock terminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;

			terminalBlock.AppendingCustomInfo += UpdateBlockInfo;

			//debug
			if (!EnergyShieldsCore.shieldGenerators.ContainsKey(Entity.EntityId))
			{
				EnergyShieldsCore.shieldGenerators.TryAdd(Entity.EntityId, this);
			}

			InitResourceSink();

			base.Init(objectBuilder);
		}

        private void InitStorage()
        {
            if(Entity.Storage == null)
            {
				Entity.Storage = new MyModStorageComponent();
			}
		}

        public void InitResourceSink()
		{
			m_resourceSink = new MyResourceSinkComponent();
			MyResourceSinkInfo info = new MyResourceSinkInfo() { ResourceTypeId = MyResourceDistributorComponent.ElectricityId, RequiredInputFunc = GetRequiredInput, MaxRequiredInput = 5 };
			m_resourceSink.Init(MyStringHash.GetOrCompute("Charging"), info, Entity as MyCubeBlock);

			IMyCubeBlock cubeBlock = Entity as IMyCubeBlock;
			cubeBlock.ResourceSink = m_resourceSink;
		}


		public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
		{
			if (copy)
			{
				return (MyObjectBuilder_EntityBase)m_objectBuilder.Clone();
			}
			else
			{
				return m_objectBuilder;
			}
		}

		public override void UpdateOnceBeforeFrame()
		{
			InitStorage();
			LoadStorage();
			SaveStorage();
		}

		public override void UpdateBeforeSimulation()
		{
			IMyCubeBlock cubeBlock = Entity as IMyCubeBlock;

			if (!m_init)
			{


				for (int i = 0; i < 5; i++)
					LastGridPositions.Enqueue(cubeBlock.CubeGrid.PositionComp.GetPosition());

				m_init = true;
			}

			CalculateMaximumShieldPoints();
			CalculateShieldPointsRecharge();

			if (LastGridPositions.Count > 4)
				LastGridPositions.Dequeue();

			LastGridPositions.Enqueue(cubeBlock.CubeGrid.PositionComp.GetPosition());

			saveFileShieldMessage.shieldPoints = m_currentShieldPoints;
		}

		public float GetRequiredInput()
		{
			return m_setPowerConsumption;
		}

		public void UpdatePrintBalanced()
		{
			if (!m_closed && Entity.InScene)
			{
				Sandbox.ModAPI.IMyTerminalBlock terminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;
				terminalBlock.RefreshCustomInfo();
				printShieldStatus();
			}
		}

		public void UpdateNetworkBalanced()
		{
			if (!m_closed && Entity.InScene)
			{

				if (MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Multiplayer.IsServer)
				{
					byte[] message = new byte[12];
					byte[] messageID = BitConverter.GetBytes(Entity.EntityId);
					byte[] messageValue = BitConverter.GetBytes(m_currentShieldPoints);

					for (int i = 0; i < 8; i++) {
						message[i] = messageID[i];
					}

					for (int i = 0; i < 4; i++) {
						message[i + 8] = messageValue[i];
					}

					MyAPIGateway.Multiplayer.SendMessageToOthers(5854, message, true);
					//Log.writeLine("<ShieldGeneratorGameLogic> Sync sent.");
				}

			}
		}

		public override void UpdateAfterSimulation()
		{
			m_ticks++;
		}

		public override void OnAddedToScene()
		{
			
		}

        private void LoadStorage()
        {
			if (!Entity.Storage.ContainsKey(ShieldGeneratorStorageData.StorageGuid))
				return;


			// Deserialize storage data from entity storage
			var data = Entity.Storage.GetValue(ShieldGeneratorStorageData.StorageGuid);
			try
			{
				var storagedata = MyAPIGateway.Utilities.SerializeFromBinary<ShieldGeneratorStorageData>(Convert.FromBase64String(data));

				// Load storage data into game logic
				m_currentShieldPoints = storagedata.shieldPoints;
			}
			catch (Exception e)
			{
				// Repair broken storage data
				SaveStorage();
			}
		}

        public override void OnRemovedFromScene()
		{
			SaveStorage();
		}

        private void SaveStorage()
        {
			if (Entity.Storage == null)
				InitStorage();

			// Load storage data from game logic data
			var storageData = new ShieldGeneratorStorageData();
			storageData.shieldPoints = m_currentShieldPoints;

			// Serialize storage data to entity storage
			var data = MyAPIGateway.Utilities.SerializeToBinary<ShieldGeneratorStorageData>(storageData);
			Entity.Storage.SetValue(ShieldGeneratorStorageData.StorageGuid, Convert.ToBase64String(data));
		}

        public override bool IsSerialized()
        {
			SaveStorage();
			return true;
        }

		public override void Close()
		{
			m_closed = true;

			Sandbox.ModAPI.IMyTerminalBlock terminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;

			terminalBlock.AppendingCustomInfo -= UpdateBlockInfo;

			if (EnergyShieldsCore.shieldGenerators.ContainsKey(Entity.EntityId)) {
				EnergyShieldsCore.shieldGenerators.Remove(Entity.EntityId);
			}
		}

		ShieldType getShieldType() 
		{

			string subtypeID = (Entity as IMyCubeBlock).BlockDefinition.SubtypeId;

			if (subtypeID == "LargeShipSmallShieldGeneratorBase")
			{
				return ShieldType.LargeBlockSmall;
			}
			else if (subtypeID == "LargeShipLargeShieldGeneratorBase")
			{
				return ShieldType.LargeBlockLarge;
			}
			if (subtypeID == "SmallShipMicroShieldGeneratorBase")
			{
				return ShieldType.SmallBlockSmall;
			}
			else if (subtypeID == "SmallShipSmallShieldGeneratorBase")
			{
				return ShieldType.SmallBlockLarge;
			}
			else
			{
				return ShieldType.LargeBlockSmall;
			}

		}

		ShieldDefinition getShieldDefinition()
		{
			ShieldType type = m_shieldType;

			if (type == ShieldType.LargeBlockSmall)
			{
				return EnergyShieldsCore.Config.LargeBlockSmallGenerator;
			}
			else if (type == ShieldType.LargeBlockLarge)
			{
				return EnergyShieldsCore.Config.LargeBlockLargeGenerator;
			}
			else if (type == ShieldType.SmallBlockSmall)
			{
				return EnergyShieldsCore.Config.SmallBlockSmallGenerator;
			}
			else if (type == ShieldType.SmallBlockLarge)
			{
				return EnergyShieldsCore.Config.SmallBlockLargeGenerator;
			}
			else
			{
				return EnergyShieldsCore.Config.LargeBlockSmallGenerator;
			}

		}

		public void UpdateBlockInfo(Sandbox.ModAPI.IMyTerminalBlock block, StringBuilder info)
		{
			try
			{
				if (block == null)
					return;

				if (info == null)
					return;

				info.Clear();

				info.AppendLine("");
				info.AppendLine("");


				IMyGridTerminalSystem tsystem = null;

				if (block.CubeGrid != null)
					tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(block.CubeGrid);

				if (tsystem != null)
				{
					List<IMyTerminalBlock> shieldsConnected = new List<IMyTerminalBlock>();

					tsystem.GetBlocksOfType<IMyRefinery>(shieldsConnected, EnergyShieldsCore.shieldFilter);

					float shipCurrentShieldPoints = 0f;
					float shipMaximumShieldPoints = 0f;

					foreach (var shield in shieldsConnected)
					{
						if (shield == null)
							continue;

						ShieldGeneratorGameLogic generatorLogic = null;

						if (EnergyShieldsCore.shieldGenerators.TryGetValue(shield.EntityId, out generatorLogic))
						{
							shipCurrentShieldPoints += generatorLogic.m_currentShieldPoints;
							shipMaximumShieldPoints += generatorLogic.m_maximumShieldPoints;
						}

					}

					info.Append("Ship Shield: ");
					MyValueFormatter.AppendGenericInBestUnit(shipCurrentShieldPoints, info);
					info.Append("Pt/");
					MyValueFormatter.AppendGenericInBestUnit(shipMaximumShieldPoints, info);
					info.Append("Pt\n");


				}

				info.Append("Local Shield: ");
				MyValueFormatter.AppendGenericInBestUnit(m_currentShieldPoints, info);
				info.Append("Pt/");
				MyValueFormatter.AppendGenericInBestUnit(m_maximumShieldPoints, info);
				info.Append("Pt\n");

				info.Append("Recharge: ");
				MyValueFormatter.AppendGenericInBestUnit(m_pointsToRecharge * 60, info);
				info.Append("Pt/s ");
				if (EnergyShieldsCore.Config.AlternativeRechargeMode.Enable && (m_ticksUntilRecharge > 0))
				{
					info.Append("(" + (int)Math.Ceiling(m_ticksUntilRecharge / 60d) + "s)\n");
				}
				else
				{
					info.Append("\n");
				}

				info.Append("Effectivity: ");
				MyValueFormatter.AppendWorkInBestUnit(m_currentPowerConsumption, info);
				info.Append("/");
				MyValueFormatter.AppendWorkInBestUnit(m_setPowerConsumption, info);
				info.Append(" (" + (m_rechargeMultiplier * 100).ToString("N") + "%)");
			}
			catch (Exception e)
			{ }
		}

		private void printShieldStatus() {
			try
			{
				if (MyAPIGateway.Multiplayer.IsServer)
				{
					if (Entity.InScene)
					{
						Sandbox.ModAPI.IMyFunctionalBlock funcBlock = Entity as Sandbox.ModAPI.IMyFunctionalBlock;

						if (funcBlock.CustomName.Contains(":"))
						{
							String name = funcBlock.CustomName;
							long gridID = funcBlock.CubeGrid.EntityId;
							int index = name.IndexOf(':');

							if ((index + 1) < name.Length)
							{
								name = name.Remove(index + 1);
							}

							IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(funcBlock.CubeGrid as IMyCubeGrid);
							List<IMyTerminalBlock> shieldsConnected = new List<IMyTerminalBlock>();

							if (tsystem != null)
							{
								tsystem.GetBlocksOfType<IMyRefinery>(shieldsConnected, EnergyShieldsCore.shieldFilter);

								float shipCurrentShieldPoints = 0f;
								float shipMaximumShieldPoints = 0f;

								foreach (var shield in shieldsConnected)
								{
									ShieldGeneratorGameLogic generatorLogic = ((IMyTerminalBlock)shield).GameLogic.GetAs<ShieldGeneratorGameLogic>();

									shipCurrentShieldPoints += generatorLogic.m_currentShieldPoints;
									shipMaximumShieldPoints += generatorLogic.m_maximumShieldPoints;
								}

								name = name + " (" + Math.Round(shipCurrentShieldPoints) + "/" +
									Math.Round(shipMaximumShieldPoints) + ")";
								funcBlock.SetCustomName(name);
							}
							else
							{
								name = name + " (" + Math.Round(m_currentShieldPoints) + "/" +
									Math.Round(m_maximumShieldPoints) + ")";
								funcBlock.SetCustomName(name);
							}
						}
					}

				}
			}
			catch (Exception e)
			{ }
		}

		void CalculateMaximumShieldPoints()
		{
			IMyCubeBlock cubeBlock = Entity as IMyCubeBlock;

			m_maximumShieldPoints = getShieldDefinition().Points * cubeBlock.UpgradeValues["ShieldPoints"] * EnergyShieldsCore.Config.UpgradeModuleMultiplier.PointMultiplier;
		}

		void CalculateShieldPointsRecharge()
		{
			IMyFunctionalBlock functionalBlock = (Entity as Sandbox.ModAPI.IMyFunctionalBlock);
			bool recharge = true;
			float rechargeMultiplier = 0;


			if (!functionalBlock.IsFunctional)
			{
				m_currentShieldPoints = 0f;
			}

			// Update power multipliers
			if (functionalBlock.IsFunctional
				&& functionalBlock.Enabled
				&& (m_currentShieldPoints < m_maximumShieldPoints))
			{
				float powerMultiplier;

				if (!EnergyShieldsCore.Config.AlternativeRechargeMode.Enable)
				{
					float multiplier = (m_currentShieldPoints / m_maximumShieldPoints);

					if (multiplier > 0.99999f)
					{
						multiplier = 0.00001f;
					}
					else if ((multiplier > 0.9f) || (multiplier < 0.08f))
					{
						multiplier = 0.1f;
					}
					else if ((multiplier > 0.7f) || (multiplier < 0.12f))
					{
						multiplier = 0.2f;
					}
					else if ((multiplier > 0.5f) || (multiplier < 0.18f))
					{
						multiplier = 0.4f;
					}
					else if ((multiplier > 0.35f) || (multiplier < 0.22f))
					{
						multiplier = 0.7f;
					}
					else
					{
						multiplier = 1.0f;
					}

					powerMultiplier = multiplier;
					rechargeMultiplier = multiplier;
				}
				else
				{
					powerMultiplier = 1;
					rechargeMultiplier = EnergyShieldsCore.Config.AlternativeRechargeMode.RechargeMultiplier;
				}

				m_setPowerConsumption = getShieldDefinition().PowerConsumption * functionalBlock.UpgradeValues["PowerConsumption"] * EnergyShieldsCore.Config.UpgradeModuleMultiplier.PowerMultiplier * powerMultiplier;
			}
			else
			{
				m_setPowerConsumption = 0f;
				recharge = false;
			}

			// Update power consumption
			if (m_setPowerConsumption != m_resourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId))
			{
				m_resourceSink.Update();
			}
			m_currentPowerConsumption = m_resourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);

			// Update recharge
			if (recharge)
			{
				m_rechargeMultiplier = m_currentPowerConsumption / m_setPowerConsumption;

				if (EnergyShieldsCore.Config.AlternativeRechargeMode.Enable && m_ticksUntilRecharge > 0)
				{
					m_ticksUntilRecharge--;
					m_pointsToRecharge = 0;
				}
				else
				{
					m_pointsToRecharge = getShieldDefinition().RechargeAtPeak * functionalBlock.UpgradeValues["ShieldRecharge"]
						* EnergyShieldsCore.Config.UpgradeModuleMultiplier.RechargeMultiplier * m_rechargeMultiplier * rechargeMultiplier;
				}

				if (m_pointsToRecharge > 0)
				{
					m_currentShieldPoints = Math.Min(m_currentShieldPoints + m_pointsToRecharge, m_maximumShieldPoints);
				}
			}
			else
			{
				m_pointsToRecharge = 0;
			}


			// Truncate current points to maximum
			if (m_maximumShieldPoints < m_currentShieldPoints)
			{
				if (m_overchargedTicks >= m_overchargeTimeout)
				{
					m_currentShieldPoints = m_maximumShieldPoints;
				}
				m_overchargedTicks++;
			}
			else
			{
				m_overchargedTicks = 0;
			}
		}
	}
}

