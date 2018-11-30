using System;
using System.Linq;
using System.Reflection;
using BattleTech;
using Harmony;
using Newtonsoft.Json;
using UnityEngine;
using static InternalHeaters.AssemblyPatch;

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
        public static float DoubleHeatsinkEngineDissipation(MechDef mechDef, HeatConstantsDef heatConstants)
        {
            var heatsinks = Array.FindAll(mechDef.Inventory,
                componentRef => componentRef.ComponentDefType == ComponentType.HeatSink);
            if (ModSettings.AllDoubleHeatSinksDoubleEngineHeatDissipation &&
                heatsinks.Any(componentRef => ModSettings.DoubleHeatSinksDoubleEngineHeatDissipationComponentIds.Contains(componentRef.ComponentDefID)) &&
                heatsinks.All(componentRef => ModSettings.DoubleHeatSinksDoubleEngineHeatDissipationComponentIds.Contains(componentRef.ComponentDefID)))
            {
                var componentHeatRemoval = 0f;
                if (ModSettings.DoNotCountFirstDoubleHeatSinksComponentDissipation)
                {
                    var component = (HeatSinkDef)heatsinks.First(componentRef =>
                        ModSettings.DoubleHeatSinksDoubleEngineHeatDissipationComponentIds.Contains(componentRef.ComponentDefID)
                    ).Def;
                    componentHeatRemoval = component.DissipationCapacity;
                }
                return (heatConstants.DefaultHeatSinkDissipationCapacity * heatConstants.InternalHeatSinkCount) - componentHeatRemoval;
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
            if (ModSettings.UseChassisHeatSinks)
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

    // Stuff below is copied almost directly from https://github.com/CptMoore/StatsFixMod
    [HarmonyPatch(typeof(MechStatisticsRules), "CalculateHeatEfficiencyStat")]
    public static class MechStatisticsRulesCalculateHeatEfficiencyStatPatch
    {
        private static CombatGameConstants Combat;

        public static bool Prefix(MechDef mechDef, ref float currentValue, ref float maxValue)
        {
            try
            {
                if (Combat == null)
                {
                    Combat = CombatGameConstants.GetInstance(UnityGameInstance.BattleTechGame);
                }
                var baseHeatSinkDissipation = Combat.Heat.InternalHeatSinkCount * Combat.Heat.DefaultHeatSinkDissipationCapacity;
                var totalHeatSinkDissipation = baseHeatSinkDissipation + Calculators.DoubleHeatsinkEngineDissipation(mechDef, Combat.Heat);
                var heatGenerationWeapons = 0f;
                var numberOfJumpJets = 0;

                foreach (var mechComponentRef in mechDef.Inventory)
                {
                    if (mechComponentRef.Def == null)
                    {
                        mechComponentRef.RefreshComponentDef();
                    }
                    if (mechComponentRef.Def is WeaponDef)
                    {
                        var weaponDef = (WeaponDef)mechComponentRef.Def;
                        heatGenerationWeapons += weaponDef.HeatGenerated;
                    }
                    else if (mechComponentRef.ComponentDefType == ComponentType.JumpJet)
                    {
                        if (mechComponentRef.DamageLevel < ComponentDamageLevel.NonFunctional)
                        {
                            numberOfJumpJets++;
                        }
                    }
                    else if (mechComponentRef.Def is HeatSinkDef)
                    {
                        var heatSinkDef = (HeatSinkDef)mechComponentRef.Def;
                        totalHeatSinkDissipation += heatSinkDef.DissipationCapacity;
                    }
                }

                Logger.Debug($"heatGenerationWeapons: {heatGenerationWeapons}");
                Logger.Debug($"totalHeatSinkDissipation: {totalHeatSinkDissipation}");

                var maxHeat = Combat.Heat.MaxHeat;
                {
                    var stats = new StatCollection();
                    var maxHeatStatistic = stats.AddStatistic("MaxHeat", maxHeat);
                    var heatGeneratedStatistic = stats.AddStatistic("HeatGenerated", heatGenerationWeapons);

                    foreach (var mechComponentRef in mechDef.Inventory)
                    {
                        if (mechComponentRef.Def == null || mechComponentRef.Def.statusEffects == null)
                        {
                            continue;
                        }

                        var statusEffects = mechComponentRef.Def.statusEffects;
                        foreach (var effect in statusEffects)
                        {
                            switch (effect.statisticData.statName)
                            {
                                case "MaxHeat":
                                    stats.PerformOperation(maxHeatStatistic, effect.statisticData);
                                    break;
                                case "HeatGenerated":
                                    if (effect.statisticData.targetCollection == StatisticEffectData.TargetCollection.Weapon)
                                    {
                                        stats.PerformOperation(heatGeneratedStatistic, effect.statisticData);
                                    }
                                    break;
                            }
                        }
                    }
                    
                    maxHeat = maxHeatStatistic.CurrentValue.Value<int>();
                    heatGenerationWeapons = heatGeneratedStatistic.CurrentValue.Value<float>();
                }

                Logger.Debug($"maxHeat: {maxHeat}");
                Logger.Debug($"heatGenerationWeapons: {heatGenerationWeapons}");

                if (numberOfJumpJets >= Combat.MoveConstants.MoveTable.Length)
                {
                    numberOfJumpJets = Combat.MoveConstants.MoveTable.Length - 1;
                }

                var heatGenerationJumpJets = 0f;
                var jumpHeatDivisor = 3;
                if (numberOfJumpJets > 0)
                {
                    heatGenerationJumpJets += numberOfJumpJets * Combat.Heat.JumpHeatUnitSize / jumpHeatDivisor;
                }
                else
                {
                    heatGenerationJumpJets = 0f;
                }

                totalHeatSinkDissipation *= Combat.Heat.GlobalHeatSinkMultiplier;
                var totalHeatGeneration = (heatGenerationWeapons + heatGenerationJumpJets) * Combat.Heat.GlobalHeatIncreaseMultiplier;

                Logger.Debug($"totalHeatGeneration: {totalHeatGeneration}");

                // rounding steps for heatSinkDissipation
                var heatDissipationPercent = Mathf.Min(totalHeatSinkDissipation / totalHeatGeneration * 100f, UnityGameInstance.BattleTechGame.MechStatisticsConstants.MaxHeatEfficiency);
                heatDissipationPercent = Mathf.Max(heatDissipationPercent, UnityGameInstance.BattleTechGame.MechStatisticsConstants.MinHeatEfficiency);

                Logger.Debug($"heatDissipationPercent: {heatDissipationPercent}");

                totalHeatSinkDissipation = totalHeatGeneration * (heatDissipationPercent / 100f);

                Logger.Debug($"totalHeatSinkDissipation: {totalHeatSinkDissipation}");

                var heatLeftOver = totalHeatGeneration - totalHeatSinkDissipation;
                var unusedHeatCapacity = maxHeat - heatLeftOver;

                Logger.Debug($"heatLeftOver: {heatLeftOver}");
                Logger.Debug($"unusedHeatCapacity: {unusedHeatCapacity}");

                currentValue = Mathf.Round((unusedHeatCapacity / maxHeat) * 10f);
                currentValue = Mathf.Max(Mathf.Min(currentValue, 10f), 1f);
                maxValue = 10f;
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return true;
            }
        }
    }

    internal static class Utility
    {
        internal static void PerformOperation(this StatCollection collection, Statistic statistic,
            StatisticEffectData data)
        {
            var type = Type.GetType(data.modType);
            var variant = new Variant(type);
            variant.SetValue(data.modValue);
            variant.statName = data.statName;
            collection.PerformOperation(statistic, data.operation, variant);
        }
    }
}