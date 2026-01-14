using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Zorro.Core;

[BepInPlugin("sappykun.PEAK_SpawnPoolControl", "PEAK Spawn Pool Control", "0.2.0")]
public class SpawnPoolControlPlugin : BaseUnityPlugin
{

	internal static ManualLogSource? Log;
	internal static SpawnPoolConfig? referencePool;
	internal static SpawnPoolConfig? currentPool;

	internal static Dictionary<string, ushort> itemNameReverseLUT = new Dictionary<string, ushort>(); 

	private ConfigEntry<string>? currentPoolName;

	private void Awake()
	{
		Harmony val = new Harmony("sappykun.PEAK_SpawnPoolControl");
		val.PatchAll();
		Log = Logger;
		Log?.LogInfo("Plugin sappykun.PEAK_SpawnPoolControl is loaded!");

		currentPoolName = Config.Bind<string>("General", "ConfigName", "", "Name of JSON config file to load (without extensions).");
		currentPoolName.SettingChanged += delegate
		{
			currentPool = JsonHandler.Load(currentPoolName.Value);
			LootData.PopulateLootData();
		};
	}

	private void Start()
	{
		GenerateReferenceConfig();

		if (currentPoolName == null || currentPoolName.Value == "") { return; }

		currentPool = JsonHandler.Load(currentPoolName.Value);
		LootData.PopulateLootData();
	}

	[HarmonyPatch(typeof(LootData))]
	public static class LootData_PopulateLootData_Patch
	{
		[HarmonyPostfix]
		[HarmonyPatch("PopulateLootData")]
		public static void Postfix()
		{
			if (referencePool == null) { return; }
			if (currentPool?.SpawnPoolItems == null) { return; }

			if (currentPool.ClearDefaultWeights)
			{
	 			LootData.AllSpawnWeightData = new Dictionary<SpawnPool, Dictionary<ushort, int>>();
			}

			foreach (var spawnPoolItem in currentPool.SpawnPoolItems)
			{
				foreach (var pool in spawnPoolItem.Value)
				{
					if (Enum.TryParse(pool.Key, out SpawnPool spawnPool))
					{
						if (itemNameReverseLUT.TryGetValue(spawnPoolItem.Key, out var itemID))
						{
							LootData.AllSpawnWeightData.TryAdd(spawnPool, new Dictionary<ushort, int>()); 
							LootData.AllSpawnWeightData[spawnPool][itemID] = pool.Value;
						}
					}
				}
			}

		}
	}
	private static void GenerateReferenceConfig()
	{
		if (referencePool != null) { return; }

		itemNameReverseLUT = new Dictionary<string, ushort>();

		LootData.PopulateLootData();

		referencePool = new SpawnPoolConfig
		{
			ClearDefaultWeights = false,
			ForceAllowDuplicates = false,
			SpawnPoolItems = new Dictionary<string, Dictionary<string, int>>(),
			SpawnPoolReplacements = new Dictionary<string, string>(),
		};

		foreach(var peakItem in SingletonAsset<ItemDatabase>.Instance.itemLookup)
		{
			Dictionary<string, int> poolWeightDict = new Dictionary<string, int>();
			referencePool.SpawnPoolItems.Add(peakItem.Value.gameObject.name, poolWeightDict);

			itemNameReverseLUT[peakItem.Value.gameObject.name] = peakItem.Key;

			foreach (var pool in Enum.GetValues(typeof(SpawnPool)).Cast<SpawnPool>())
			{		
				if (!LootData.AllSpawnWeightData.TryGetValue(pool, out var allItemsInPool))	{ continue; }

				foreach (KeyValuePair<ushort, int> poolItem in allItemsInPool)
				{
					if (peakItem.Key != poolItem.Key)	{ continue; }

					referencePool.SpawnPoolItems[peakItem.Value.gameObject.name][pool.ToString()] = poolItem.Value;
				}
			}
		}

		JsonHandler.Save(referencePool, "_reference");
	}

	[HarmonyPatch(typeof(LootData), "GetRandomItems")]
	public static class Patch_GetRandomItems
	{
		static void Prefix(ref bool canRepeat)
		{
			if (currentPool != null && currentPool.ForceAllowDuplicates)
			{
	 			canRepeat = true;
			}
		}
	}

