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

    public static class Calculators
    {
        private const string DoubleHeatSinkComponentId = "Gear_HeatSink_Generic_Double";
        public static float DoubleHeatsinkEngineDissipation(MechDef mechDef, HeatConstantsDef heatConstants)
        {
            var heatsinks = Array.FindAll(mechDef.Inventory,
                componentRef => componentRef.ComponentDefType == ComponentType.HeatSink);
            if (AssemblyPatch.ModSettings.AllDoubleHeatSinksDoubleEngineHeatDissipation &&
                heatsinks.Any(componentRef => componentRef.ComponentDefID == DoubleHeatSinkComponentId) &&
                heatsinks.All(componentRef => componentRef.ComponentDefID == DoubleHeatSinkComponentId))
            {
                return heatConstants.DefaultHeatSinkDissipationCapacity * heatConstants.InternalHeatSinkCount;
            }
            return 0f;
        }
    }

    [HarmonyPatch(typeof(Mech), "GetHeatSinkDissipation")]
    public static class Mech_GetHeatSinkDissipation_Patch
    {
        public static void Postfix(Mech __instance, ref float __result)
        {
            var mech = __instance;
            Logger.Debug($"Patching in\npreheated: {__result}");
            Logger.Debug($"hsc? {mech.StatCollection.GetValue<int>("HeatSinkCapacity")}");
            var extraEngineDissipation = Calculators.DoubleHeatsinkEngineDissipation(mech.ToMechDef(), mech.Combat.Constants.Heat);
            var additionalHeatSinks = 0;
            var heatSinkDissipation = 0f;
            if (AssemblyPatch.ModSettings.UseChassisHeatSinks)
            {
                additionalHeatSinks = mech.MechDef.Chassis.Heatsinks;
                heatSinkDissipation = mech.Combat.Constants.Heat.DefaultHeatSinkDissipationCapacity;
            }

            var additionalHeatSinkDissipation = (additionalHeatSinks * heatSinkDissipation) + extraEngineDissipation;
            Logger.Debug(
                $"additionalHeatSinks: {additionalHeatSinks}\nheatsinkDissipation: {heatSinkDissipation}\naddtionalHeatSinkDissipation: {additionalHeatSinkDissipation}\nextraEngineDissipation: {extraEngineDissipation}");
            __result = __result + additionalHeatSinkDissipation;
            Logger.Debug($"reheated: {__result}");
        }
    }
}