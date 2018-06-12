namespace InternalHeaters
{
    public class Settings
    {
        public const string ModName = "InternalHeaters";

        public bool debug = false;

        public bool useChassisHeatSinks = false;
        public bool UseChassisHeatSinks => useChassisHeatSinks;

        public bool allDoubleHeatSinksDoubleEngineHeatDissipation = false;
        public bool AllDoubleHeatSinksDoubleEngineHeatDissipation => allDoubleHeatSinksDoubleEngineHeatDissipation;
    }
}