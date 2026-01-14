# Spawn Pool Control

This mod provides a very powerful way of modifying Peak's spawn pools.  It supports all modded items and _should be_ resilient to game updates, including new biomes, items, and potential spawn pools.

Meant to replace tony4twenty's PEAK Luggage/Nature items.  Still in early alpha; please file an issue or contact me directly if something doesn't work.

## Configuration

This mod only contains one config option:

`ConfigName = something`

This will load a JSON file from `BepinEx/configs/SpawnPoolControl/something.json`, which will dictate how items are spawned.

**This config option is blank by default and will need to be set manually.** Run the game once to generate the config, then set the value to the name of your file.

---

<details>
  <summary>Click to see an example of a config file</summary>

```
{
  "clear_default_weights": true,
  "force_allow_duplicates": true,
  "items": {
    "Energy Drink": {
      "LuggageBeach": 100,
      "LuggageJungle": 100,
      "LuggageTundra": 100,
      "LuggageCaldera": 100,
      "LuggageMesa": 100,
      "LuggageRoots": 100,
      "LuggageCursed": 100,
      "LuggageClimber": 100,
      "RespawnCoffin": 100,
      "BerryBushBeach": 100,
      "BerryBushJungle": 100,
      "JungleVine": 100,
      "SpikyVine": 100,
      "CoconutTree": 100,
      "WillowTreeJungle": 100,
      "NestEgg": 100,
      "Cactus": 100,
      "Redwood": 100,
      "Campfire": 100
    },
    "Napberry": {
      "SpawnControl_Vine": 1,
      "SpawnControl_MushroomCluster": 1
    },
    "Megaphone": {
      "LuggageBeach": 20,
      "SpawnControl_Vine": 9,
      "SpawnControl_MushroomCluster": 9
    }
  },
  "replacements": {
    "BingBong": "AncientIdol",
    "Shell Big": "Dynamite"
  }
}
```

`clear_default_weights` dictates whether or not we should override the default items in any given spawn pool. Set this to `true` if you want to do an "X items only" run, or `false` if you just want to add a specific item to a spawn pool.

`force_allow_duplicates` dictates if duplicates are allowed in small/big luggages (not Explorer or Ancient lugagges). You should set this to `true` if you don't have a lot of items in a given pool.

`items` is a dictionary of item names with the given pool and the associated weight. Note that the numbers are weights, not percentages - if you have two items with 100 weight each, then they will have a 50/50 chance of spawning.

`replacements` is a dictionary of single items to replace with another item. This only applies for items spawned via a SingleItemSpawner - see below.

In this particular example:
- we're overriding the default pools, so only the specified items will spawn
- we're allowing duplicates since the pools have few items. If we set this to false, most luggages would spawn Airline Food (item ID 0).
- Energy Drink will spawn in every item pool.
- The Megaphone will have a 1/6 chance of spawning in Shore (since 100 + 20 = 120, and 20/120 = 1/6), and a 90% chance of spawning on Vines and in mushroom clusters.
- The Napberry will have a 10% chance of spawning on Vines and in mushroom clusters.
- The Bing Bong item at the start of the game will be replaced by the Ancient Idol.
- The pink conch shells on Shore will be replaced by dynamite, which explodes if you get near.

</details>

---

Whenever you launch the game, the mod will generate `_reference.json` in the above folder. It is a raw dump of all items and their respective spawn pools. This will include any modded items.
You generally will have to launch the game once to generate the file, find the IDs of whichever items you want to tweak, then construct your own custom file.
Any changes to the file will be overwritten on startup.

This plugin has some special pools that trigger different codepaths due to how the game generates these items:

- `SpawnControl_MushroomCluster`
- `SpawnControl_Vine`

MushroomCluster pools spawn items in random locations in groups of 2-4, or groups of 1 in Roots. All items in the group are the same type.
Vine pools spawn items on vines in Tropics and Roots.

These pools do not have any effect when modified in-game despite having seemingly valid entries:

 - `JungleVine`
 - `LuggageAncient`
 - `GuidebookPageBeach`
 - `GuidebookPageTropics`
 - `GuidebookPageAlpine`
 - `MushroomCluster`

Some items in the game are controlled by a SingleItemSpawner object. These items include, but aren't limited to:

- remedy shroom/shelf fungus/magic beans
- item spawns on beach (airplane crash stuff, shells)
- backpacks
- dynamite/scorpion spawns

Currently I've only implemented replacing these items directly, no weights. To do that, just add values to the replacements section of the JSON file in this format:

```
  "replacements": {
    "BingBong": "AncientIdol",
    "Shell Big": "Dynamite"
  }
```

This will replace the Bing Bong plush at the crash site with the Ancient Idol, and the conch shells littered around the beach with dynamite.
