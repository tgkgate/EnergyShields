using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;

namespace Cython.EnergyShields
{
	public class NetworkLoadBalancer
	{
		int m_periodLength = 0;

		int m_progress = 0;

		float m_progressPerTick = 0;

		int m_processedCount = 0;

		int m_initialSize = 0;

		Queue<ShieldGeneratorGameLogic> m_notProcessed = new Queue<ShieldGeneratorGameLogic>();

		public NetworkLoadBalancer ()
		{

		}

		public void Update()
		{
			if(!(m_progress < m_periodLength))
			{
				m_periodLength = 100;

				m_progress = 0;

				m_notProcessed.Clear();

				foreach(var kv in EnergyShieldsCore.shieldGenerators)
				{
					m_notProcessed.Enqueue(kv.Value);
				}

				m_initialSize = m_notProcessed.Count;

				m_processedCount = 0;

				m_progressPerTick = m_periodLength / ((float) m_notProcessed.Count);
			}

			m_progress += 1;

			int toProcess = ((int) (((float)m_progress/m_periodLength) * m_initialSize)) - m_processedCount;

			for(int i = 0; i < toProcess; i++)
			{
				if(m_notProcessed.Count > 0)
				{
					var generator = m_notProcessed.Dequeue();

					generator.UpdateNetworkBalanced();
				}
			}

			m_processedCount += toProcess;
		}

	}
}

