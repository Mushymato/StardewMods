using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace DetailedCalendar;

public sealed class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif

    public const string ModId = "mushymato.DetailedCalendar";
    private const string BillboardTexture = "LooseSprites\\Billboard";
    private static IMonitor? mon;

    private (Color, Color)? uiColors;
    internal (Color, Color) UIColors
    {
        get
        {
            if (uiColors.HasValue)
            {
                return uiColors.Value;
            }
            Texture2D bbTx = Game1.temporaryContent.Load<Texture2D>(BillboardTexture);
            Color[] pxData = new Color[bbTx.Width * bbTx.Height];
            bbTx.GetData(pxData);
            Color bgColor = pxData[bbTx.Width * 277 + 67];
            Color edColor = pxData[bbTx.Width * 279 + 69];
            uiColors = new(bgColor, edColor);
            return uiColors.Value;
        }
    }

    private DetailedDisplay? currentDisplay = null;
    private DetailedDisplay? CurrentDisplay
    {
        get => currentDisplay;
        set
        {
            if (value == null)
            {
                currentDisplay?.Dispose();
            }
            currentDisplay = value;
        }
    }

    internal static readonly FieldInfo dailyQuestBoardField = typeof(Billboard).GetField(
        "dailyQuestBoard",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;
    internal static readonly FieldInfo hoverTextField = typeof(Billboard).GetField(
        "hoverText",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    public override void Entry(IModHelper helper)
    {
        if (dailyQuestBoardField == null || hoverTextField == null)
            throw new RuntimeWrappedException("Failed to reflect Billboard");
        I18n.Init(helper.Translation);
        mon = Monitor;

        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Content.AssetsInvalidated += OnAssetInvalidated;
    }

    private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(nm => nm.IsEquivalentTo(BillboardTexture)))
        {
            uiColors = null;
        }
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        CurrentDisplay = null;
        if (e.NewMenu is Billboard billboard && !(bool)dailyQuestBoardField.GetValue(billboard)!)
        {
            Helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        }
        else
        {
            Helper.Events.Input.ButtonsChanged -= OnButtonsChanged;
        }
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (Game1.didPlayerJustLeftClick())
        {
            if (Game1.activeClickableMenu is not Billboard billboard)
                return;
            if (CurrentDisplay is null)
            {
                Vector2 screenPx = e.Cursor.GetScaledScreenPixels();
                if (
                    billboard.calendarDays.FirstOrDefault(cc => cc.bounds.Contains(screenPx))
                        is ClickableTextureComponent chosenDayCC
                    && billboard.calendarDayData.TryGetValue(chosenDayCC.myID, out Billboard.BillboardDay? chosenDay)
                    && chosenDay.Events.Any()
                )
                {
                    CurrentDisplay = new DetailedDisplay(Helper, UIColors, billboard, chosenDay, chosenDayCC.myID);
                }
            }
            else
            {
                CurrentDisplay = null;
            }
        }
    }

    /// <summary>SMAPI static monitor Log wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }

    /// <summary>SMAPI static monitor LogOnce wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.LogOnce(msg, level);
    }
}
