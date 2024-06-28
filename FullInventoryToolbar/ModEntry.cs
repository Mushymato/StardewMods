using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Reflection.Emit;
using StardewValley.GameData.HomeRenovations;
using System.Reflection;


namespace FullInventoryToolbar
{
    public enum ToolbarArrangement
    {
        Vanilla = 0,
        H_213 = 1,
        H_312 = 2,
        V_123 = 3,
    }
    public class ModConfig
    {
        public ToolbarArrangement Arrangement = ToolbarArrangement.H_213;
        public bool HideToolbarItemBoxes = false;
        public bool HideToolbarBackground = false;

        public void Reset()
        {
            Arrangement = ToolbarArrangement.H_312;
            HideToolbarItemBoxes = false;
            HideToolbarBackground = false;
        }
    }
    internal sealed class ModEntry : Mod
    {
        private static IMonitor? mon;
        private const int ToolbarHeight = 72;
        private const int ToolbarWidth = 776;
        public static Texture2D? WhiteSquare;
        public static ModConfig? Config;
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            mon = Monitor;
            Harmony patcher = new(ModManifest.UniqueID);
            Patch(patcher); // need to patch immediately since Toolbar constructor is called super early

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        public static void Log(string msg, LogLevel level = LogLevel.Debug)
        {
            mon!.Log(msg, level);
        }


        /// <summary>
        /// Apply <see cref="GamePatches"/> on game launch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            SetupConfig();
        }


