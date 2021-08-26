using ProtoBuf;
using System;
using System.ComponentModel;

namespace Cython.EnergyShields
{
	[ProtoContract]
	public class ShieldGeneratorStorageData
	{
		public static readonly Guid StorageGuid = new Guid("20C6C85C-FCAC-48EB-88C2-A432BAD5BDDB");

		[ProtoMember(1), DefaultValue(0f)]
		public float shieldPoints = 0f;

		public ShieldGeneratorStorageData (float value)
		{
			this.shieldPoints = value;
		}
		
		public ShieldGeneratorStorageData ()
		{
		}
	}
}

