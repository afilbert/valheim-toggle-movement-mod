# Valheim - Toggle Movement Mod (ToMoMo)

**_Works with Mistlands Update!_**

Have you ever played so much Valheim that you injured yourself? Well, I did. When Mistlands released, I bounded over the new and difficult terrain for so many hours mashing my `Shift` key that I experienced what I can only describe as "Valheim pinkie." Basically, I suffered an RSI that forced me to take a break. That's when I knew I needed to make this mod!

## Features

* Sprint (Run) functions as a toggle
* Auto-sneak is now possible
* Auto-run/sneak now follows look direction by default
  * Can be overridden to detach look direction when assigned key is held (default is CapsLock)
* Auto-run while sprinting safeguards stamina regen at threshold (20% by default)
* Auto-sneak while crouching safeguards stamina regen at threshold (20% by default)
* Seamlessly auto-run directly into auto-sneak (and vice versa)
* Jumping doesn't cancel auto-run
* Regain manual control of auto-run/sneak at any time by using directional input (or Back key/button if AutorunStrafe enabled)
* Sprinting will temporarily and automatically pause while Arbalest is reloading
* Auto-run sprinting will temporarily and automatically pause to allow equipping/switching weapons
* Sprint will detoggle if health less than threshold (30% by default)
* Fully configurable

## QOL Detail

### Run toggle

No more holding that Run button for hours!

Mistlands requires sprint jumping to effectively scale the jagged terrain. Gorging on high-stamina foods is essentially a requirement, and you need to almost constantly hold your Run key/button in order to get around.

Stock Valheim still hasn't introduced a Run toggle setting, so this mod fixes that by turning your Run key/button into a toggle.

### Seamless auto-run/sneak

This mod allows you to auto-sneak!

Sweating bullets trying to sneak back through the Prairie biome to recover your grave is a unique experience. As you creep forward toward the map marker, frantically attempting to spot nearby enemies, your stamina eventually depletes to zero. And just like that, your character pops out of stealth to attract the receiving end of a Deathsquito's sting. You ruefully hope your "no skill drain" effect lasts a lil longer, right? 

And when you do manage to recover your belongings, you'll have more than ample motivation to maximize your corpse run ability and snap immediately into a full on auto-run.

Stock Valheim auto-run doesn't work while sneaking. If you're crouching and activate auto-run, you pop right up into a walk by default. Similarly auto-running and crouching into sneak will simply disable auto-run.

This mod fixes all that seamlessly, and installs stamina safeguards while auto-running/sneaking.

* The default stamina safeguard is 20%
* When auto-running, you will transition to a walk until stamina fully regenerates 
  * Stamina regen can be overridden if needed by holding your Run key/button or Forward key/button
* When auto-sneaking, you will simply stop while crouched until stamina fully regenerates
  * Stamina regen can be overridden if needed by holding your directional input keys while crouching

### Safeguard Stamina on Low Health

With sprint enabled, especially with manual directional input, your stamina can quickly dwindle to 0 without you noticing. _Sometimes this is a really bad situation to be in!_

That's especially true if your health is so low that you can't run away or fight back, and strafing to evade more blows is keeping your stamina from recharging. It's easy to forget that you've toggled on sprint when your character is plodding around. This is likely one of the reasons that the developers didn't include a toggle in the first place.

So v0.0.3 introduces a safety valve that toggles off Sprint if your health drops below a target threshold (30% by default).

### Auto-run Sprint Enable/Switch Weapons

v0.0.3 introduced the ability to equip/switch weapons during auto-run while sprinting. Sometimes you know you're getting close to a battle, and want to arrive prepared. Stock Valheim won't let you equip/switch weapons while sprinting, but now you can!

## Config

The [Official BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) is a required dependency.

Configuration allows:

* **Enable**, Enable the mod, default: true
* **SprintToggle**, Sprint works like a toggle, default: true
* **AutorunToggle**, Fixes auto-run to follow look, default: true
* **AutorunFreelookKey**, Overrides look direction in auto-run while pressed, default: CapsLock
* **AutorunStrafe**, Enable strafing while in auto-run/crouch, default: true
* **RunToCrouchToggle**, Go from run to crouch with the click of a button, default: true
* **StopSneakOnNoStam**, Stops sneak movement if no stamina available, default: true
* **MinStamRefillPercentValue**, Percentage to stop running/sneaking and let stamina refill, default: 20%
* **StopStamLimitOnManualInputToggle**, Stops the wait for 100% stam fill to resume sprinting on manual Forward input, default: true
* **SafeguardStaminaOnLowHealthToggle**", Allow stamina to recover on low health by automatically detoggling sprint, default: true
* **SprintHealthOverridePercentValue**, Percentage of health to detoggle sprint so stamina can start to recover, default: 30%

Built with [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)

![toggle-movement-mod](https://raw.githubusercontent.com/afilbert/valheim-toggle-movement-mod/main/doc/img/ToggleMovementMod.png)

## Releases

Releases in github repo are packaged for Thunderstore Mod Manager.

* 0.0.5 Strafe while auto-running/sneaking
* 0.0.4 Unequip weapons while sprinting
* 0.0.3 More QOL improvements. Bump build version number
  * Temporarily disables sprint toggle while Arbalest is reloading
  * Toggle sprint off if health reduces to less than threshold (30% by default)
  * Allow switching weapons while sprinting
  * Fixes issue preventing sprinting until 100% stam refill, even after manual intervention
* 0.0.2 Fix README hero link
* 0.0.1 Initial publication
