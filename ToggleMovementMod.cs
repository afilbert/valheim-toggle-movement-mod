using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;

namespace ValheimMovementMods
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    [BepInProcess("valheim.exe")]
    public class ToggleMovementMod : BaseUnityPlugin
    {
		const string pluginGUID = "afilbert.ValheimToggleMovementMod";
		const string pluginName = "Valheim - Toggle Movement Mod";
		const string pluginVersion = "0.0.1";
		public static ManualLogSource logger;

		private readonly Harmony _harmony = new Harmony(pluginGUID);
		public static ToggleMovementMod _plugin;

		public static bool Started = false;

		public static ConfigEntry<bool> EnableToggle;
		public static ConfigEntry<bool> SprintToggle;
		public static ConfigEntry<bool> AutorunOverride;
		public static ConfigEntry<string> AutorunFreelookKey;
		public static ConfigEntry<bool> RunToCrouchToggle;
		public static ConfigEntry<bool> StopSneakMovementToggle;
		public static ConfigEntry<float> MinStamRefillPercent;

		public static bool StaminaRefilling = false, SprintSet = false, AutorunSet = false;
		public static bool RunToCrouch = false, Crouching = false;
		public static float StamRefillThreshold = 0f;

		void Awake()
        {
			_plugin = this;
			logger = Logger;
			EnableToggle = Config.Bind<bool>("Mod Config", "Enable", true, "Enable this mod");
			SprintToggle = Config.Bind<bool>("Sprint", "SprintToggle", true, "Sprint works like a toggle when true");
			AutorunOverride = Config.Bind<bool>("Autorun", "AutorunToggle", true, "Fixes auto-run to follow look direction");
			AutorunFreelookKey = Config.Bind<string>("Autorun", "AutorunFreelookKey", "CapsLock", "Overrides look direction in auto-run while pressed");
			RunToCrouchToggle = Config.Bind<bool>("Crouch", "RunToCrouchToggle", true, "Allows going from full run to crouch with a click of the crouch button (and vice versa)");
			StopSneakMovementToggle = Config.Bind<bool>("Crouch", "StopSneakOnNoStam", true, "Stops sneak movement if no stamina available. Stock behavior is to pop out of sneak into walk");
			MinStamRefillPercent = Config.Bind<float>("Stamina", "MinStamRefillPercentValue", 20f, "Percentage to stop running and let stamina refill");
			StamRefillThreshold = MinStamRefillPercent.Value / 100f;

			_harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Player), "SetControls")]
        private class ToggleMovement
        {
            private static void Prefix(ref Player __instance, ref bool run, ref bool autoRun, ref bool crouch, ref Vector3 movedir, ref Vector3 ___m_lookDir, ref Vector3 ___m_moveDir, ref bool ___m_autoRun, ref bool ___m_crouchToggled, ref bool ___m_attached)
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
				autoRun = false;
				___m_autoRun = false;
				if (AutorunSet && !___m_autoRun)
				{
					___m_autoRun = true;
				}
				if (___m_autoRun && (AutorunOverride.Value && !ZInput.GetButton("Caps")))
				{
					___m_moveDir.x = ___m_lookDir.x;
					___m_moveDir.z = ___m_lookDir.z;
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
					if (SprintSet && !StaminaRefilling)
					{
						run = true;
						crouch = false;
					}
				}
			}
        }

		[HarmonyPatch(typeof(ZInput), "Reset")]
		private class ZInput_PatchReset
		{
			private static void Postfix(ZInput __instance)
			{
				KeyCode key = (KeyCode)Enum.Parse(typeof(KeyCode), "Escape");
				__instance.AddButton("Esc", key);
				key = (KeyCode)Enum.Parse(typeof(KeyCode), AutorunFreelookKey.Value);
				__instance.AddButton("Caps", key);
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
				bool run = ZInput.GetButtonUp("Run") || ZInput.GetButtonUp("JoyRun");
				bool crouch = ZInput.GetButtonDown("Crouch") || ZInput.GetButtonDown("JoyCrouch");
				bool autoRun = ZInput.GetButtonDown("AutoRun");

				bool forwardDown = ZInput.GetButtonDown("Forward") || ZInput.GetButtonDown("JoyForward");
				bool backwardDown = ZInput.GetButtonDown("Backward") || ZInput.GetButtonDown("JoyBackward");
				bool leftDown = ZInput.GetButtonDown("Left") || ZInput.GetButtonDown("JoyLeft");
				bool rightDown = ZInput.GetButtonDown("Right") || ZInput.GetButtonDown("JoyRight");

				if (!AutorunOverride.Value)
                {
					AutorunSet = false;
				}
				if(!RunToCrouchToggle.Value)
                {
					RunToCrouch = false;
                }
				if(!SprintToggle.Value)
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
                if (AutorunSet && AutorunOverride.Value && (forwardDown || backwardDown || leftDown || rightDown))
                {
                    AutorunSet = false;
                }
            }
		}
	}
}