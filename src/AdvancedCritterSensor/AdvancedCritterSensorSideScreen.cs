using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PeterHan.PLib.UI;
using UnityEngine;

namespace AdvancedCritterSensor
{
	/// <summary>
	/// Side screen for the Advanced Critter Sensor. Built with PLib UI; the three threshold
	/// editors are clones of the vanilla ThresholdSwitchSideScreen prefab so they look and
	/// behave exactly like the stock critter sensor's controls.
	///
	/// Layout (top to bottom):
	///   current count line
	///   [Combined threshold] [Separate thresholds]
	///   combined threshold editor            (combined mode)
	///   [x] Count Critters
	///       [x] All critters / per-species rows
	///       critter threshold editor         (separate mode)
	///   [x] Count Eggs
	///       [x] All eggs / per-egg rows
	///       egg threshold editor             (separate mode)
	/// </summary>
	public sealed class AdvancedCritterSensorSideScreen : SideScreenContent, IRender200ms
	{
		private static readonly FieldInfo SideScreensField = AccessTools.Field(typeof(DetailsScreen), "sideScreens");
		private static readonly FieldInfo CurrentValueField = AccessTools.Field(typeof(ThresholdSwitchSideScreen), "currentValue");

		private const int Indent = 24;
		private static readonly Vector2 IconSize = new Vector2(24f, 24f);
		private static readonly Vector2 CheckSize = new Vector2(16f, 16f);

		private sealed class ThresholdBlock
		{
			public GameObject host;
			public ThresholdSwitchSideScreen screen;
			public ThresholdAdapter adapter;
		}

		private sealed class SpeciesList
		{
			public bool critters;
			public GameObject toggle;
			public GameObject panel;
			public GameObject allRow;
			public readonly List<Tag> visible = new List<Tag>();
			public readonly Dictionary<Tag, GameObject> rows = new Dictionary<Tag, GameObject>();
			public ThresholdBlock threshold;
		}

		private AdvancedCritterSensor target;
		private bool built;
		private bool discoverHooked;

		private GameObject header;
		private GameObject combinedButton;
		private GameObject separateButton;
		private ThresholdBlock combined;
		private readonly SpeciesList critters = new SpeciesList { critters = true };
		private readonly SpeciesList eggs = new SpeciesList { critters = false };

		public override bool IsValidForTarget(GameObject go)
		{
			return go != null && go.GetComponent<AdvancedCritterSensor>() != null;
		}

		public override string GetTitle()
		{
			return ModStrings.SideScreenTitle;
		}

		public override int GetSideScreenSortOrder()
		{
			return 1;
		}

		public override void SetTarget(GameObject go)
		{
			base.SetTarget(go);
			target = go != null ? go.GetComponent<AdvancedCritterSensor>() : null;
			if (target == null)
				return;
			EnsureBuilt();
			HookDiscover(true);
			RebuildList(critters);
			RebuildList(eggs);
			RefreshAll();
		}

		public override void ClearTarget()
		{
			base.ClearTarget();
			target = null;
			HookDiscover(false);
		}

		protected override void OnCleanUp()
		{
			HookDiscover(false);
			base.OnCleanUp();
		}

		public void Render200ms(float dt)
		{
			if (target != null && built && gameObject.activeInHierarchy)
				UpdateHeader();
		}

		// ---- construction ----

		private void EnsureBuilt()
		{
			if (built)
				return;
			built = true;

			PPanel root = new PPanel("AdvancedCritterSensorRoot")
			{
				Direction = PanelDirection.Vertical,
				Alignment = TextAnchor.UpperLeft,
				Spacing = 6,
				Margin = new RectOffset(8, 8, 8, 8),
				FlexSize = Vector2.right,
				DynamicSize = true,
			};

			root.AddChild(new PLabel("Header")
			{
				Text = " ",
				TextStyle = PUITuning.Fonts.TextLightStyle,
				TextAlignment = TextAnchor.MiddleLeft,
				FlexSize = Vector2.right,
				DynamicSize = true,
			}.AddOnRealize(go => header = go));

			PPanel modeRow = new PPanel("ModeRow")
			{
				Direction = PanelDirection.Horizontal,
				Alignment = TextAnchor.MiddleCenter,
				Spacing = 4,
				FlexSize = Vector2.right,
				DynamicSize = true,
			};
			modeRow.AddChild(new PButton("Combined")
			{
				Text = ModStrings.ModeCombined,
				ToolTip = ModStrings.ModeCombinedTooltip,
				TextStyle = PUITuning.Fonts.TextLightStyle,
				Margin = new RectOffset(8, 8, 5, 5),
				FlexSize = Vector2.right,
				OnClick = _ => SetMode(false),
			}.AddOnRealize(go => combinedButton = go));
			modeRow.AddChild(new PButton("Separate")
			{
				Text = ModStrings.ModeSeparate,
				ToolTip = ModStrings.ModeSeparateTooltip,
				TextStyle = PUITuning.Fonts.TextLightStyle,
				Margin = new RectOffset(8, 8, 5, 5),
				FlexSize = Vector2.right,
				OnClick = _ => SetMode(true),
			}.AddOnRealize(go => separateButton = go));
			root.AddChild(modeRow);

			combined = new ThresholdBlock();
			root.AddChild(Host("CombinedThreshold", 0).AddOnRealize(go => combined.host = go));

			AddSpeciesSection(root, critters, STRINGS.BUILDINGS.PREFABS.LOGICCRITTERCOUNTSENSOR.COUNT_CRITTER_LABEL, ModStrings.CountCrittersTooltip);
			AddSpeciesSection(root, eggs, STRINGS.BUILDINGS.PREFABS.LOGICCRITTERCOUNTSENSOR.COUNT_EGG_LABEL, ModStrings.CountEggsTooltip);

			root.AddTo(gameObject);

			CreateThresholdEditor(combined, ThresholdAdapter.Kind.Combined);
			CreateThresholdEditor(critters.threshold, ThresholdAdapter.Kind.Critters);
			CreateThresholdEditor(eggs.threshold, ThresholdAdapter.Kind.Eggs);
		}