	[HarmonyPatch(typeof(BerryVine))]
	public class BerryVinePatch
	{
		[HarmonyPrefix]
		[HarmonyPatch("SpawnItems", typeof(List<Transform>))]
		public static void SpawnItemsPrefix(BerryVine __instance, List<Transform> spawnSpots, Vector2 __state)
		{
			if (currentPool?.SpawnPoolItems == null)
				return;
			
			foreach (var spawnPoolItem in currentPool.SpawnPoolItems)
			{
				foreach (var pool in spawnPoolItem.Value)
				{
					if (Enum.TryParse(pool.Key, out CustomSpawnPool_Vine spawnPool))
					{
						if (!BiomeChecker.CheckBiome(spawnPool))
							continue;

						if (SpawnPoolControlPlugin.currentPool.ClearDefaultWeights)
						{
							__instance.spawns = new SpawnList();
							__instance.spawns.items = new List<SpawnEntry>(); 
						}

						if (ItemDatabase.TryGetItem(SpawnPoolControlPlugin.itemNameReverseLUT[spawnPoolItem.Key], out var item))
						{
							SpawnEntry spawnEntry = new SpawnEntry();
							spawnEntry.prefab = item.gameObject;
							spawnEntry.weight = pool.Value;
							__instance.spawns.items.Add(spawnEntry);
						}
					}
				}
			}
		}
	}
	
	// Governs mushroom spawns, including the ShroomBerry spawns.
	[HarmonyPatch(typeof(GroundPlaceSpawner))]
	public class GroundPlaceSpawnerPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch("SpawnItems", typeof(List<Transform>))]
		public static void SpawnItemsPrefix(GroundPlaceSpawner __instance, List<Transform> spawnSpots)
		{
			if (currentPool?.SpawnPoolItems == null)
				return;
			
			foreach (var spawnPoolItem in currentPool.SpawnPoolItems)
			{
				foreach (var pool in spawnPoolItem.Value)
				{
					if (Enum.TryParse(pool.Key, out CustomSpawnPool_MushroomCluster spawnPool))
					{
						if (!BiomeChecker.CheckBiome(spawnPool))
							continue;

						if (SpawnPoolControlPlugin.currentPool.ClearDefaultWeights)
						{
							__instance.spawns = new SpawnList();
							__instance.spawns.items = new List<SpawnEntry>(); 
						}

						if (ItemDatabase.TryGetItem(SpawnPoolControlPlugin.itemNameReverseLUT[spawnPoolItem.Key], out var item))
						{
							SpawnEntry spawnEntry = new SpawnEntry();
							spawnEntry.prefab = item.gameObject;
							spawnEntry.weight = pool.Value;
							__instance.spawns.items.Add(spawnEntry);
						}
					}
				}
			}
		}
	}

	// Handles a bunch of single item spawns, such as:
	// 	- remedy shroom/shelf fungus/magic beans
	//  - item spawns on beach (airplane crash stuff, shells)
	//  - backpacks
	//  - dynamite/scorpion spawns
	[HarmonyPatch(typeof(SingleItemSpawner))]
	public class SingleItemSpawnerPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch("TrySpawnItems")]
		public static bool TrySpawnItemsPrefix(SingleItemSpawner __instance)
		{
			if (currentPool?.SpawnPoolReplacements == null)
				return true;

			if (currentPool.SpawnPoolReplacements.TryGetValue(__instance.prefab.name, out string value) &&
				itemNameReverseLUT.TryGetValue(value, out var newItemID) &&
				ItemDatabase.TryGetItem(newItemID, out var newItem))
			{
				__instance.prefab = newItem.gameObject;
			}

			return true;
		}
	}

	// Technically governs any kind of Spawner, which really only includes the scroll
	// at the first three campfires. Luggages are technically Spawners too, but
	// we exclude them here.
	[HarmonyPatch(typeof(Spawner), "GetObjectsToSpawn")]
	public static class SpawnerPatch
	{	
		private static void Postfix(Spawner __instance, ref List<GameObject> __result)
		{
			if (__instance.GetType().FullName != "Spawner") { return; }
			if (currentPool?.SpawnPoolReplacements == null) { return; }

			for (int i = 0; i < __result.Count; i++)
			{
				if (currentPool.SpawnPoolReplacements.TryGetValue(__result[i].name, out string value) &&
					itemNameReverseLUT.ContainsKey(__result[i].name) &&
					itemNameReverseLUT.TryGetValue(value, out var newItemID) &&
					ItemDatabase.TryGetItem(newItemID, out var newItem))
				{
					__result[i] = newItem.gameObject;
				}
			}
		}
	}
} 

public class BiomeChecker
{
	public static bool CheckBiome<TEnum>(TEnum pool)
	{
		int currentSegment = Singleton<MapHandler>.Instance.currentSegment;
		int currentBiome = (int)Singleton<MapHandler>.Instance.segments[currentSegment].biome;

		int value = Convert.ToInt32(pool);

		if (Convert.ToInt32(value) < 0)
		{
			SpawnPoolControlPlugin.Log?.LogInfo("Using global override for pool type " + typeof(TEnum).Name);
			return true;
		}

		SpawnPoolControlPlugin.Log?.LogDebug($"BiomeChecker: checking value {value} versus current biome {currentBiome}");

		return Convert.ToInt32(value) == currentBiome;
	}
}
