using STRINGS;

namespace AdvancedCritterSensor
{
	public static class ModStrings
	{
		private const string PrefabKey = "STRINGS.BUILDINGS.PREFABS.ADVANCEDCRITTERSENSOR.";

		private static readonly string Green = UI.FormatAsAutomationState("Green Signal", UI.AutomationState.Active);
		private static readonly string Red = UI.FormatAsAutomationState("Red Signal", UI.AutomationState.Standby);
		private static readonly string Ribbon = UI.FormatAsLink("Automation Ribbon", "LOGICRIBBON");

		public static readonly string Name = UI.FormatAsLink("Advanced Critter Sensor", "ADVANCEDCRITTERSENSOR");
		public const string Desc = "Tracking exactly which critters and eggs share a room allows for much finer control over automated ranching.";
		public static readonly string Effect =
			"Counts the selected " + UI.FormatAsLink("Critters", "CREATURES") + " and Eggs in its room and writes the results to an " + Ribbon + ":\n" +
			"\n" +
			"<b>Bit 1</b>: " + Green + " when the count passes the threshold\n" +
			"<b>Bit 2</b>: " + Green + " when the egg count passes its own threshold (separate-thresholds mode only)\n" +
			"<b>Bit 3</b>: a brief " + Green + " pulse each time the tracked count rises\n" +
			"<b>Bit 4</b>: a brief " + Green + " pulse each time the tracked count falls\n" +
			"\n" +
			Ribbon + " must be used as the output wire to avoid overloading.";

		public const string PortName = "Critter Count (4-Bit)";
		public static readonly string PortActive =
			"Bit 1: " + Green + " if the tracked count is past the threshold\n" +
			"Bit 2: " + Green + " if the egg count is past its threshold (separate mode)\n" +
			"Bit 3: " + Green + " pulse when the tracked count rises\n" +
			"Bit 4: " + Green + " pulse when the tracked count falls";
		public static readonly string PortInactive = "Otherwise, sends a " + Red + " on each bit";

		public const string SideScreenTitleKey = "STRINGS.UI.UISIDESCREENS.ADVANCED_CRITTER_SENSOR_SIDE_SCREEN.TITLE";
		public const string SideScreenTitle = "Advanced Critter Sensor";

		public const string ModeCombined = "Combined threshold";
		public static readonly string ModeCombinedTooltip = "One threshold for critters and eggs together. Bit 1 sends a " + Green + " when the combined count passes it.";
		public const string ModeSeparate = "Separate thresholds";
		public static readonly string ModeSeparateTooltip = "Separate thresholds for critters and eggs. Bit 1 reports the critter count, bit 2 reports the egg count.";

		public const string CurrentCritters = "Current Critters: {0}";
		public const string CurrentEggs = "Current Eggs: {0}";
		public const string NotCounting = "Nothing is being counted";
		public const string NotInRoom = "Not in a room";

		public const string AllCritters = "All critters";
		public const string AllCrittersTooltip = "Count every critter species, including ones discovered later";
		public const string AllEggs = "All eggs";
		public const string AllEggsTooltip = "Count every egg type, including ones discovered later";
		public const string CountCrittersTooltip = "Include critters in the count. The list below chooses which species; only discovered species are shown.";
		public const string CountEggsTooltip = "Include eggs in the count. The list below chooses which egg types; only discovered eggs are shown.";

		public const string ValueNameCount = "Count";
		public const string ValueNameCritters = "Critters";
		public const string ValueNameEggs = "Eggs";

		private static readonly string KwCritters = UI.PRE_KEYWORD + "Critters" + UI.PST_KEYWORD;
		private static readonly string KwEggs = UI.PRE_KEYWORD + "Eggs" + UI.PST_KEYWORD;

		public static readonly string CombinedAbove = "Bit 1 sends a " + Green + " if there are more than <b>{0}</b> tracked " + KwCritters + " or " + KwEggs + " in the room";
		public static readonly string CombinedBelow = "Bit 1 sends a " + Green + " if there are fewer than <b>{0}</b> tracked " + KwCritters + " or " + KwEggs + " in the room";
		public static readonly string CrittersAbove = "Bit 1 sends a " + Green + " if there are more than <b>{0}</b> tracked " + KwCritters + " in the room";
		public static readonly string CrittersBelow = "Bit 1 sends a " + Green + " if there are fewer than <b>{0}</b> tracked " + KwCritters + " in the room";
		public static readonly string EggsAbove = "Bit 2 sends a " + Green + " if there are more than <b>{0}</b> tracked " + KwEggs + " in the room";
		public static readonly string EggsBelow = "Bit 2 sends a " + Green + " if there are fewer than <b>{0}</b> tracked " + KwEggs + " in the room";

		public static void Register()
		{
			Strings.Add(PrefabKey + "NAME", Name);
			Strings.Add(PrefabKey + "DESC", Desc);
			Strings.Add(PrefabKey + "EFFECT", Effect);
			Strings.Add(SideScreenTitleKey, SideScreenTitle);
		}
	}
}