		private void AddSpeciesSection(PPanel root, SpeciesList list, string label, string tooltip)
		{
			root.AddChild(new PCheckBox("Count" + (list.critters ? "Critters" : "Eggs"))
			{
				Text = label,
				ToolTip = tooltip,
				TextStyle = PUITuning.Fonts.TextLightStyle,
				TextAlignment = TextAnchor.MiddleLeft,
				CheckSize = CheckSize,
				FlexSize = Vector2.right,
				OnChecked = (_, __) => ToggleCounting(list),
			}.AddOnRealize(go => list.toggle = go));

			root.AddChild(new PPanel((list.critters ? "Critter" : "Egg") + "List")
			{
				Direction = PanelDirection.Vertical,
				Alignment = TextAnchor.UpperLeft,
				Spacing = 2,
				Margin = new RectOffset(Indent, 0, 0, 0),
				FlexSize = Vector2.right,
				DynamicSize = true,
			}.AddOnRealize(go => list.panel = go));

			list.threshold = new ThresholdBlock();
			root.AddChild(Host((list.critters ? "Critter" : "Egg") + "Threshold", Indent).AddOnRealize(go => list.threshold.host = go));
		}

		private static PPanel Host(string name, int indent)
		{
			return new PPanel(name)
			{
				Direction = PanelDirection.Vertical,
				Alignment = TextAnchor.UpperLeft,
				Margin = new RectOffset(indent, 0, 0, 0),
				FlexSize = Vector2.right,
				DynamicSize = true,
			};
		}

		private void CreateThresholdEditor(ThresholdBlock block, ThresholdAdapter.Kind kind)
		{
			if (block == null || block.host == null)
				return;
			ThresholdSwitchSideScreen prefab = FindThresholdPrefab();
			if (prefab == null)
			{
				Debug.LogWarning("[AdvancedCritterSensor] Vanilla ThresholdSwitchSideScreen prefab not found; threshold editor unavailable");
				return;
			}

			GameObject adapterGo = new GameObject("ThresholdAdapter_" + kind);
			adapterGo.transform.SetParent(gameObject.transform, false);
			block.adapter = adapterGo.AddComponent<ThresholdAdapter>();
			block.adapter.kind = kind;

			GameObject clone = Util.KInstantiateUI(prefab.gameObject, block.host, force_active: false);
			clone.name = "ThresholdEditor_" + kind;
			block.screen = clone.GetComponent<ThresholdSwitchSideScreen>();

			// The vanilla editor shows "Current Count:\n<n>" above its controls; this screen
			// has its own one-line header instead.
			LocText currentValue = CurrentValueField != null ? CurrentValueField.GetValue(block.screen) as LocText : null;
			if (currentValue != null)
				currentValue.gameObject.SetActive(false);
		}

		private static ThresholdSwitchSideScreen FindThresholdPrefab()
		{
			if (DetailsScreen.Instance == null || SideScreensField == null)
				return null;
			List<DetailsScreen.SideScreenRef> refs = SideScreensField.GetValue(DetailsScreen.Instance) as List<DetailsScreen.SideScreenRef>;
			if (refs == null)
				return null;
			foreach (DetailsScreen.SideScreenRef r in refs)
			{
				ThresholdSwitchSideScreen screen = r.screenPrefab as ThresholdSwitchSideScreen;
				if (screen != null)
					return screen;
			}
			return null;
		}

		// ---- species lists ----

