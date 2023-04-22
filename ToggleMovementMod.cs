using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimMovementMods
{
	[BepInPlugin(pluginGUID, pluginName, pluginVersion)]
	[BepInProcess("valheim.exe")]
	public class ToggleMovementMod : BaseUnityPlugin
	{
		const string pluginGUID = "afilbert.ValheimToggleMovementMod";
		const string pluginName = "Valheim - Toggle Movement Mod";
		const string pluginVersion = "0.0.9";
		public static ManualLogSource logger;

		private readonly Harmony _harmony = new Harmony(pluginGUID);
		public static ToggleMovementMod _plugin;

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
		public static ConfigEntry<bool> ReequipWeaponAfterSwimming;
		public static ConfigEntry<bool> RunToCrouchToggle;
		public static ConfigEntry<bool> StopSneakMovementToggle;
		public static ConfigEntry<float> MinStamRefillPercent;
		public static ConfigEntry<bool> SafeguardStaminaOnLowHealth;
		public static ConfigEntry<float> SprintHealthOverride;
		public static ConfigEntry<bool> TrackElapsedZeroStamToggle;
		public static ConfigEntry<float> TrackElapsedZeroStamTime;

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
			SprintToggle = Config.Bind<bool>("Sprint", "SprintToggle", true, "Sprint works like a toggle when true");
			SprintToggleAlternate = Config.Bind<bool>("SprintAlternate", "SprintToggleAlternate", false, "Sprint is toggled through use of another key/button");
			SprintToggleAlternateKey = Config.Bind<string>("SprintAlternate", "SprintToggleAlternateKey", "T", "Used in conjunction with SprintToggleAlternate. This is the key used to toggle sprint on/off");
			AutorunOverride = Config.Bind<bool>("Auto-run", "AutorunToggle", true, "Fixes auto-run to follow look direction");
			AutorunFreelookKey = Config.Bind<string>("Auto-run", "AutorunFreelookKey", "CapsLock", "Overrides look direction in auto-run while pressed");
			AutorunStrafe = Config.Bind<bool>("Auto-run", "AutorunStrafe", true, "Enable strafing while in auto-run/crouch");
			AutorunStrafeForwardDisables = Config.Bind<bool>("Auto-run", "AutorunStrafeForwardDisables", false, "Disable autorun if Forward key/button pressed while AutorunStrafe enabled");
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

			_harmony.PatchAll();
		}

		[HarmonyPatch(typeof(Player), "SetControls")]
		private class ToggleMovement
		{
			private static void Prefix(ref Player __instance, ref Vector3 movedir, ref bool run, ref bool autoRun, ref bool crouch, ref Vector3 ___m_lookDir, ref Vector3 ___m_moveDir, ref bool ___m_autoRun, ref bool ___m_crouchToggled, ref string ___m_actionAnimation, ref List<Player.MinorActionData> ___m_actionQueue)
			{
				if (!__instance || _plugin.IsInMenu() || !EnableToggle.Value)
				{
					autoRun = false;
					___m_autoRun = false;
					AutorunSet = false;
					return;
				}
				if (!Started)
				{
					Started = true;
				}

				bool forwardDown = ZInput.GetButton("Forward") || ZInput.GetButton("JoyForward");
				bool backwardDown = ZInput.GetButton("Backward") || ZInput.GetButton("JoyBackward");
				bool leftDown = ZInput.GetButton("Left") || ZInput.GetButton("JoyLeft");
				bool rightDown = ZInput.GetButton("Right") || ZInput.GetButton("JoyRight");

				bool directionalDown = false;

				if (ReequipWeaponAfterSwimming.Value && EquippedItem != null && EquippedItem.m_shared.m_name != "Unarmed" && __instance.InLiquidSwimDepth())
				{
					ReequipItem = EquippedItem;
				}
				else
				{
					if (ReequipWeaponAfterSwimming.Value && ReequipItem != null && !__instance.InLiquidSwimDepth())
					{
						__instance.EquipItem(ReequipItem);
						ReequipItem = null;
					}
				}

				EquippedItem = __instance.GetCurrentWeapon();

				if (AutorunStrafe.Value)
				{
					directionalDown = backwardDown || (AutorunStrafeForwardDisables.Value && forwardDown);
				}
				else
				{
					directionalDown = forwardDown || backwardDown || leftDown || rightDown;
				}

				bool isWeaponLoaded = true;
				autoRun = false;
				___m_autoRun = false;

				bool equipmentAnimating = ___m_actionAnimation != null;

				if (___m_actionQueue.Exists(item => (item.m_type == Player.MinorActionData.ActionType.Equip || item.m_type == Player.MinorActionData.ActionType.Unequip)))
				{
					equipmentAnimating = true;
				}
				if (AutorunSet && AutorunOverride.Value && (directionalDown))
				{
					AutorunSet = false;
				}
				if (DisableStamLimitOnManualCntrl.Value && (directionalDown) && StaminaRefilling)
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
				if (___m_autoRun && (AutorunOverride.Value && !ZInput.GetButton("Caps")))
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
				if (__instance.GetStaminaPercentage() < StamRefillThreshold)
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
						autoRun = false;
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
				if (TrackElapsedZeroStamToggle.Value)
				{
					if (SprintSet && __instance.GetStaminaPercentage() == 0)
					{
						ElapsedTimeAtZeroStam += Time.deltaTime;
					}
					else
					{
						ElapsedTimeAtZeroStam = 0f;
					}
					if (SprintSet && ElapsedTimeAtZeroStam > TrackElapsedZeroStamTime.Value)
					{
						SprintSet = false;
					}
				}
			}
		}

		[HarmonyPatch(typeof(ZInput), "Reset")]
		private class ZInput_PatchReset
		{
			private static void Postfix(ZInput __instance)
			{
				KeyCode key = (KeyCode)Enum.Parse(typeof(KeyCode), AutorunFreelookKey.Value);
				__instance.AddButton("Caps", key);
				if (SprintToggleAlternate.Value)
				{
					key = (KeyCode)Enum.Parse(typeof(KeyCode), SprintToggleAlternateKey.Value);
					__instance.AddButton("Sprint", key);
				}
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

		private void OnDestroy()
		{
			_harmony.UnpatchSelf();
		}

		private bool IsInMenu()
		{
			return ZInput.GetButtonDown("Esc") || ZInput.GetButtonDown("JoyMenu") || InventoryGui.IsVisible() || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || StoreGui.IsVisible() || Hud.IsPieceSelectionVisible() || UnifiedPopup.IsVisible();
		}

		private void Update()
		{
			if (Started && EnableToggle.Value && !IsInMenu())
			{
				bool run;
				if (SprintToggleAlternate.Value)
				{
					run = ZInput.GetButtonUp("Sprint") || ZInput.GetButtonUp(SprintToggleAlternateKey.Value);
				}
				else
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
				if(!RunToCrouchToggle.Value)
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
				if (run && SprintToggle.Value)
				{
					SprintSet = !SprintSet;
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