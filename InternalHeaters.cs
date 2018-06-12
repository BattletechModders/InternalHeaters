using System;
using System.Linq;
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

            var harmony = HarmonyInstance.Create($"com.joelmeador.{Settings.ModName}");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Mech), "GetHeatSinkDissipation")]
    public static class Mech_GetHeatSinkDissipation_Patch
    {
        public static void Postfix(Mech __instance, ref float __result)
        {
            var mech = __instance;
            Logger.Debug($"Patching in\npreheated: {__result}");
            Logger.Debug($"hsc? {__instance.StatCollection.GetValue<int>("HeatSinkCapacity")}");
            var extraEngineDissipation = 0f;
            if (AssemblyPatch.ModSettings.AllDoubleHeatSinksDoubleEngineHeatDissipation &&
                mech.allComponents
                    .FindAll(component => component.componentType == ComponentType.HeatSink)
                    .All(component => component.Description.Id == "Gear_HeatSink_Generic_Double"))
            {
                extraEngineDissipation = __instance.Combat.Constants.Heat.DefaultHeatSinkDissipationCapacity *
                                         __instance.Combat.Constants.Heat.InternalHeatSinkCount;
            }

            var additionalHeatSinks = 0;
            var heatSinkDissipation = 0f;

            if (AssemblyPatch.ModSettings.UseChassisHeatSinks)
            {
                additionalHeatSinks = __instance.MechDef.Chassis.Heatsinks;
                heatSinkDissipation = __instance.Combat.Constants.Heat.DefaultHeatSinkDissipationCapacity;
            }

            var additionalHeatSinkDissipation = (additionalHeatSinks * heatSinkDissipation) + extraEngineDissipation;
            Logger.Debug(
                $"additionalHeatSinks: {additionalHeatSinks}\nheatsinkDissipation: {heatSinkDissipation}\naddtionalHeatSinkDissipation: {additionalHeatSinkDissipation}\nextraEngineDissipation: {extraEngineDissipation}");
            __result = __result + additionalHeatSinkDissipation;
            Logger.Debug($"reheated: {__result}");
        }
    }
}