		private void HookDiscover(bool hook)
		{
			DiscoveredResources dr = DiscoveredResources.Instance;
			if (dr == null)
				return;
			if (hook && !discoverHooked)
			{
				dr.OnDiscover += OnDiscovered;
				discoverHooked = true;
			}
			else if (!hook && discoverHooked)
			{
				dr.OnDiscover -= OnDiscovered;
				discoverHooked = false;
			}
		}

		private void OnDiscovered(Tag tag, Tag category)
		{
			if (target == null || !built)
				return;
			RebuildList(critters);
			RebuildList(eggs);
			RefreshRows(critters);
			RefreshRows(eggs);
		}

		/// <summary>
		/// Recomputes the visible entries (discovered plus anything currently selected) and
		/// rebuilds the row widgets when the set changed. Returns true if rows were rebuilt.
		/// </summary>
		private bool RebuildList(SpeciesList list)
		{
			List<Tag> visible = list.critters ? CollectCritters() : CollectEggs();
			if (list.allRow != null && SameTags(visible, list.visible))
				return false;

			list.visible.Clear();
			list.visible.AddRange(visible);
			foreach (GameObject row in list.rows.Values)
				if (row != null)
					Destroy(row);
			list.rows.Clear();
			if (list.allRow != null)
				Destroy(list.allRow);

			SpeciesList captured = list;
			list.allRow = new PCheckBox("All")
			{
				Text = list.critters ? ModStrings.AllCritters : ModStrings.AllEggs,
				ToolTip = list.critters ? ModStrings.AllCrittersTooltip : ModStrings.AllEggsTooltip,
				TextStyle = PUITuning.Fonts.TextLightStyle,
				TextAlignment = TextAnchor.MiddleLeft,
				CheckSize = CheckSize,
				FlexSize = Vector2.right,
				OnChecked = (_, __) => ToggleAll(captured),
			}.AddTo(list.panel);

			foreach (Tag tag in list.visible)
			{
				Tag capturedTag = tag;
				GameObject prefab = Assets.GetPrefab(tag);
				Sprite icon = null;
				if (prefab != null)
				{
					var ui = Def.GetUISprite(prefab);
					icon = ui != null ? ui.first : null;
				}
				list.rows[tag] = new PCheckBox(tag.Name)
				{
					Text = tag.ProperName(),
					ToolTip = tag.ProperName(),
					Sprite = icon,
					SpriteSize = IconSize,
					SpritePosition = TextAnchor.MiddleLeft,
					TextStyle = PUITuning.Fonts.TextLightStyle,
					TextAlignment = TextAnchor.MiddleLeft,
					CheckSize = CheckSize,
					FlexSize = Vector2.right,
					OnChecked = (_, __) => ToggleSpecies(captured, capturedTag),
				}.AddTo(list.panel);
			}
			return true;
		}

		private static bool SameTags(List<Tag> a, List<Tag> b)
		{
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}

		/// <summary>Discovered baggable species (the Critter Pick-Up list) plus any selected tag.</summary>
		private List<Tag> CollectCritters()
		{
			HashSet<Tag> set = new HashSet<Tag>();
			if (DiscoveredResources.Instance != null)
				foreach (Tag tag in DiscoveredResources.Instance.GetDiscoveredResourcesFromTag(GameTags.BagableCreature))
					set.Add(tag);
			if (target != null && target.critterTags != null)
				foreach (Tag tag in target.critterTags)
					set.Add(tag);
			List<Tag> result = new List<Tag>(set);
			result.Sort((x, y) => string.Compare(x.ProperName(), y.ProperName(), StringComparison.CurrentCultureIgnoreCase));
			return result;
		}

		/// <summary>Discovered egg prefabs in the incubator's order, plus any selected tag.</summary>
		private List<Tag> CollectEggs()
		{
			HashSet<Tag> selected = new HashSet<Tag>();
			if (target != null && target.eggTags != null)
				foreach (Tag tag in target.eggTags)
					selected.Add(tag);
			List<KeyValuePair<Tag, int>> entries = new List<KeyValuePair<Tag, int>>();
			HashSet<Tag> seen = new HashSet<Tag>();
			foreach (GameObject prefab in Assets.GetPrefabsWithTag(GameTags.Egg))
			{
				if (prefab == null)
					continue;
				Tag tag = prefab.PrefabID();
				if (!seen.Add(tag))
					continue;
				bool discovered = DiscoveredResources.Instance != null && DiscoveredResources.Instance.IsDiscovered(tag);
				if (!discovered && !selected.Contains(tag) && !DebugHandler.InstantBuildMode)
					continue;
				IHasSortOrder sortable = prefab.GetComponent<IHasSortOrder>();
				entries.Add(new KeyValuePair<Tag, int>(tag, sortable != null ? sortable.sortOrder : int.MaxValue));
			}
			foreach (Tag tag in selected)
				if (seen.Add(tag))
					entries.Add(new KeyValuePair<Tag, int>(tag, int.MaxValue));
			entries.Sort((x, y) =>
			{
				int c = x.Value.CompareTo(y.Value);
				return c != 0 ? c : string.Compare(x.Key.ProperName(), y.Key.ProperName(), StringComparison.CurrentCultureIgnoreCase);
			});
			List<Tag> result = new List<Tag>(entries.Count);
			foreach (KeyValuePair<Tag, int> entry in entries)
				result.Add(entry.Key);
			return result;
		}

