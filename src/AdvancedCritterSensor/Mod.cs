using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;

namespace AdvancedCritterSensor
{
	public sealed class AdvancedCritterSensorMod : UserMod2
	{
		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
			PUtil.InitLibrary(false);
			ModStrings.Register();
			Debug.Log("[AdvancedCritterSensor] Loaded version " + typeof(AdvancedCritterSensorMod).Assembly.GetName().Version);
		}
	}
}
