using System;
using System.IO;
using Sandbox.ModAPI;

namespace Cython.EnergyShields
{
	public class Log
	{
		private static Log INSTANCE = null;
		private TextWriter file = null;
		private string fileName = "";

		private Log() {
		}

		private static Log getInstance() {
			if (Log.INSTANCE == null) {
				Log.INSTANCE = new Log ();
			} 

			return INSTANCE;
		}

		public static bool init(string name) {

			bool output = false;

			if (getInstance ().file == null) {

				try {
					getInstance ().fileName = name;
					getInstance ().file = MyAPIGateway.Utilities.WriteFileInLocalStorage (name, typeof(Cython.EnergyShields.Log));
					output = true;
				} catch (Exception e) {
					MyAPIGateway.Utilities.ShowNotification (e.Message, 5000);
				}
			} else {
				output = true;
			}

			return output;
		}

		public static void writeLine(string text) {
			try {
				if (getInstance ().file != null) {
					getInstance ().file.WriteLine (text);
					getInstance ().file.Flush ();
				}
			} catch(Exception e) {
			}
		}

		public static void close() {
			try {
				if (getInstance ().file != null) {

					getInstance ().file.Flush ();
					getInstance ().file.Close ();
				}
			} catch(Exception e) {
			}
		}
	}
}