		// ---- interaction ----

		private void SetMode(bool separate)
		{
			if (target == null)
				return;
			target.separateThresholds = separate;
			target.NotifyConfigChanged();
			RefreshAll();
		}

		private void ToggleCounting(SpeciesList list)
		{
			if (target == null)
				return;
			if (list.critters)
				target.countCritters = !target.countCritters;
			else
				target.countEggs = !target.countEggs;
			target.NotifyConfigChanged();
			RefreshAll();
		}

		private void ToggleAll(SpeciesList list)
		{
			if (target == null)
				return;
			bool all = list.critters ? target.allCritters : target.allEggs;
			target.SetAllSpecies(list.critters, !all);
			RefreshRows(list);
			UpdateHeader();
		}

		private void ToggleSpecies(SpeciesList list, Tag tag)
		{
			if (target == null)
				return;
			target.ToggleSpecies(list.critters, tag, list.visible);
			RefreshRows(list);
			UpdateHeader();
		}

		// ---- refresh ----

		private void RefreshAll()
		{
			if (target == null || !built)
				return;
			bool separate = target.separateThresholds;
			SetButtonSelected(combinedButton, !separate);
			SetButtonSelected(separateButton, separate);

			ShowThreshold(combined, !separate);
			RefreshSection(critters, target.countCritters, separate);
			RefreshSection(eggs, target.countEggs, separate);
			UpdateHeader();
		}

		private void RefreshSection(SpeciesList list, bool counting, bool separate)
		{
			if (list.toggle != null)
				PCheckBox.SetCheckState(list.toggle, counting ? PCheckBox.STATE_CHECKED : PCheckBox.STATE_UNCHECKED);
			if (list.panel != null)
				list.panel.SetActive(counting);
			if (counting)
				RefreshRows(list);
			ShowThreshold(list.threshold, separate && counting);
		}

		private void RefreshRows(SpeciesList list)
		{
			if (target == null)
				return;
			bool all = list.critters ? target.allCritters : target.allEggs;
			if (list.allRow != null)
			{
				int state = all ? PCheckBox.STATE_CHECKED : (target.AnySpeciesSelected(list.critters) ? PCheckBox.STATE_PARTIAL : PCheckBox.STATE_UNCHECKED);
				PCheckBox.SetCheckState(list.allRow, state);
			}
			foreach (KeyValuePair<Tag, GameObject> row in list.rows)
			{
				if (row.Value == null)
					continue;
				bool selected = target.IsSpeciesSelected(list.critters, row.Key);
				PCheckBox.SetCheckState(row.Value, selected ? PCheckBox.STATE_CHECKED : PCheckBox.STATE_UNCHECKED);
			}
		}

		private void ShowThreshold(ThresholdBlock block, bool visible)
		{
			if (block == null || block.host == null)
				return;
			block.host.SetActive(visible && block.screen != null);
			if (!visible || block.screen == null)
			{
				if (block.screen != null && block.screen.gameObject.activeSelf)
					block.screen.Show(false);
				return;
			}
			block.adapter.sensor = target;
			block.screen.SetTarget(block.adapter.gameObject);
			block.screen.Show(true);
		}

		private static void SetButtonSelected(GameObject button, bool selected)
		{
			if (button == null)
				return;
			KImage image = button.GetComponent<KImage>();
			if (image == null)
				return;
			image.colorStyleSetting = selected ? PUITuning.Colors.ButtonPinkStyle : PUITuning.Colors.ButtonBlueStyle;
			image.ApplyColorStyleSetting();
		}

		private void UpdateHeader()
		{
			if (header == null || target == null)
				return;
			string text;
			if (!target.InRoom)
				text = ModStrings.NotInRoom;
			else if (target.countCritters && target.countEggs)
				text = string.Format(ModStrings.CurrentCritters, target.CritterCount) + "    " + string.Format(ModStrings.CurrentEggs, target.EggCount);
			else if (target.countCritters)
				text = string.Format(ModStrings.CurrentCritters, target.CritterCount);
			else if (target.countEggs)
				text = string.Format(ModStrings.CurrentEggs, target.EggCount);
			else
				text = ModStrings.NotCounting;
			PUIElements.SetText(header, text);
		}
	}
}
