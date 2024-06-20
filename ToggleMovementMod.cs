using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace ValheimMovementMods
{
	[BepInPlugin(pluginGUID, pluginName, pluginVersion)]
	[BepInProcess("valheim.exe")]
	public class ToggleMovementMod : BaseUnityPlugin
	{
		const string pluginGUID = "afilbert.ValheimToggleMovementMod";
		const string pluginName = "Valheim - Toggle Movement Mod";
		const string pluginVersion = "1.3.0";
		const string freeLookKey = "FreeLook";
		const string sprintKey = "Sprint";

		public static ManualLogSource logger;

		private readonly Harmony _harmony = new Harmony(pluginGUID);
		public static ToggleMovementMod _plugin;
		public static ZInput _inputInstance;
		public static Dictionary<string, ZInput.ButtonDef> _buttonsDict;

		public static bool Started = false;

		public static ConfigEntry<bool> EnableToggle;
		public static ConfigEntry<bool> SprintToggle;
		public static ConfigEntry<bool> SprintToggleAlternate;
		public static ConfigEntry<string> SprintToggleAlternateKey;
		public static ConfigEntry<bool> DisableStamLimitOnManualCntrl;
		public static ConfigEntry<bool> AutorunOverride;
		public static ConfigEntry<string> AutorunFreelookKey;
		public static ConfigEntry<bool> AutorunStrafe;
		public static ConfigEntry<bool> AutorunStrafeForwardDisables;
		public static ConfigEntry<bool> AutorunDisableOnInput;
		public static ConfigEntry<bool> AutorunDisableOnNoStam;
		public static ConfigEntry<bool> ReequipWeaponAfterSwimming;
		public static ConfigEntry<bool> RunToCrouchToggle;
		public static ConfigEntry<bool> StopSneakMovementToggle;
		public static ConfigEntry<float> MinStamRefillPercent;
		public static ConfigEntry<bool> SafeguardStaminaOnLowHealth;
		public static ConfigEntry<float> SprintHealthOverride;
		public static ConfigEntry<bool> TrackElapsedZeroStamToggle;
		public static ConfigEntry<float> TrackElapsedZeroStamTime;
		public static ConfigEntry<bool> OverrideAutorunSetting;
		public static ConfigEntry<bool> AllowAutorunWhileInMap;
		public static ConfigEntry<bool> VisuallyIndicateSprintState;
		public static ConfigEntry<bool> SprintToggleOnAutorun;
		public static ConfigEntry<bool> AllowAutorunInInventory;
		public static ConfigEntry<bool> DetoggleSprintAtLowStamWhenAttacking;
		public static ConfigEntry<float> DetoggleSprintAtLowStamWhenAttackingThreshold;


		public static string InitialSprintToggleAlternateKey;
		public static string InitialAutorunFreelookKey;

		public static bool StaminaRefilling = false, SprintSet = false, AutorunSet = false;
		public static bool RunToCrouch = false, Crouching = false;
		public static float ElapsedTimeAtZeroStam = 0f, StamRefillThreshold = 0f, SprintHealthThreshold = 0f;

		public static ItemDrop.ItemData EquippedItem = null;
		public static ItemDrop.ItemData ReequipItem = null;

		void Awake()
		{
			_plugin = this;
			logger = Logger;
			EnableToggle = Config.Bind<bool>("Mod Config", "Enable", true, "Enable this mod");
			OverrideAutorunSetting = Config.Bind<bool>("Mod Config", "OverrideGameAutorun", true, "This overrides the new auto-run config setting that functions as a sprint toggle");
			SprintToggle = Config.Bind<bool>("Sprint", "SprintToggle", true, "Sprint works like a toggle when true");
			SprintToggleOnAutorun = Config.Bind<bool>("Sprint", "OnlyToggleWhenAutorunning", false, "Sprint only works like a toggle when auto-running");
			SprintToggleAlternate = Config.Bind<bool>("SprintAlternate", "SprintToggleAlternate", false, "Sprint is toggled through use of another key/button");
			SprintToggleAlternateKey = Config.Bind<string>("SprintAlternate", "SprintToggleAlternateKey", "T", "Used in conjunction with SprintToggleAlternate. This is the key used to toggle sprint on/off");
			AutorunOverride = Config.Bind<bool>("Auto-run", "AutorunToggle", true, "Fixes auto-run to follow look direction");
			AutorunFreelookKey = Config.Bind<string>("Auto-run", "AutorunFreelookKey", "CapsLock", "Overrides look direction in auto-run while pressed");
			AutorunStrafe = Config.Bind<bool>("Auto-run", "AutorunStrafe", true, "Enable strafing while in auto-run/crouch");
			AutorunStrafeForwardDisables = Config.Bind<bool>("Auto-run", "AutorunStrafeForwardDisables", false, "Disable autorun if Forward key/button pressed while AutorunStrafe enabled");
			AutorunDisableOnInput = Config.Bind<bool>("Auto-run", "AutorunDisableOnInput", false, "Disable autorun if directional key/button pressed while AutorunStrafe enabled");
			AutorunDisableOnNoStam = Config.Bind<bool>("Auto-run", "AutorunDisableOnNoStam", false, "Disable autorun if zero stamina reached while AutorunStrafe enabled");
			AllowAutorunWhileInMap = Config.Bind<bool>("Auto-run", "AutorunInMap", true, "Keep running while viewing map");
			AllowAutorunInInventory = Config.Bind<bool>("Auto-run", "AutorunInInventory", false, "Keep running while viewing inventory");
			ReequipWeaponAfterSwimming = Config.Bind<bool>("Swim", "ReequipWeaponAfterSwimming", true, "Any weapon stowed in order to swim will reequip once out of swimming state");
			RunToCrouchToggle = Config.Bind<bool>("Auto-sneak", "RunToCrouchToggle", true, "Allows going from full run to crouch with a click of the crouch button (and vice versa)");
			StopSneakMovementToggle = Config.Bind<bool>("Auto-sneak", "StopSneakOnNoStam", true, "Stops sneak movement if no stamina available. Stock behavior is to pop out of sneak into walk");
			MinStamRefillPercent = Config.Bind<float>("Stamina", "MinStamRefillPercentValue", 20f, "Percentage to stop running and let stamina refill");
			DisableStamLimitOnManualCntrl = Config.Bind<bool>("Stamina", "StopStamLimitOnManualInputToggle", true, "Stops the wait for 100% stam fill to resume sprinting on manual Forward input");
			StamRefillThreshold = MinStamRefillPercent.Value / 100f;
			SafeguardStaminaOnLowHealth = Config.Bind<bool>("Stamina", "SafeguardStaminaOnLowHealthToggle", true, "Allow stamina to recover on low health by automatically detoggling sprint");
			SprintHealthOverride = Config.Bind<float>("Stamina", "SprintHealthOverridePercentValue", 30f, "Percentage of health to detoggle sprint so stamina can start to recover");
			SprintHealthThreshold = SprintHealthOverride.Value / 100f;
			TrackElapsedZeroStamToggle = Config.Bind<bool>("Stamina", "TrackElapsedZeroStamToggle", true, "Automatically toggle off sprint after elapsed time spent at zero stamina");
			TrackElapsedZeroStamTime = Config.Bind<float>("Stamina", "TrackElapsedZeroStamTime", 5f, "Seconds to wait at zero stamina before toggling off sprint");
			VisuallyIndicateSprintState = Config.Bind<bool>("Stamina", "ChangeStamColorOnSprint", true, "Changes stamina bar color to orange when draining and sprint enabled, and blue when stam regenerating. Flashes empty bar if stam drained fully while sprinting");
			DetoggleSprintAtLowStamWhenAttacking = Config.Bind<bool>("Stamina", "DetoggleSprintAtLowStamWhenAttacking", true, "Detoggles sprint if attacking at low stamina");
			DetoggleSprintAtLowStamWhenAttackingThreshold = Config.Bind<float>("Stamina", "DetoggleSprintAtLowStamWhenAttackingThreshold", 0.04f, "Threshold at which stamina will detoggle sprint if also attacking");

			InitialSprintToggleAlternateKey = SprintToggleAlternateKey.Value;
			InitialAutorunFreelookKey = AutorunFreelookKey.Value;

			_harmony.PatchAll();
		}

		[HarmonyPatch(typeof(Player), "SetControls")]
		private class ToggleMovement
		{
			private static void Prefix(ref Player __instance, ref Vector3 movedir, ref bool run, ref bool crouch, ref Vector3 ___m_lookDir, ref Vector3 ___m_moveDir, ref bool ___m_autoRun, ref bool ___m_crouchToggled, ref string ___m_actionAnimation, ref List<Player.MinorActionData> ___m_actionQueue)
			{
				if (!EnableToggle.Value)
				{
					return;
				}
				if (!__instance || _plugin.IsInMenu())
				{
					___m_autoRun = false;
					AutorunSet = false;
					return;
				}
				if (!Started)
				{
					Started = true;
				}
				if (!SprintSet)
				{
					StaminaRefilling = false;
				}

				_plugin.MaybeUpdateConfigurableInput();

				if (OverrideAutorunSetting.Value)
				{
					run = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun");
				}

				bool forwardDown = ZInput.GetButton("Forward") || ZInput.GetButton("JoyForward");
				bool backwardDown = ZInput.GetButton("Backward") || ZInput.GetButton("JoyBackward");
				bool leftDown = ZInput.GetButton("Left") || ZInput.GetButton("JoyLeft");
				bool rightDown = ZInput.GetButton("Right") || ZInput.GetButton("JoyRight");

				bool attackDown = ZInput.GetButton("Attack") || ZInput.GetButton("JoyAttack");

				bool directionalDown = false;

				if (ReequipWeaponAfterSwimming.Value && EquippedItem != null && EquippedItem.m_shared.m_name != "Unarmed" && __instance.IsSwimming())
				{
					ReequipItem = EquippedItem;
				}
				else
				{
					if (ReequipWeaponAfterSwimming.Value && ReequipItem != null && !__instance.IsSwimming())
					{
						__instance.EquipItem(ReequipItem);
						ReequipItem = null;
					}
				}

				EquippedItem = __instance.GetCurrentWeapon();

				if (!_plugin.IsInChat())
				{
					if (AutorunStrafe.Value && !AutorunDisableOnInput.Value)
					{
						directionalDown = backwardDown || (AutorunStrafeForwardDisables.Value && forwardDown);
					}
					else
					{
						directionalDown = (!AutorunStrafe.Value || AutorunDisableOnInput.Value) && (forwardDown || backwardDown || leftDown || rightDown);
					}
				}

				bool isWeaponLoaded = true;
				if (AutorunOverride.Value)
				{
					___m_autoRun = false;
				}

				bool equipmentAnimating = ___m_actionAnimation != null;

				if (___m_actionQueue.Exists(item => (item.m_type == Player.MinorActionData.ActionType.Equip || item.m_type == Player.MinorActionData.ActionType.Unequip)))
				{
					equipmentAnimating = true;
				}
				if ((AutorunSet && AutorunOverride.Value && directionalDown) || (AutorunDisableOnNoStam.Value && __instance.GetStaminaPercentage() == 0))
				{
					AutorunSet = false;
				}
				if (DisableStamLimitOnManualCntrl.Value && directionalDown && StaminaRefilling)
				{
					StaminaRefilling = false;
				}
				if (__instance.GetCurrentWeapon() != null && __instance.GetCurrentWeapon().m_shared.m_name == "$item_crossbow_arbalest")
				{
					isWeaponLoaded = __instance.IsWeaponLoaded();
				}
				if (AutorunSet && !___m_autoRun && !equipmentAnimating)
				{
					___m_autoRun = true;
				}
				if (___m_autoRun && (AutorunOverride.Value && !ZInput.GetButton(freeLookKey)))
				{
					if (AutorunStrafe.Value)
					{
						Vector3 lookDir = ___m_lookDir;
						lookDir.y = 0f;
						lookDir.Normalize();
						___m_moveDir = lookDir + movedir.x * Vector3.Cross(Vector3.up, lookDir);
						if (___m_moveDir.magnitude > 1f)
						{
							___m_moveDir.Normalize();
						}
					}
					else
					{
						___m_moveDir = ___m_lookDir;
						___m_moveDir.y = 0f;
						___m_moveDir.Normalize();
					}
				}
				if (__instance.GetStaminaPercentage() < StamRefillThreshold && !directionalDown)
				{
					StaminaRefilling = true;
				}
				if (__instance.GetStaminaPercentage() == 1)
				{
					StaminaRefilling = false;
				}
				if (Crouching)
				{
					run = false;
					___m_crouchToggled = true;
					crouch = false;
					if (StaminaRefilling && AutorunSet && StopSneakMovementToggle.Value)
					{
						___m_autoRun = false;
					}
				}
				else
				{
					if (__instance.GetHealthPercentage() < SprintHealthThreshold && SafeguardStaminaOnLowHealth.Value)
					{
						SprintSet = false;
					}
					if (SprintSet && (!StaminaRefilling || (forwardDown && DisableStamLimitOnManualCntrl.Value)) && isWeaponLoaded && !equipmentAnimating)
					{
						run = true;
						crouch = false;
					}
				}
				if (SprintSet && __instance.GetStaminaPercentage() == 0)
				{
					ElapsedTimeAtZeroStam += Time.deltaTime;
				}
				else
				{
					ElapsedTimeAtZeroStam = 0f;
				}
				if (TrackElapsedZeroStamToggle.Value)
				{
					if (SprintSet && ElapsedTimeAtZeroStam > TrackElapsedZeroStamTime.Value)
					{
						SprintSet = false;
					}
				}
				if (DetoggleSprintAtLowStamWhenAttacking.Value && SprintSet && __instance.GetStaminaPercentage() <= DetoggleSprintAtLowStamWhenAttackingThreshold.Value && attackDown)
                {
					SprintSet = false;
                }
			}
		}

		[HarmonyPatch(typeof(ZInput), "Load")]
		private class ZInput_PatchLoad
		{
			private static void Postfix(ZInput __instance, Dictionary<string, ZInput.ButtonDef> ___m_buttons)
			{
				_inputInstance = __instance;
				_buttonsDict = ___m_buttons;
				Key key = (Key)Enum.Parse(typeof(Key), "Escape");
				_inputInstance.AddButton("Esc", key);
				_plugin.UpdateBindings();
			}
		}

		private void MaybeUpdateConfigurableInput()
        {
			if (InitialSprintToggleAlternateKey != SprintToggleAlternateKey.Value || InitialAutorunFreelookKey != AutorunFreelookKey.Value)
            {
				_buttonsDict.Remove(sprintKey);
				_buttonsDict.Remove(freeLookKey);
				if (UpdateBindings())
				{
					// Only update the "initial" state if bindings are successful
					InitialSprintToggleAlternateKey = SprintToggleAlternateKey.Value;
					InitialAutorunFreelookKey = AutorunFreelookKey.Value;
				}
			}
		}

		private bool UpdateBindings()
        {
			try
			{
				Key key = (Key)Enum.Parse(typeof(Key), AutorunFreelookKey.Value);
				_inputInstance.AddButton(freeLookKey, key);
				key = (Key)Enum.Parse(typeof(Key), SprintToggleAlternateKey.Value);
				_inputInstance.AddButton(sprintKey, key);
				logger.LogInfo($"Bindings - Free look: {AutorunFreelookKey.Value}. Toggle alternate: {SprintToggleAlternateKey.Value}");

				return true;
			}
			catch (ArgumentException e)
			{
				logger.LogError($"Error binding input buttons: {e.Message}");

				return false;
			}
		}

		[HarmonyPatch(typeof(Game), "Logout")]
		private class Game_PatchLogout
		{
			private static void Postfix()
			{
				Started = false;
			}
		}

		[HarmonyPatch(typeof(Hud), "UpdateStamina")]
		private class Hud_UpdateStamina
		{
			private static void Postfix(Hud __instance, ref Player player, ref TMP_Text ___m_staminaText, ref RectTransform ___m_staminaBar2Root, ref GuiBar ___m_staminaBar2Slow, ref GuiBar ___m_staminaBar2Fast)
			{
				if (EnableToggle.Value && VisuallyIndicateSprintState.Value && SprintToggle.Value && SprintSet)
				{
					___m_staminaBar2Fast.SetColor(new Color(1.0f, 0.64f, 0.0f));

					if (StaminaRefilling)
					{
						___m_staminaBar2Fast.SetColor(Color.blue);
					}
					if (ElapsedTimeAtZeroStam > 1)
					{
						__instance.StaminaBarEmptyFlash();
					}
				}
				else
				{
					___m_staminaBar2Fast.SetColor(Color.yellow);
				}
			}
		}

		private void OnDestroy()
		{
			_harmony.UnpatchSelf();
		}

		private bool IsInMenu()
		{
			return ZInput.GetButtonDown("Esc") || ZInput.GetButtonDown("JoyMenu") || (!AllowAutorunInInventory.Value && InventoryGui.IsVisible()) || (!AllowAutorunWhileInMap.Value && Minimap.IsOpen()) || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || StoreGui.IsVisible() || Hud.IsPieceSelectionVisible() || UnifiedPopup.IsVisible() || GameCamera.InFreeFly() || PlayerCustomizaton.IsBarberGuiVisible();
		}

		private bool IsInChat()
        {
			return Chat.instance && Chat.instance.HasFocus();

		}

		private void Update()
		{
			if (Started && EnableToggle.Value && !IsInMenu() && !IsInChat())
			{
				bool run = false;
				if (SprintToggleAlternate.Value)
				{
					run = ZInput.GetButtonUp("Sprint");
				}
				else if (SprintToggle.Value)
				{
					run = ZInput.GetButtonUp("Run") || ZInput.GetButtonUp("JoyRun");
				}

				bool crouch = ZInput.GetButtonDown("Crouch") || ZInput.GetButtonDown("JoyCrouch");
				bool autoRun = ZInput.GetButtonDown("AutoRun");

				bool backwardDown = ZInput.GetButton("Backward") || ZInput.GetButton("JoyBackward");

				if (!AutorunOverride.Value)
				{
					AutorunSet = false;
				}
				if (!RunToCrouchToggle.Value)
				{
					RunToCrouch = false;
				}
				if (!SprintToggle.Value)
				{
					SprintSet = false;
				}
				if (autoRun && AutorunOverride.Value)
				{
					AutorunSet = !AutorunSet;
				}
				if (!AutorunSet && SprintToggleOnAutorun.Value)
				{
					SprintSet = false;
				}
				if (run && SprintToggle.Value)
				{
					if (!SprintToggleOnAutorun.Value)
					{
						SprintSet = !SprintSet;
					}
					if (SprintToggleOnAutorun.Value && AutorunSet)
					{
						SprintSet = !SprintSet;
					}
				}
				if (crouch && RunToCrouchToggle.Value)
				{
					Crouching = !Crouching;
				}
				if (AutorunSet && backwardDown)
				{
					AutorunSet = false;
				}
			}
		}
	}
}