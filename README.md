# Battletech Game - Internal Heaters

This mod allows one to emulate engines with more internal heatsinks than the base game allows through a couple of methods.

1. Change the amount of internal single heatsinks a mech has.
2. Allow for certain items (the vanilla double heatsink by default) to make the engine have double heatsinks

This mod also changes calculations for heatsink stats in the mech bay display to be accurate for the new heat dissipation.

## Install
- [Install BTML and Modtek](https://github.com/Mpstark/ModTek/wiki/The-Drop-Dead-Simple-Guide-to-Installing-BTML-&-ModTek-&-ModTek-mods).
- Put the InternalHeaters folder containing the `InternalHeaters.dll` and `mod.json` files into your `\BATTLETECH\Mods` folder.
- If you want to change any settings described below do so in the mod.json.
- Start the game.

## Settings
Setting | Type | Default | Description
--- | --- | --- | ---
`useChassisHeatSinks` | `bool` | false | change the size of mechs using the format `"chassis string" : multiplier`. A big locust would be like `"chassisdef_locust_LCT-1V": 15`
`allDoubleHeatSinksDoubleEngineHeatDissipation` | `bool`| true | if the only heatsink components found in the mech are those specified in `doubleHeatSinksDoubleEngineHeatDissipationComponentIds`, the base heat dissipation of the mech is doubled  
`doubleHeatSinksDoubleEngineHeatDissipationComponentIds` | `array` of string component ids | `["Gear_Heatsink_Generic_Double"]` | array of component ids for items that are allowed to be used for double heatsink'ing
`doNotCountFirstDoubleHeatSinksComponentDissipation` | `bool` | false | ignore the first heatsink'ing of the first detected item of type specified in `doubleHeatSinksDoubleEngineHeatDissipationComponentIds`. this is useful if you want to use a component like the vanilla double heatsink as a placeholder for double engine and get 60 heat dissipation instead of 66. 
`debug` | `bool` | false | enable debug logging
`doubleHeatSinksDoubleEngineHeatDissipationComponentId` | `string` | `"Gear_Heatsink_Generic_Double"` | **DEPRECATED** set the heatsink component that when exclusively installed causes double heatsink'ing.

*Note: `doubleHeatSinksDoubleEngineHeatDissipationComponentId` is still usable to allow for transition from version 0.0.10 to 1.0.0. It will be removed in the next version.* 

## Requirements

* [Battletech Game](http://battletechgame.com/)
* [BTML](https://github.com/Mpstark/BattleTechModLoader)
* [ModTek](https://github.com/Mpstark/ModTek)

## Special Thanks

Thanks HBS for the great game and thanks mpstark for the mod framework.

## License

[MIT](LICENSE)