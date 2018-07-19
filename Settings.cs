using System.Collections.Generic;

namespace InternalHeaters
{
    public class Settings
    {
        public const string ModName = "InternalHeaters";

        // allow non-exception logging
        public bool debug = false;

        // use the heatsinks property from chassis defition to add more heatsinking
        public bool useChassisHeatSinks = false;
        public bool UseChassisHeatSinks => useChassisHeatSinks;

        // if only items of the type defined by doubleHeatSinksDoubleEngineHeatDissipationComponentId,
        // double the base heat dissipation of the mech
        public bool allDoubleHeatSinksDoubleEngineHeatDissipation = true;
        public bool AllDoubleHeatSinksDoubleEngineHeatDissipation => allDoubleHeatSinksDoubleEngineHeatDissipation;

        // the component id(s) for the item(s) that makes double heat sinking happen
        // default is the vanilla DHS
        // The singular version is the pre-1.0 version of this.
        public string doubleHeatSinksDoubleEngineHeatDissipationComponentId
        {
            set => doubleHeatSinksDoubleEngineHeatDissipationComponentIds.Add(value);
        }
        public List<string> doubleHeatSinksDoubleEngineHeatDissipationComponentIds = new List<string> {"Gear_Heatsink_Generic_Double"};
        public List<string> DoubleHeatSinksDoubleEngineHeatDissipationComponentIds =>
            doubleHeatSinksDoubleEngineHeatDissipationComponentIds;

        // enable this to remove the Heatsinking properties of the first heatsink item used to
        // create double heatsinking of the engine. e.g. if one DHS is installed, then the mech dissipates 60 heat insteat of 66
        public bool doNotCountFirstDoubleHeatSinksComponentDissipation = false;
        public bool DoNotCountFirstDoubleHeatSinksComponentDissipation => doNotCountFirstDoubleHeatSinksComponentDissipation;
    }
}