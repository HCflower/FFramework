namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    public static class NumberFormatter
    {
        public static string FormatDecimalPlaces(float value)
        {
            return value.ToString(value % 1 == 0 ? "F0" : "F2");
        }
    }
}