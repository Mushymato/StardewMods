using System.Reflection.Emit;
using CueSwapGenerator;
using HarmonyLib;
using StardewValley;
using StardewValley.Buildings;

namespace CueSwap;

[CueSwapTranspiler(
    nameof(OpCodes.Callvirt),
    nameof(GameLocation),
    nameof(GameLocation.localSound),
    "doorCreak",
    "doorCreak.ShippingBin"
)]
[CueSwapTranspiler(
    nameof(OpCodes.Callvirt),
    nameof(GameLocation),
    nameof(GameLocation.localSound),
    "doorCreakReverse",
    "doorCreakReverse.ShippingBin"
)]
[CueSwapTranspiler(
    nameof(OpCodes.Call),
    nameof(DelayedAction),
    nameof(DelayedAction.playSoundAfterDelay),
    "Ship",
    "Ship.ShippingBin"
)]
internal static partial class Patches
{
    internal static void Patch(string modId)
    {
        Harmony harmony = new(modId);
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(ShippingBin), "openShippingBinLid"),
            transpiler: new HarmonyMethod(
                typeof(Patches),
                nameof(TP_GameLocation_localSound_doorCreak_doorCreakShippingBin)
            )
        );
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(ShippingBin), "closeShippingBinLid"),
            transpiler: new HarmonyMethod(
                typeof(Patches),
                nameof(TP_GameLocation_localSound_doorCreakReverse_doorCreakReverseShippingBin)
            )
        );
        harmony.Patch(
            original: AccessTools.DeclaredMethod(
                typeof(ShippingBin),
                nameof(ShippingBin.showShipment)
            ),
            transpiler: new HarmonyMethod(
                typeof(Patches),
                nameof(TP_DelayedAction_playSoundAfterDelay_Ship_ShipShippingBin)
            )
        );
    }
}
