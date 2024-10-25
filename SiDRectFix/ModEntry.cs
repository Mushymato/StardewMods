using StardewModdingAPI;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework.Graphics;
using HarmonyLib;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using StardewValley.GameData.Characters;
using StardewValley;

namespace SiDRectFix
{
    public class ModEntry : Mod
    {
        private static IMonitor mon = null!;
        private static Type replacedTextureType = null!;
        private static Harmony harmony = null!;
        private static readonly Dictionary<string, CharacterData> SiDnpcs = [];

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
        }

        private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            if (e.NamesWithoutLocale.Any(an => an.IsEquivalentTo("Data/Characters")))
            {
                SiDnpcs.Clear();
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.Name.StartsWith("Characters/") && e.DataType == typeof(Texture2D))
            {
                e.Edit((asset) =>
                {
                    if (asset.AsImage().Data.GetType().FullName == "SpritesInDetail.ReplacedTexture")
                    {
                        string charaName = e.Name.BaseName.Split("/").Last();
                        if (Game1.characterData.TryGetValue(charaName, out CharacterData? charData))
                        {
                            SiDnpcs[e.Name.BaseName] = charData;
                            if (harmony == null)
                            {
                                replacedTextureType = asset.AsImage().Data.GetType();
                                harmony = new Harmony(ModManifest.UniqueID);
                                DoPatches(harmony);
                            }
                        }
                    }
                });
            }
        }

        private void DoPatches(Harmony harmony)
        {
            try
            {
                // harmony.Patch(
                //     original: AccessTools.DeclaredMethod(typeof(SocialPage), nameof(SocialPage.CreateSpriteComponent))
                // );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(SpritesInDetail.ModEntry), nameof(SpritesInDetail.ModEntry.DrawReplacedTexture)),
                    // prefix: new HarmonyMethod(typeof(ModEntry), nameof(SiD_DrawReplacedTexture_Prefix))
                    transpiler: new HarmonyMethod(typeof(ModEntry), nameof(SiD_DrawReplacedTexture_Transpiler))
                );
            }
            catch (Exception err)
            {
                mon.Log($"Failed to patch SiDRectFix:\n{err}", LogLevel.Error);
            }
        }

        private static object SiD_DrawReplacedTexture_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                matcher.End()
                .MatchStartBackwards([
                    new(OpCodes.Callvirt, AccessTools.Method(
                        typeof(SpriteBatch), nameof(SpriteBatch.Draw),
                        [typeof(Texture2D), typeof(Rectangle), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(SpriteEffects), typeof(float)]
                    ))
                ])
                .ThrowIfNotMatch("Failed to find last SpriteBatch.Draw");
                // matcher.Operand = AccessTools.DeclaredMethod(typeof(ModEntry), nameof(SpriteBatch_Draw));
                matcher.Opcode = OpCodes.Call;
                matcher.Operand = AccessTools.DeclaredMethod(typeof(ModEntry), nameof(SpriteBatch_Draw));
                // magic knowledge
                matcher.Insert([
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(replacedTextureType, "HDTextureInfo"))
                ]);

                // foreach (var inst in matcher.Instructions())
                // {
                //     Console.WriteLine(inst);
                // }

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                mon.Log($"Error in SiD_DrawReplacedTexture_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }

        // callvirt System.Void Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D texture, Microsoft.Xna.Framework.Rectangle destinationRectangle, System.Nullable`1<Microsoft.Xna.Framework.Rectangle> sourceRectangle, Microsoft.Xna.Framework.Color color, System.Single rotation, Microsoft.Xna.Framework.Vector2 origin, Microsoft.Xna.Framework.Graphics.SpriteEffects effects, System.Single layerDepth)
        private static void SpriteBatch_Draw(SpriteBatch b, Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth, SpritesInDetail.HDTextureInfo hdTxInfo)
        {
            if (SiDnpcs.TryGetValue(hdTxInfo.Target, out CharacterData? data) && sourceRectangle is Rectangle sourceRect)
            {
                bool adjusted = false;
                bool isSocial = origin.X == 32 && origin.Y == 55;
                bool isCalendar = origin.X == 16 && origin.Y == 34;
                int targetHeight = 0;
                mon.LogOnce($"SpriteBatch_Draw {hdTxInfo.Target} - destinationRectangle: {destinationRectangle} sourceRectangle {sourceRectangle} origin {origin}", LogLevel.Info);
                if (isSocial &&
                    (data.CustomFields?.TryGetValue("mushymato.SiDRectFix/SocialHeight", out string? socialHeightStr) ?? false) &&
                    int.TryParse(socialHeightStr, out int socialHeight))
                {
                    targetHeight = socialHeight;
                }
                else if (isCalendar &&
                    (data.CustomFields?.TryGetValue("mushymato.SiDRectFix/CalendarHeight", out string? calendarHeightStr) ?? false) &&
                    int.TryParse(calendarHeightStr, out int calendarHeight))
                {
                    targetHeight = calendarHeight;
                }

                if (targetHeight > 0)
                {
                    // adjust to target height
                    origin.Y -= (sourceRect.Height - targetHeight) / 2;
                    sourceRect.Height = targetHeight;
                    destinationRectangle.X += (destinationRectangle.Width - (int)(origin.X + sourceRect.Width)) / 2;
                    destinationRectangle.Width = (int)(origin.X + sourceRect.Width);
                    destinationRectangle.Y += (destinationRectangle.Height - (int)(origin.Y + sourceRect.Height)) / 2;
                    destinationRectangle.Height = (int)(origin.Y + sourceRect.Height);
                    if (isCalendar)
                        destinationRectangle.Y += 24;
                    adjusted = true;
                    mon.LogOnce($"Modified {hdTxInfo.Target} - destinationRectangle: {destinationRectangle} sourceRectangle {sourceRectangle} origin {origin}", LogLevel.Info);
                }

                if (adjusted)
                {
                    b.Draw(texture, destinationRectangle, sourceRect, color, rotation, origin, effects, layerDepth);
                    return;
                }
            }
            b.Draw(texture, destinationRectangle, sourceRectangle, color, rotation, origin, effects, layerDepth);
        }
    }
}
