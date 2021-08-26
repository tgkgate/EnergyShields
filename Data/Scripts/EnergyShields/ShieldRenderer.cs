using System;
using VRage.Game.ModAPI;
using Sandbox.Game.Entities;
using VRage.ModAPI;
using Sandbox.ModAPI;
using VRageMath;
using VRage.Game;
using VRage.Game.Entity;
using Sandbox.Definitions;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace Cython.EnergyShields
{
	public enum ShieldRenderMode
	{
		Normal
	}

	public enum ShieldColor
	{
		BlueToRed
	}

	public class ShieldRenderer
	{
		public int m_timeToLive = 8;

		// sound subtype names
		readonly string soundNameNormal = "ShieldHitNormal";

		// effect blocks subtype names
		readonly static MyStringId effectMaterialBlue = MyStringId.GetOrCompute("ShieldBlue");
		readonly static MyStringId effectMaterialMagenta = MyStringId.GetOrCompute("ShieldMagenta");
		readonly static MyStringId effectMaterialRed = MyStringId.GetOrCompute("ShieldRed");
		readonly static MyStringId effectMaterial = MyStringId.GetOrCompute("Shield");

		Color m_shieldBlue = new Color(0,153,255);

		IMySlimBlock m_block;

		MatrixD m_matrix;
		Vector3D m_shieldScale;

		float m_shieldStatus;

		MyEntity3DSoundEmitter m_soundEmitter;

		public ShieldRenderer (IMySlimBlock block, ShieldRenderMode mode, ShieldColor color, float shieldStatus, bool deformation)
		{
			m_block = block;
			m_shieldStatus = shieldStatus;

			MyCubeBlockDefinition blockDefinition = block.BlockDefinition as MyCubeBlockDefinition;


			var blockSize = blockDefinition.Size;
			if(blockDefinition.CubeSize == MyCubeSize.Large)
			{

				m_shieldScale.X = blockDefinition.Size.X + 0.1;
				m_shieldScale.Y = blockDefinition.Size.Y + 0.1;
				m_shieldScale.Z = blockDefinition.Size.Z + 0.1;
			}
			else
			{
				m_shieldScale.X = (blockDefinition.Size.X + 0.1) * 0.2d;
				m_shieldScale.Y = (blockDefinition.Size.Y + 0.1) * 0.2d;
				m_shieldScale.Z = (blockDefinition.Size.Z + 0.1) * 0.2d;
			}

			Vector3D shieldPosition;

			if(block.FatBlock == null) 
			{
				shieldPosition = block.CubeGrid.GridIntegerToWorld(block.Position);

				m_matrix = MatrixD.CreateFromTransformScale(Quaternion.CreateFromRotationMatrix(block.CubeGrid.WorldMatrix.GetOrientation()), shieldPosition, m_shieldScale);

				if(EnergyShieldsCore.Config.Effects.PlaySound)
				{
					m_soundEmitter = new MyEntity3DSoundEmitter((MyEntity)block.CubeGrid);
				}
			}
			else
			{
				shieldPosition = block.FatBlock.WorldMatrix.Translation;

				m_matrix = MatrixD.CreateFromTransformScale(Quaternion.CreateFromRotationMatrix(block.FatBlock.WorldMatrix.GetOrientation()), shieldPosition, m_shieldScale);

				if(EnergyShieldsCore.Config.Effects.PlaySound)
				{
					m_soundEmitter = new MyEntity3DSoundEmitter((MyEntity)block.FatBlock);
				}

			}

			if(EnergyShieldsCore.Config.Effects.PlaySound)
			{
				m_soundEmitter.SetPosition(shieldPosition);

				if(deformation)
				{
					m_soundEmitter.CustomVolume = 0.05f;
				}

				m_soundEmitter.PlaySound(new MySoundPair(soundNameNormal + "High"));
			}
		}

		public void update()
		{
			m_timeToLive--;

			if(!m_block.CubeGrid.Closed)
			{
				if(EnergyShieldsCore.Config.Effects.ShowEffects)
				{
					Color color = Color.White;

					float ttlPercent = m_timeToLive/8f;

					if((ttlPercent < 0.4) || (ttlPercent > 0.7)) 
					{

						BoundingBoxD renderBox = new BoundingBoxD(
							new Vector3D(-1.25d),
							new Vector3D(1.25d)
						);

						if(m_shieldStatus < 0.2)
						{
							color = Color.Red;
							MySimpleObjectDraw.DrawTransparentBox(ref m_matrix, ref renderBox, ref color, MySimpleObjectRasterizer.Solid, 0, 1f, effectMaterial, null, true);
						}
						else if(m_shieldStatus < 0.6)
						{
							color = Color.OrangeRed;
							MySimpleObjectDraw.DrawTransparentBox(ref m_matrix, ref renderBox, ref color, MySimpleObjectRasterizer.Solid, 0, 1f, effectMaterial, null, true);
						}
						else
						{
							MySimpleObjectDraw.DrawTransparentBox(ref m_matrix, ref renderBox, ref m_shieldBlue, MySimpleObjectRasterizer.Solid, 0, 1f, effectMaterial, null, true);

                        }
					}
				}	
			}
			else
			{
				m_timeToLive = 0;
			}
		}

		public void close()
		{
			
		}

		private IMyEntity generateShieldEffect(string name)
		{
			
			EnergyShieldsCore.shieldEffectLargeObjectBuilder.CubeBlocks[0].SubtypeName = name;

			MyAPIGateway.Entities.RemapObjectBuilder(EnergyShieldsCore.shieldEffectLargeObjectBuilder);
			var ent = MyAPIGateway.Entities.CreateFromObjectBuilder(EnergyShieldsCore.shieldEffectLargeObjectBuilder);
			ent.Flags &= ~EntityFlags.Sync;
			ent.Flags &= ~EntityFlags.Save;

			MyAPIGateway.Entities.AddEntity(ent, true);
			return ent;
		}


	}
}

