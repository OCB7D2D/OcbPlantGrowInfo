using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class OcbPlantGrowInfo : IModApi
{

    // Entry class for A20 patching
    public void InitMod(Mod mod)
    {
        Log.Out("Loading OCB Plant Growth Info Patch: " + GetType().ToString());
        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    // Programmatically patch block.xml
    [HarmonyPatch(typeof(Block))]
    [HarmonyPatch("AssignIds")]
    public class Block_AssignIds
    {
        static void Postfix()
        {
            foreach (Block block in Block.list)
            {
                if (block is BlockPlant)
                {
                    // Patch display info for plants (show more data)
                    block.DisplayInfo = Block.EnumDisplayInfo.Custom;
                    block.Properties.Values["RemoteDescription"] = "true";
                }
            }
        }
    }

    [HarmonyPatch(typeof(Block))]
    [HarmonyPatch("GetCustomDescription")]
    public class Block_GetCustomDescription
    {

        static readonly FieldInfo FieldScheduledTicksDict = AccessTools
            .Field(typeof(WorldBlockTicker), "scheduledTicksDict");

        static readonly FieldInfo FieldLightLevelGrow = AccessTools
            .Field(typeof(BlockPlantGrowing), "lightLevelGrow");

        static readonly FieldInfo FieldLightLevelStay = AccessTools
            .Field(typeof(BlockPlant), "lightLevelStay");

        public static bool Prefix(
            Vector3i _blockPos,
            BlockValue _bv,
            ref string __result)
        {
            if (_bv.Block is BlockPlantGrowing plant)
            {
                __result = _bv.Block.GetLocalizedBlockName();
                if (GameManager.Instance.World is WorldBase world)
                {
                    // This returns null on dedicated clients
                    if (world.GetWBT() is WorldBlockTicker ticker)
                    {
                        if (FieldScheduledTicksDict.GetValue(ticker) is
                            Dictionary<int, WorldBlockTickerEntry> scheduled)
                        {
                            var hash = WorldBlockTickerEntry.ToHashCode(0, _blockPos, _bv.type);
                            if (scheduled.TryGetValue(hash, out WorldBlockTickerEntry entry))
                            {
                                // Get an estimation how much progress until next tick
                                ulong rest = entry.scheduledTime - GameTimer.Instance.ticks;
                                __result += string.Format("\n" +
                                    Localization.Get("plantProgress"),
                                    100 - 100 * rest / plant.GetTickRate(),
                                    rest, plant.GetTickRate());
                                // Check light levels to indicate if plant can grow or not
                                var light = world.GetBlockLightValue(0, _blockPos);
                                byte lightSun = (byte)(light & 15);
                                // byte lightBlock = (byte)(light >> 4 & 15);
                                string localization = "plantWithering";
                                int levelStay = (int)FieldLightLevelStay.GetValue(plant);
                                int levelGrow = (int)FieldLightLevelGrow.GetValue(plant);
                                if (lightSun >= levelGrow) localization = "plantGrowing";
                                else if (lightSun >= levelStay) localization = "plantStaying";
                                __result += string.Format("\n" +
                                    Localization.Get(localization),
                                    lightSun, levelStay, levelGrow);
                            }
                        }
                    }
                }
                return false;
            }
            return true;
        }
    }

}
