using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.HomeRenovations;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.SpecialOrders.Objectives;

namespace SpecialOrderNotifications.Framework
{
    public static class QuestPingHelper
    {
        private static Texture2D? JunimoKart;
        private static Texture2D? MineTiles;

        /// <summary>
        /// Set questPing(Texture|SourceRect|String) and questNotificationTimer fields on Game1.dayTimeMoneyBox via reflection
        /// </summary>
        /// <param name="questPingTexture"></param>
        /// <param name="questPingSourceRect"></param>
        /// <param name="questPingString"></param>
        /// <param name="questNotificationTimer"></param>
        public static void SetQuestPing(Texture2D questPingTexture, Rectangle questPingSourceRect, string? questPingString, int questNotificationTimer = 3000)
        {
            if (Game1.dayTimeMoneyBox.GetType().GetField("questPingTexture", BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo questPingTextureField)
            {
                questPingTextureField.SetValue(Game1.dayTimeMoneyBox, questPingTexture);
            }

            if (Game1.dayTimeMoneyBox.GetType().GetField("questPingSourceRect", BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo questPingSourceRectField)
            {
                questPingSourceRectField.SetValue(Game1.dayTimeMoneyBox, questPingSourceRect);
            }

            if (Game1.dayTimeMoneyBox.GetType().GetField("questPingString", BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo questPingStringField)
            {
                questPingStringField.SetValue(Game1.dayTimeMoneyBox, questPingString);
            }

            if (Game1.dayTimeMoneyBox.GetType().GetField("questNotificationTimer", BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo questNotificationTimerField)
            {
                questNotificationTimerField.SetValue(Game1.dayTimeMoneyBox, questNotificationTimer);
            }
        }

        /// <summary>
        /// Set questPing(Texture|SourceRect), questNotificationTimer, and questPingString based on current and max number
        /// </summary>
        /// <param name="questPingTexture"></param>
        /// <param name="questPingSourceRect"></param>
        /// <param name="current"></param>
        /// <param name="max"></param>
        /// <param name="questNotificationTimer"></param>
        public static void SetQuestPing(Texture2D questPingTexture, Rectangle questPingSourceRect, int current, int max, int questNotificationTimer = 3000)
        {
            SetQuestPing(questPingTexture, questPingSourceRect, current != max ? $"{current}/{max}" : null, questNotificationTimer: questNotificationTimer);
        }

        /// <summary>
        /// Draws the quest ping box, resized to fit text
        /// </summary>
        /// <param name="b"></param>
        /// <param name="position"></param>
        /// <param name="questPingTexture"></param>
        /// <param name="questPingSourceRect"></param>
        /// <param name="questPingString"></param>
        public static void DrawQuestPingBox(SpriteBatch b, Vector2 position, Texture2D questPingTexture, Rectangle questPingSourceRect, string? questPingString)
        {
            Vector2 basePosition = position + new Vector2(27f, 76f) * 4f;
            Vector2 stringSize = questPingString == null ? Vector2.Zero : Game1.smallFont.MeasureString(questPingString);
            if (stringSize.X < 60)
            {
                b.Draw(Game1.mouseCursors_1_6, basePosition, new Rectangle(257, 228, 39, 18), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                b.Draw(questPingTexture, basePosition + new Vector2(1f, 1f) * 4f, questPingSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.91f);
                if (questPingString == null)
                    b.Draw(Game1.mouseCursors_1_6, basePosition + new Vector2(22f, 5f) * 4f, new Rectangle(297, 229, 9, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.91f);
                else
                    Utility.drawTextWithShadow(b, questPingString, Game1.smallFont, basePosition + new Vector2(27f, 9.5f) * 4f - stringSize * 0.5f, Game1.textColor);
            }
            else
            {
                int extraM = (int)Math.Floor(stringSize.X / 4 - 15);
                basePosition.X -= extraM * 4;
                // left (square) and first segment
                b.Draw(Game1.mouseCursors_1_6, basePosition, new(257, 228, 31, 18), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                b.Draw(questPingTexture, basePosition + new Vector2(1f, 1f) * 4f, questPingSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.91f);
                // extra middle segments
                Vector2 offset = new(31f, 0f);
                for (int i = 0; i < extraM; i++)
                {
                    b.Draw(Game1.mouseCursors_1_6, basePosition + offset * 4f, new(287, 228, 1, 18), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                    offset.X += 1;
                }
                // tail
                b.Draw(Game1.mouseCursors_1_6, basePosition + offset * 4f, new(288, 228, 8, 18), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                basePosition.X += extraM / 2;
                // text
                offset = new Vector2(offset.X, 9.5f) * 4;
                offset.X += 11; // magic number weh
                offset.X -= stringSize.X;
                offset.Y -= stringSize.Y / 2;
                Utility.drawTextWithShadow(b, questPingString, Game1.smallFont, basePosition + offset, Game1.textColor);
            }
        }

        /// <summary>
        /// Send a ping in format of [item icon] current / max
        /// </summary>
        /// <param name="item"></param>
        /// <param name="current"></param>
        /// <param name="max"></param>
        public static void PingItem(Item item, int current, int max)
        {
            ParsedItemData parsedItem = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
            SetQuestPing(parsedItem.GetTexture(), parsedItem.GetSourceRect(), current, max);
        }

        /// <summary>
        /// Send a ping in format of [item icon] current / max
        /// </summary>
        /// <param name="item"></param>
        /// <param name="current"></param>
        /// <param name="max"></param>
        public static void PingMonster(Monster monster, int current, int max)
        {
            if (monster.Sprite.Texture == null)
                return;
            // No good way to auto pick icon rect from a monster sprite sheet, follow game hardcoding
            // StardewValley.Menus/DayTimeMoneyBox.cs pingQuest SlayMonsterQuest
            Rectangle sourceRect = new(0, 5, 16, 16);
            if (monster.Name.Equals("Green Slime"))
            {
                sourceRect = new Rectangle(0, 264, 16, 16);
            }
            else if (monster.Name.Equals("Frost Jelly"))
            {
                sourceRect = new Rectangle(16, 264, 16, 16);
            }
            else if (monster.Name.Contains("Sludge"))
            {
                sourceRect = new Rectangle(32, 264, 16, 16);
            }
            else if (monster.Name.Equals("Dust Spirit"))
            {
                sourceRect.Y = 8;
            }
            else if (monster.Name.Contains("Crab"))
            {
                sourceRect = new Rectangle(48, 106, 16, 16);
            }
            else if (monster.Name.Contains("Duggy"))
            {
                sourceRect = new Rectangle(0, 32, 16, 16);
            }
            else if (monster.Name.Equals("Squid Kid"))
            {
                sourceRect = new Rectangle(0, 0, 16, 16);
            }
            SetQuestPing(monster.Sprite.Texture, sourceRect, current, max);
        }

        /// <summary>
        /// Send a ping in format of [gift box] current / max
        /// </summary>
        /// <param name="current"></param>
        /// <param name="max"></param>
        public static void PingGift(int current, int max)
        {
            SetQuestPing(
                Game1.mouseCursors2,
                new Rectangle(166, 174, 14, 12),
                current, max
            );
        }

        /// <summary>
        /// Send a ping in format of [junimo kart] current / max
        /// </summary>
        /// <param name="current"></param>
        /// <param name="max"></param>
        public static void PingJunimoKart(int current, int max)
        {
            JunimoKart ??= Game1.content.Load<Texture2D>("Minigames\\MineCart");
            SetQuestPing(JunimoKart, new Rectangle(400, 512, 16, 16), current, max);
        }

        /// <summary>
        /// Send a ping in format of [mine ladder] current / max
        /// </summary>
        /// <param name="current"></param>
        /// <param name="max"></param>
        public static void PingMineLadder(int current, int max)
        {
            MineTiles ??= Game1.content.Load<Texture2D>("Maps\\Mines\\mine");
            SetQuestPing(MineTiles, new Rectangle(208, 160, 16, 16), current, max);
        }
    }
}
