using System;
using System.Reflection;
using BattleTech;
using Harmony;
using Newtonsoft.Json;

namespace InternalHeaters
{
    public class AssemblyPatch 
    {
        internal static Settings ModSettings = new Settings();
        internal static string ModDirectory;
        public static void Init(string directory, string settingsJSON)
        {
            ModDirectory = directory;
            try
            {
                ModSettings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                ModSettings = new Settings();
            }
            var harmony = HarmonyInstance.Create("com.joelmeador.ShopSeller");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
    
    [HarmonyPatch(typeof(Mech), "GetHeatSinkDissipation")]
    public static class Mech_GetHeatSinkDissipation_Patch
    {
        public static void Postfix(Mech __instance, ref float __result)
        {
            Logger.Debug($"Patching in\npreheated: {__result}");
            Logger.Debug($"hsc? {__instance.StatCollection.GetValue<int>("HeatSinkCapacity")}");
            // (float)base.Combat.Constants.Heat.InternalHeatSinkCount * base.Combat.Constants.Heat.DefaultHeatSinkDissipationCapacity;
            var additionalHeatSinks = __instance.MechDef.Chassis.Heatsinks;
            var heatSinkDissipation = __instance.Combat.Constants.Heat.DefaultHeatSinkDissipationCapacity;
            var additionalHeatSinkDissipation = (float) additionalHeatSinks * heatSinkDissipation;
            Logger.Debug($"additionalHeatSinks: {additionalHeatSinks}\nheatsinkDissipation: {heatSinkDissipation}\naddtionalHeatSinkDissipation: {additionalHeatSinkDissipation}");
            __result = __result + additionalHeatSinkDissipation;
            Logger.Debug($"reheated: {__result}");
        }
    }
}