        private void SetupConfig()
        {
            if (Helper.ModRegistry.GetApi<Integration.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu") is Integration.IGenericModConfigMenuApi GMCM)
            {
                GMCM.Register(
                    mod: ModManifest,
                    reset: () =>
                    {
                        Config!.Reset();
                        Helper.WriteConfig(this);
                    },
                    save: () =>
                    {
                        Helper.WriteConfig(this);
                    },
                    titleScreenOnly: false
                );
                GMCM.AddNumberOption(
                    ModManifest,
                    getValue: () => { return (int)Config!.Arrangement; },
                    setValue: (value) => { Config!.Arrangement = (ToolbarArrangement)value; },
                    formatValue: (value) => { return Helper.Translation.Get($"config.ToolbarArrangement.{value}"); },
                    name: () => Helper.Translation.Get("config.ToolbarArrangement.name"),
                    tooltip: () => Helper.Translation.Get("config.ToolbarArrangement.description"),
                    min: 0, max: 3
                );
                GMCM.AddBoolOption(
                    ModManifest,
                    getValue: () => { return Config!.HideToolbarItemBoxes; },
                    setValue: (value) => { Config!.HideToolbarItemBoxes = value; },
                    name: () => Helper.Translation.Get("config.HideToolbarItemBoxes.name"),
                    tooltip: () => Helper.Translation.Get("config.HideToolbarItemBoxes.description")
                );
                GMCM.AddBoolOption(
                    ModManifest,
                    getValue: () => { return Config!.HideToolbarBackground; },
                    setValue: (value) => { Config!.HideToolbarBackground = value; },
                    name: () => Helper.Translation.Get("config.HideToolbarBackground.name"),
                    tooltip: () => Helper.Translation.Get("config.HideToolbarBackground.description")
                );
            }
            else
            {
                // Helper.WriteConfig(this);
            }
        }

        private static void Patch(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.DeclaredConstructor(typeof(Toolbar), Array.Empty<Type>()),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(Toolbar_constructor_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(Toolbar), nameof(Toolbar.gameWindowSizeChanged)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(Toolbar_gameWindowSizeChanged_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(Toolbar), nameof(Toolbar.isWithinBounds)),
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(Toolbar_isWithinBounds_Prefix))
                );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(Toolbar), nameof(Toolbar.draw)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(Toolbar_draw_Postfix)),
                transpiler: new HarmonyMethod(typeof(ModEntry), nameof(Toolbar_draw_Transpiler))
                );
            }
            catch (Exception err)
            {
                Log($"Failed to patch FullInventoryToolbar:\n{err}", LogLevel.Error);
            }
        }

        private static int AlignY(int yPositionOnScreen, int row)
        {
            return Config!.Arrangement switch
            {
                // simplified determination of toolbar top align-ness
                ToolbarArrangement.V_123 => ((yPositionOnScreen > Game1.uiViewport.Height / 2) ? -1 : 1) * row * ToolbarHeight,
                _ => 0,
            };
        }

        private static int AlignX(int row)
        {
            return Config!.Arrangement switch
            {
                ToolbarArrangement.H_213 => ((row % 2 == 1) ? -1 : 1) * ToolbarWidth,
                ToolbarArrangement.H_312 => ((row % 2 == 1) ? 1 : -1) * ToolbarWidth,
                _ => 0,
            };
        }

        private static int HighestItem()
        {
            int highestItem = 0;
            for (int i = Farmer.hotbarSize; i < Game1.player.maxItems.Value; i++)
            {
                if (Game1.player.Items.Count > i && Game1.player.Items[i] != null)
                    highestItem = i;
            }
            return highestItem;
        }

        private static void Toolbar_constructor_Postfix(Toolbar __instance)
        {
            try
            {
                // __instance.xPositionOnScreen -= ToolbarWidth;
                // __instance.width *= 3;
                for (int i = Farmer.hotbarSize; i < Farmer.maxInventorySpace; i++)
                {
                    __instance.buttons.Add(
                        new ClickableComponent(
                            new Rectangle(
                                Game1.uiViewport.Width / 2 - 384 + i % Farmer.hotbarSize * 64 + AlignX(i / Farmer.hotbarSize),
                                __instance.yPositionOnScreen - 96 + 8 + AlignY(__instance.yPositionOnScreen, i / Farmer.hotbarSize),
                                64, 64
                            ),
                            i.ToString() ?? ""
                        )
                    );
                }
            }
            catch (Exception err)
            {
                Log($"Error in Toolbar_constructor_Postfix:\n{err}", LogLevel.Error);
            }
        }

        private static void Toolbar_gameWindowSizeChanged_Postfix(Toolbar __instance, Rectangle oldBounds, Rectangle newBounds)
        {
            try
            {
                for (int i = Farmer.hotbarSize; i < __instance.buttons.Count; i++)
                {
                    UpdateButtonBounds(__instance, i);
                }
            }
            catch (Exception err)
            {
                Log($"Error in Toolbar_gameWindowSizeChanged_Postfix:\n{err}", LogLevel.Error);
            }
        }

        private static void UpdateButtonBounds(Toolbar tb, int i)
        {
            tb.buttons[i].bounds.X = Game1.uiViewport.Width / 2 - 384 + i % Farmer.hotbarSize * 64 + AlignX(i / Farmer.hotbarSize);
            tb.buttons[i].bounds.Y = tb.yPositionOnScreen - 96 + 8 + AlignY(tb.yPositionOnScreen, i / Farmer.hotbarSize);
        }

        private static bool Toolbar_isWithinBounds_Prefix(Toolbar __instance, ref bool __result, int x, int y)
        {
            try
            {
                Rectangle firstBounds;
                Rectangle lastBounds;
                switch (Config!.Arrangement)
                {
                    case ToolbarArrangement.H_213:
                        firstBounds = __instance.buttons[Farmer.hotbarSize].bounds;
                        lastBounds = __instance.buttons.Last().bounds;
                        break;
                    case ToolbarArrangement.H_312:
                        firstBounds = __instance.buttons[Farmer.hotbarSize * 2].bounds;
                        lastBounds = __instance.buttons[Farmer.hotbarSize * 2 - 1].bounds;
                        break;
                    case ToolbarArrangement.V_123:
                        if (__instance.yPositionOnScreen > Game1.uiViewport.Height / 2)
                        {
                            // toolbar on bottom
                            firstBounds = __instance.buttons[Farmer.hotbarSize * 2].bounds;
                            lastBounds = __instance.buttons[Farmer.hotbarSize - 1].bounds;
                        }
                        else
                        {
                            // toolbar on top
                            firstBounds = __instance.buttons.First().bounds;
                            lastBounds = __instance.buttons.Last().bounds;
                        }
                        break;
                    default:
                        firstBounds = __instance.buttons[0].bounds;
                        lastBounds = __instance.buttons[Farmer.hotbarSize - 1].bounds;
                        break;
                };
                Log($"firstBounds {firstBounds} lastBounds {lastBounds}");
                Rectangle tbBounds = new(
                    firstBounds.X, firstBounds.Y,
                    lastBounds.Right - firstBounds.Left,
                    lastBounds.Bottom - firstBounds.Top
                );
                __result = tbBounds.Contains(x, y);
                Log($"Check ({x}, {y}) in {tbBounds}? {__result}");
                return false;
            }
            catch (Exception err)
            {
                Log($"Error in Toolbar_gameWindowSizeChanged_Postfix:\n{err}", LogLevel.Error);
                return true;
            }
        }


        private static void Toolbar_draw_Postfix(Toolbar __instance, SpriteBatch b)
        {
            try
            {
                if (!(Config!.Arrangement == ToolbarArrangement.Vanilla))
                    DrawToolbarRows(__instance, b);
            }
            catch (Exception err)
            {
                Log($"Error in Toolbar_draw_Postfix:\n{err}", LogLevel.Error);
            }
        }

        private static void DrawToolbarBox(SpriteBatch b, Texture2D texture, Rectangle sourceRect, int x, int y, int width, int height, Color color, float scale = 1f, bool drawShadow = true, float draw_layer = -1f)
        {
            if (Config!.HideToolbarBackground)
                return;
            if (!(Config!.Arrangement == ToolbarArrangement.Vanilla))
            {
                if (Config!.Arrangement == ToolbarArrangement.V_123)
                {
                    height += ToolbarHeight * 2;
                    if (y > Game1.uiViewport.Height / 2)
                        y -= ToolbarHeight * 2;
                }
                else
                {
                    width += ToolbarWidth * 2;
                    x -= ToolbarWidth;
                }
            }
            IClickableMenu.drawTextureBox(b, texture, sourceRect, x, y, width, height, color, scale: scale, drawShadow: drawShadow, draw_layer: draw_layer);
        }

        private static void DrawItemBox(SpriteBatch b, Texture2D texture, Vector2 position, Rectangle? sourceRect, Color color)
        {
            if (Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 56) == sourceRect)
                b.Draw(texture, position, sourceRect, color);
        }

        private static void DrawToolbarRows(Toolbar tb, SpriteBatch b)
        {
            if (Game1.activeClickableMenu != null)
                return;

            mon!.LogOnce($"tooblar pos: {tb.xPositionOnScreen}, {tb.yPositionOnScreen}");
            mon!.LogOnce($"Farmer.hotbarSize: {Farmer.hotbarSize}");

            int highestItem = HighestItem();
            if (highestItem / Farmer.hotbarSize == 0)
                return;

            int i;
            for (i = Farmer.hotbarSize; i < Farmer.maxInventorySpace; i++)
            {
                UpdateButtonBounds(tb, i);
                Vector2 toDraw = new(Game1.uiViewport.Width / 2 - 384 + i % Farmer.hotbarSize * 64 + AlignX(i / Farmer.hotbarSize), tb.yPositionOnScreen - 96 + 8 + AlignY(tb.yPositionOnScreen, i / Farmer.hotbarSize));
                DrawItemBox(b, Game1.menuTexture, toDraw, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, (Game1.player.CurrentToolIndex == i) ? 56 : 10), Color.White * tb.transparency);
            }

            for (i = Farmer.hotbarSize; i < (highestItem + 1); i++)
            {
                if (Game1.player.Items.Count > i && Game1.player.Items[i] != null)
                {
                    tb.buttons[i].scale = Math.Max(1f, tb.buttons[i].scale - 0.025f);
                    Vector2 toDraw2 = new(Game1.uiViewport.Width / 2 - 384 + i % Farmer.hotbarSize * 64 + AlignX(i / Farmer.hotbarSize), tb.yPositionOnScreen - 96 + 8 + AlignY(tb.yPositionOnScreen, i / Farmer.hotbarSize));
                    Game1.player.Items[i].drawInMenu(b, toDraw2, (Game1.player.CurrentToolIndex == i) ? 0.9f : (tb.buttons[i].scale * 0.8f), tb.transparency, 0.88f);
                }
            }

            if (tb.hoverItem != null)
            {
                IClickableMenu.drawToolTip(b, tb.hoverItem.getDescription(), tb.hoverItem.DisplayName, tb.hoverItem);
                tb.hoverItem = null;
            }
        }

        private static IEnumerable<CodeInstruction> Toolbar_draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                // hijack IClickableMenu.drawTextureBox call
                matcher = matcher.Start()
                .MatchStartForward(new CodeMatch[]{
                    new(
                        OpCodes.Call,
                        AccessTools.Method(
                            typeof(IClickableMenu), nameof(IClickableMenu.drawTextureBox),
                            new Type[]{
                                typeof(SpriteBatch), typeof(Texture2D), typeof(Rectangle),
                                typeof(int), typeof(int), typeof(int), typeof(int),
                                typeof(Color), typeof(float), typeof(bool), typeof(float)
                            }
                        )
                    ),
                    new(OpCodes.Ldc_I4_0)
                })
                ;
                matcher.Instruction.operand = AccessTools.Method(typeof(ModEntry), nameof(DrawToolbarBox));

                // replace drawing of item boxes
                matcher = matcher
                .MatchStartForward(new CodeMatch[]{
                    new(OpCodes.Callvirt, AccessTools.Method(
                        typeof(SpriteBatch), nameof(SpriteBatch.Draw),
                        new Type[]{
                            typeof(Texture2D), typeof(Vector2),
                            typeof(Rectangle?), typeof(Color)
                        }
                    )),
                    new(OpCodes.Call),
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(Options), nameof(Options.gamepadControls)))
                })
                .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(DrawItemBox))));

                // block drawing of hoverItem, to let draw postfix handle it
                matcher = matcher
                .MatchStartForward(new CodeMatch[]{
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(Toolbar), nameof(Toolbar.hoverItem))),
                    new(OpCodes.Brfalse_S)
                })
                ;

                matcher.Instruction.opcode = OpCodes.Nop;
                matcher.Instruction.operand = null;
                matcher.Advance(1);
                matcher.Instruction.opcode = OpCodes.Nop;
                matcher.Instruction.operand = null;
                matcher.Advance(1);
                matcher.Instruction.opcode = OpCodes.Br_S;

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                Log($"Error in Toolbar_draw_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }
    }
}
