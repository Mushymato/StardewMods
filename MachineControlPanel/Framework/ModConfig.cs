using MachineControlPanel.Framework.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace MachineControlPanel.Framework
{
    // Adds a button in GMCM, credit to ichortower
    // https://github.com/ichortower/Nightshade/blob/dev/src/GMCM.cs#L79
    internal class OpenMenuButton(Func<MachineMenu> getMachineSelectMenu)
    {
        private bool mouseLastFrame = false;
        private readonly string notInGame = I18n.Config_OpenMachineSelectMenu_Description();

        public void Draw(SpriteBatch b, Vector2 origin)
        {
            if (Game1.gameMode == Game1.playingGameMode)
            {
                origin.Y -= 4;
                bool mouseThisFrame =
                    Game1.input.GetMouseState().LeftButton == ButtonState.Pressed ||
                    Game1.input.GetGamePadState().IsButtonDown(Buttons.A);
                bool justClicked = mouseThisFrame && !mouseLastFrame;
                mouseLastFrame = mouseThisFrame;
                int mouseX = Game1.getMouseX();
                int mouseY = Game1.getMouseY();
                Rectangle bounds = new((int)origin.X, (int)origin.Y, 80, 80);
                bool hovering = bounds.Contains(mouseX, mouseY);
                if (hovering && justClicked)
                {
                    Game1.playSound("bigSelect");
                    Game1.activeClickableMenu.SetChildMenu(getMachineSelectMenu());
                }
                b.Draw(
                    Game1.mouseCursors2,
                    new((int)origin.X, (int)origin.Y, 80, 80),
                    new Rectangle(154, 154, 20, 20),
                    Color.White, 0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    1f
                );
            }
            else
            {
                b.DrawString(
                    Game1.dialogueFont,
                    notInGame,
                    new Vector2(origin.X + 12, origin.Y + 4),
                    Game1.textColor
                );
            }
        }
    }
    /// <summary>
    /// Options for default opened page
    /// Rules: machine rules page first
    /// Inputs: input page first
    /// </summary>
    internal enum DefaultPageOption
    {
        Rules = 1,
        Inputs = 2
    }

    /// <summary>
    /// Mod config class + GMCM
    /// </summary>
    internal sealed class ModConfig
    {
        /// <summary>Key for opening control panel when next to a machine</summary>
        public KeybindList ControlPanelKey { get; set; } = KeybindList.Parse($"{SButton.Q}");
        /// <summary>Key for opening machine selection page</summary>
        public KeybindList MachineSelectKey { get; set; } = KeybindList.Parse($"{SButton.LeftControl}+{SButton.Q}");
        /// <summary>Default page to use</summary>
        public DefaultPageOption DefaultPage { get; set; } = DefaultPageOption.Rules;
        public bool AlwaysShowCheckbox { get; set; } = false;

        private void Reset()
        {
            ControlPanelKey = KeybindList.Parse($"{SButton.MouseLeft}, {SButton.ControllerB}");
            MachineSelectKey = KeybindList.Parse($"{SButton.LeftControl}+{SButton.Q}");
            DefaultPage = DefaultPageOption.Rules;
        }

        public void Register(IModHelper helper, IManifest mod, Func<MachineMenu> GetMachineSelectMenu)
        {
            var GMCM = helper.ModRegistry.GetApi<Integration.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (GMCM == null)
            {
                helper.WriteConfig(this);
                return;
            }
            GMCM.Register(
                mod: mod,
                reset: () => { Reset(); helper.WriteConfig(this); },
                save: () => { helper.WriteConfig(this); },
                titleScreenOnly: false
            );
            GMCM.AddKeybindList(
                mod,
                getValue: () => { return ControlPanelKey; },
                setValue: (value) => { ControlPanelKey = value; },
                name: I18n.Config_ControlPanelKey_Name,
                tooltip: I18n.Config_ControlPanelKey_Description
            );
            GMCM.AddKeybindList(
                mod,
                getValue: () => { return MachineSelectKey; },
                setValue: (value) => { MachineSelectKey = value; },
                name: I18n.Config_MachineSelectKey_Name,
                tooltip: I18n.Config_MachineSelectKey_Description
            );
            GMCM.AddTextOption(
                mod,
                getValue: () => { return DefaultPage.ToString(); },
                setValue: (value) => { DefaultPage = Enum.Parse<DefaultPageOption>(value); },
                allowedValues: Enum.GetNames<DefaultPageOption>(),
                formatAllowedValue: value => value switch
                {
                    nameof(DefaultPageOption.Rules) => I18n.Config_DefaultPage_MachineRules(),
                    nameof(DefaultPageOption.Inputs) => I18n.Config_DefaultPage_ItemInputs(),
                    _ => "???" // should never happen
                },
                name: I18n.Config_DefaultPage_Name,
                tooltip: I18n.Config_DefaultPage_Description
            );
            OpenMenuButton menuBtn = new(
                GetMachineSelectMenu
            );
            GMCM.AddComplexOption(
                mod,
                name: I18n.Config_OpenMachineSelectMenu_Name,
                draw: menuBtn.Draw,
                height: () => 80
            );
        }
    }
}