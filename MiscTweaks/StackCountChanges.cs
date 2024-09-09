using HarmonyLib;


namespace MiscTweaks
{
    internal static class StackCountChanges
    {
        public static void Patch(Harmony patcher)
        {
            // patcher.Patch(
            //     original: AccessTools.Method(typeof(Item), nameof(Item.CompareTo)),
            //     prefix: new HarmonyMethod(AccessTools.Method(typeof(AtravitaItemSortTweak), nameof(Item_CompareTo_Prefix)))
            // );
        }
    }
}