using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace DetailedCalendar;

internal sealed record BirthdayEvent(Rectangle Bounds, Billboard.BillboardEvent BBEvent, Item? Favorite);

// not an actual menu more of a funny overlay deal
internal sealed class DetailedDisplay : IDisposable
{
    // args
    internal readonly IModHelper helper;
    internal (Color, Color) uiColors;
    internal readonly Billboard billboard;
    internal readonly Billboard.BillboardDay chosenDay;
    internal readonly int chosenDayNum;

    // state
    internal readonly List<ClickableTextureComponent> calendarDays;
    internal float mouseCursorTransparency = 0f;
    internal string? hoverText = null;

    // derived
    internal readonly Rectangle cellBounds;
    internal readonly Rectangle headerBounds;
    internal readonly Rectangle displayBounds;
    internal readonly Point topLeft;
    internal List<BirthdayEvent> eventsBirthday = [];

    public DetailedDisplay(
        IModHelper helper,
        (Color, Color) uiColors,
        Billboard billboard,
        Billboard.BillboardDay chosenDay,
        int chosenDayNum
    )
    {
        this.helper = helper;
        this.uiColors = uiColors;
        this.billboard = billboard;
        this.chosenDay = chosenDay;
        this.chosenDayNum = chosenDayNum;

        this.calendarDays = billboard.calendarDays;
        this.cellBounds = calendarDays.First().bounds;
        displayBounds = calendarDays.First().bounds;

        foreach (ClickableTextureComponent calendarDay in calendarDays)
        {
            displayBounds = Rectangle.Union(displayBounds, calendarDay.bounds);
        }
        topLeft = new(displayBounds.X, displayBounds.Y);
        headerBounds = new(displayBounds.X, displayBounds.Y - 64, displayBounds.Width, 60);

        FillEventBounds(chosenDay);

        Setup(helper, billboard);
    }

    private void Setup(IModHelper helper, Billboard billboard)
    {
        billboard.calendarDays = [];
        helper.Events.Display.RenderingActiveMenu += OnRenderingActiveMenu;
        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        helper.Events.Input.CursorMoved += OnCursorMoved;
    }

    public void Dispose()
    {
        billboard.calendarDays = calendarDays;
        helper.Events.Display.RenderingActiveMenu -= OnRenderingActiveMenu;
        helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
        helper.Events.Input.CursorMoved -= OnCursorMoved;
    }

    private void FillEventBounds(Billboard.BillboardDay chosenDay)
    {
        // birthdays
        Point bdayPnt = new(displayBounds.X, displayBounds.Y);
        foreach (
            Billboard.BillboardEvent bbEvent in chosenDay.Events.Where(e =>
                e.Texture != null && e.Type == Billboard.BillboardEventType.Birthday
            )
        )
        {
            eventsBirthday.Add(
                new(
                    new Rectangle(bdayPnt.X, bdayPnt.Y, cellBounds.Width, cellBounds.Height),
                    bbEvent,
                    Game1.getCharacterFromName(bbEvent.Arguments[0])?.getFavoriteItem()
                )
            );
            bdayPnt.X += cellBounds.Width;
        }
    }

    private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
        hoverText = "";
        Vector2 screenPx = e.NewPosition.GetScaledScreenPixels();
        foreach (BirthdayEvent bd in eventsBirthday)
        {
            if (bd.Bounds.Contains(screenPx))
            {
                hoverText = bd.BBEvent.DisplayName;
                break;
            }
        }
    }

    private void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e)
    {
        mouseCursorTransparency = Game1.mouseCursorTransparency;
        Game1.mouseCursorTransparency = 0f;
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        SpriteBatch b = e.SpriteBatch;

        Utility.DrawSquare(b, headerBounds, 0, backgroundColor: uiColors.Item1);
        b.DrawString(
            Game1.smallFont,
            chosenDayNum.ToString(),
            new(headerBounds.X + 16, headerBounds.Y),
            uiColors.Item2,
            0,
            Vector2.Zero,
            2,
            SpriteEffects.None,
            0.9f
        );
        Utility.DrawSquare(b, displayBounds, 0, backgroundColor: uiColors.Item1);

        // birthdays
        foreach (BirthdayEvent bd in eventsBirthday)
        {
            if (bd.Favorite is Item fav)
            {
                fav.drawInMenu(
                    b,
                    new(bd.Bounds.X, bd.Bounds.Y),
                    0.75f,
                    1f,
                    0.9f,
                    StackDrawType.Draw,
                    Color.White,
                    drawShadow: false
                );
            }
            if (bd.BBEvent.Texture != null)
            {
                b.Draw(
                    bd.BBEvent.Texture,
                    new(
                        bd.Bounds.X + bd.Bounds.Width / 2 - bd.BBEvent.TextureSourceRect.Width * 2,
                        bd.Bounds.Y + bd.Bounds.Height - bd.BBEvent.TextureSourceRect.Height * 4
                    ),
                    bd.BBEvent.TextureSourceRect,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    1f
                );
            }
        }
        Utility.drawLineWithScreenCoordinates(
            displayBounds.X,
            displayBounds.Y + cellBounds.Height,
            displayBounds.X + displayBounds.Width,
            displayBounds.Y + cellBounds.Height,
            b,
            uiColors.Item2,
            1,
            4
        );

        Game1.mouseCursorTransparency = mouseCursorTransparency;
        billboard.drawMouse(b);
        if (hoverText != null)
        {
            IClickableMenu.drawHoverText(b, hoverText, Game1.dialogueFont);
        }
    }
}
