using HarmonyLib;
using PeterHan.PLib.UI;

namespace AdvancedCritterSensor
{
	public static class Patches
	{
		// Build menu: Automation > Sensors, right after the vanilla Critter Sensor.
		[HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
		public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
		{
			public static void Prefix()
			{
				ModUtil.AddBuildingToPlanScreen("Automation", AdvancedCritterSensorConfig.ID, "sensors",
					LogicCritterCountSensorConfig.ID, ModUtil.BuildingOrdering.After);
			}
		}

		// Research: Multiplexing (tier 6, the ribbon-heavy automation node), two tiers past
		// the vanilla sensor's Animal Control.
		[HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
		public static class Db_Initialize_Patch
		{
			public const string TechId = "Multiplexing";

			public static void Postfix()
			{
				Tech tech = Db.Get().Techs.TryGet(TechId);
				if (tech == null)
				{
					Debug.LogWarning("[AdvancedCritterSensor] Tech '" + TechId + "' not found; building will be unlocked from the start");
					return;
				}
				if (!tech.unlockedItemIDs.Contains(AdvancedCritterSensorConfig.ID))
					tech.unlockedItemIDs.Add(AdvancedCritterSensorConfig.ID);
			}
		}

		[HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
		public static class DetailsScreen_OnPrefabInit_Patch
		{
			public static void Postfix()
			{
				PUIUtils.AddSideScreenContent<AdvancedCritterSensorSideScreen>();
			}
		}
	}
}
