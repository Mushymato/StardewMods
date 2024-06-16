using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.ItemTypeDefinitions;
using StardewValley.GameData.Objects;
using StardewObject = StardewValley.Object;
using StardewValley.Objects;
using StardewValley.Mods;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Inventories;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using StardewValley.GameData.Crops;

namespace SprinklerAttachments.Framework
{
    /// <summary>
    /// Helper class for getting custom field and mod data specific to this mod
    /// </summary>
    public static class ModInfoHelper
    {
        public const string Field_IntakeChestSize = $"{SprinklerAttachment.ContentModId}.IntakeChestSize";
        public const string Field_IntakeChestAcceptCategory = $"{SprinklerAttachment.ContentModId}.IntakeChestAcceptCategory";
        public const string Field_OverlayOffsetX = $"{SprinklerAttachment.ContentModId}.OverlayOffsetX";
        public const string Field_OverlayOffsetY = $"{SprinklerAttachment.ContentModId}.OverlayOffsetY";
        public const string Field_IsSowing = $"{SprinklerAttachment.ContentModId}.IsSowing";
        public const string Field_IsPressurize = $"{SprinklerAttachment.ContentModId}.IsPressurize";

        public static bool TryGetIntakeChestSize(ObjectData data, [NotNullWhen(true)] out int? ret)
        {
            return TryParseCustomField(data, Field_IntakeChestSize, out ret);
        }
        public static bool TryGetIntakeChestSize(ModDataDictionary data, [NotNullWhen(true)] out int? ret)
        {
            return TryParseModData(data, Field_IntakeChestSize, out ret);
        }
        public static bool TryGetIntakeChestAcceptCategory(ObjectData data, [NotNullWhen(true)] out List<int>? ret)
        {
            ret = null;
            if (data.CustomFields.TryGetValue(Field_IntakeChestAcceptCategory, out string? valueStr))
            {
                ret = valueStr.Split(",").ToList().ConvertAll(cat => Convert.ToInt32(cat));
                return true;
            }
            return false;
        }
        public static Vector2 GetOverlayOffset(ObjectData data)
        {
            if (!TryParseCustomField(data, Field_OverlayOffsetX, out int? offsetX))
                offsetX = 0;
            if (!TryParseCustomField(data, Field_OverlayOffsetY, out int? offsetY))
                offsetY = 0;
            return new((float)offsetX, (float)offsetY);
        }
        public static bool IsSowing(ObjectData data)
        {
            return TryParseCustomField(data, Field_IsSowing, out bool ret) && ret;
        }
        public static bool IsPressurize(ObjectData data)
        {
            return TryParseCustomField(data, Field_IsPressurize, out bool ret) && ret;
        }
        private static bool TryParseCustomField<T>(ObjectData data, string key, [NotNullWhen(true)] out T? ret)
        {
            ret = default;
            if (data.CustomFields.TryGetValue(key, out string? valueStr) && valueStr != null)
            {
                TypeConverter con = TypeDescriptor.GetConverter(typeof(T));
                if (con != null)
                {
                    ret = (T?)con.ConvertFromString(valueStr);
                    if (ret != null)
                        return true;
                }
            }
            return false;
        }
        private static bool TryParseModData<T>(ModDataDictionary data, string key, [NotNullWhen(true)] out T? ret)
        {
            ret = default;
            if (data.TryGetValue(key, out string? valueStr) && valueStr != null)
            {
                TypeConverter con = TypeDescriptor.GetConverter(typeof(T));
                if (con != null)
                {
                    ret = (T?)con.ConvertFromString(valueStr);
                    if (ret != null)
                        return true;
                }
            }
            return false;
        }

    }
    /// <summary>
    /// Functionality of sprinkler attachments
    /// </summary>
    internal static class SprinklerAttachment
    {
        /// <summary>
        /// ModId of content pack
        /// </summary>
        public const string ContentModId = "mushymato.SprinklerAttachments";

        /// <summary>
        /// Add attachment to sprinkler object, as part of <see cref="StardewObject.performObjectDropInAction"/>
        /// </summary>
        /// <param name="sprinkler">possible sprinkler object</param>
        /// <param name="attachmentItem">possible attachment held item</param>
        /// <param name="probe">dryrun</param>
        /// <returns>true if attached to sprinkler</returns>
        public static bool TryAttachToSprinkler(StardewObject sprinkler, Item attachmentItem, bool probe)
        {
            if (sprinkler.isTemporarilyInvisible || // item not loaded
                attachmentItem is not StardewObject attachment || // item not an object
                !attachment.HasContextTag(ContentModId) || // not an attachment item for this mod
                ItemRegistry.GetData(attachmentItem.QualifiedItemId)?.RawData is not ObjectData data || // item lack object data (?)
                !sprinkler.IsSprinkler() || // not a sprinkler
                sprinkler.heldObject.Value != null) // already has attached item (vanilla or mod)
                return false;

            if (probe)
                return true; // dryrun stops here

            GameLocation location = sprinkler.Location;
            if (location is MineShaft || location is VolcanoDungeon)
            {
                Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
                return false; // ban attachments in dungeons (why would you ever)
            }

            if (attachment.getOne() is not StardewObject attached)
                return false;

            // setup chest if IntakeChestSize is set
            if (ModInfoHelper.TryGetIntakeChestSize(data, out int? ret) && ret > 0 && attached.heldObject.Value == null)
            {
                Chest intakeChest = new();
                intakeChest.modData.Add(ModInfoHelper.Field_IntakeChestSize, ret.ToString());
                attached.heldObject.Value = intakeChest;
                intakeChest.mutex.Update(sprinkler.Location);
            }
            location.playSound("axe");
            sprinkler.heldObject.Value = attached;
            sprinkler.MinutesUntilReady = -1;

            return true;
        }

        /// <summary>
        /// some kind of multiplayer chest mutex thing, also makes it possible to doink the chest off in 1 hit.
        /// <seealso cref="StardewObject.updateWhenCurrentLocation(GameTime)"/>
        /// </summary>
        /// <param name="sprinkler"></param>
        /// <param name="time"></param>
        public static void UpdateWhenCurrentLocation(StardewObject sprinkler, GameTime time)
        {
            if (TryGetIntakeChest(sprinkler, out StardewObject? _, out Chest? intakeChest))
            {
                intakeChest.mutex.Update(sprinkler.Location);
                if (Game1.activeClickableMenu == null && intakeChest.GetMutex().IsLockHeld())
                {
                    intakeChest.GetMutex().ReleaseLock();
                }
            }
        }

        /// <summary>
        /// Open intake chest for attachments, if one exists on the sprinkler.
        /// <seealso cref="StardewObject.checkForAction(Farmer, bool)"/>
        /// </summary>
        /// <param name="sprinkler">possible sprinkler object</param>
        /// <param name="who">player performing action, unused</param>
        /// <param name="justCheckingForActivity">dryrun</param>
        /// <returns>true if intake chest opened</returns>
        public static bool CheckForAction(StardewObject sprinkler, Farmer who, bool justCheckingForActivity)
        {
            if (!TryGetSprinklerAttachment(sprinkler, out StardewObject? attachment))
                return false;

            if (justCheckingForActivity)
                return true; // dryrun stops here

            if (!Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true)) // TODO: how does this work with controllers?
                return false;

            if (attachment.heldObject.Value is not Chest attachedChest ||
                ItemRegistry.GetData(attachment.QualifiedItemId)?.RawData is not ObjectData data)
                return false;

            attachedChest.GetMutex().RequestLock(delegate () { ShowIntakeChestMenu(attachedChest, data); });
            return true;
        }

        /// <summary>
        /// Add 1 to sprinkler radius if the attachment has pressurize functionality
        /// </summary>
        /// <param name="sprinkler">possible sprinkler object</param>
        /// <param name="originalRadius">vanilla radius <see cref="StardewObject.GetModifiedRadiusForSprinkler"/></param>
        /// <returns></returns>
        public static int GetModifiedRadiusForSprinkler(StardewObject sprinkler, int originalRadius)
        {
            if (originalRadius > 0 &&
                TryGetSprinklerAttachment(sprinkler, out StardewObject? attachment) &&
                ItemRegistry.GetData(attachment.QualifiedItemId)?.RawData is ObjectData data &&
                ModInfoHelper.IsPressurize(data))
                return originalRadius + 1;
            return originalRadius;
        }

        /// <summary>
        /// If the chest has <see cref="ModInfoHelper.Field_IntakeChestSize"/> set, return it; else return original value.
        /// <seealso cref="Chest.GetActualCapacity"/>
        /// </summary>
        /// <param name="intakeChest">Chest object instance</param>
        /// <param name="originalValue">vanilla capacity <see cref="Chest.GetActualCapacity"/></param>
        /// <returns>int capacity for chest</returns>
        public static int GetActualCapacity(Chest intakeChest, int originalValue)
        {
            if (ModInfoHelper.TryGetIntakeChestSize(intakeChest.modData, out int? ret))
                return (int)ret;
            return originalValue;
        }

        public static void ApplySowingToAllSprinklers()
        {
            foreach (GameLocation location in Game1.locations)
            {
                if (location.GetData()?.CanPlantHere ?? location.IsFarm)
                {
                    location.objects.Lock();
                    foreach (KeyValuePair<Vector2, StardewObject> pair in location.objects.Pairs)
                    {
                        if (pair.Value.IsSprinkler())
                        {
                            ApplySowing(pair.Value);
                        }
                    }
                    location.objects.Unlock();
                }
            }
        }

        /// <summary>
        /// This doesn't work very well prob need transcriber
        /// </summary>
        /// <param name="mon"></param>
        /// <param name="sprinkler"></param>
        /// <param name="spriteBatch"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="alpha"></param>
        public static void DrawAttachment(StardewObject sprinkler, SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
        {
            if (TryGetSprinklerAttachment(sprinkler, out StardewObject? attachment) &&
                ItemRegistry.GetData(attachment.QualifiedItemId)?.RawData is ObjectData data)
            {
                Rectangle bounds = sprinkler.GetBoundingBoxAt(x, y);
                Vector2 offset = ModInfoHelper.GetOverlayOffset(data);
                ParsedItemData parsedData = ItemRegistry.GetDataOrErrorItem(attachment.QualifiedItemId);
                Rectangle sourceRect = parsedData.GetSourceRect(1);
                spriteBatch.Draw(
                    parsedData.GetTexture(),
                    Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((sprinkler.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((sprinkler.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0)) + offset),
                    sourceRect,
                    Color.White * alpha,
                    0f,
                    new Vector2(8f, 8f),
                    (sprinkler.scale.Y > 1f) ? sprinkler.getScale().Y : 4f,
                    sprinkler.Flipped ? SpriteEffects.None : SpriteEffects.FlipHorizontally, // the texture is already flipped
                    (sprinkler.isPassable() ? bounds.Top : bounds.Bottom) / 10000f + 1E-05f
                );
            }
        }

        /// <summary>
        /// Open intake chest menu.
        /// </summary>
        /// <param name="chest">intake chest</param>
        /// <param name="data">object data of attachment that holds chest</param>
        public static void ShowIntakeChestMenu(Chest chest, ObjectData data)
        {
            InventoryMenu.highlightThisItem highlightFunction;
            if (ModInfoHelper.TryGetIntakeChestAcceptCategory(data, out List<int>? ret))
            {
                highlightFunction = (Item item) => { return ret.Contains(item.Category); };
            }
            else
            {
                highlightFunction = InventoryMenu.highlightAllItems;
            }
            ItemGrabMenu? oldMenu = Game1.activeClickableMenu as ItemGrabMenu;
            Game1.activeClickableMenu = new ItemGrabMenu(
                inventory: chest.GetItemsForPlayer(),
                reverseGrab: false,
                showReceivingMenu: true,
                highlightFunction: highlightFunction,
                behaviorOnItemSelectFunction: chest.grabItemFromInventory,
                message: null,
                behaviorOnItemGrab: chest.grabItemFromChest,
                snapToBottom: false,
                canBeExitedWithKey: true,
                playRightClickSound: true,
                allowRightClick: true,
                showOrganizeButton: true,
                source: 1,
                sourceItem: chest,
                whichSpecialButton: -1,
                context: chest
            );
            if (oldMenu != null && Game1.activeClickableMenu is ItemGrabMenu newMenu)
            {
                newMenu.inventory.moveItemSound = oldMenu.inventory.moveItemSound;
                newMenu.inventory.highlightMethod = oldMenu.inventory.highlightMethod;
            }
        }

        /// <summary>
        /// Get open (no crop or no fertilizer) dirt within the sprinkler's range.
        /// </summary>
        /// <param name="sprinkler">sprinkler object</param>
        /// <param name="dirtList">list of dirt that are open</param>
        /// <returns>True if at least 1 open hoed dirt is found</returns>
        public static bool TryGetOpenHoedDirtAroundSprinkler(StardewObject sprinkler, [NotNullWhen(true)] out List<HoeDirt>? dirtList)
        {
            dirtList = new();
            foreach (Vector2 current in sprinkler.GetSprinklerTiles())
            {
                if (sprinkler.Location.terrainFeatures.TryGetValue(current, out var terrain) && terrain is HoeDirt dirt)
                {
                    if (dirt.crop == null || !dirt.HasFertilizer())
                    {
                        dirtList.Add(dirt);
                    }
                }
            }
            return dirtList.Count > 0;
        }

        private static void ApplySowing(StardewObject sprinkler)
        {
            if (TryGetIntakeChest(sprinkler, out StardewObject? attachment, out Chest? intakeChest) &&
                ItemRegistry.GetData(attachment.QualifiedItemId)?.RawData is ObjectData data &&
                ModInfoHelper.IsSowing(data) &&
                TryGetOpenHoedDirtAroundSprinkler(sprinkler, out List<HoeDirt>? dirtList) &&
                intakeChest.Items.Count > 0 &&
                intakeChest.Items[0] != null)
            {
                // Getting the chest mutex lock during DayEnding event seems to put lock in incoherent state, will just not do that
                // intakeChest.GetMutex().RequestLock(delegate
                // {
                //     PlantFromIntakeChest(dirtList, intakeChest, StardewObject.fertilizerCategory, RemotePlantFertilizer);
                //     PlantFromIntakeChest(dirtList, intakeChest, StardewObject.SeedsCategory, RemotePlantCrop);
                //     intakeChest.GetMutex().ReleaseLock();
                // });
                PlantFromIntakeChest(dirtList, intakeChest, StardewObject.fertilizerCategory, RemotePlantFertilizer);
                PlantFromIntakeChest(dirtList, intakeChest, StardewObject.SeedsCategory, RemotePlantCrop);
            }
        }


        private static bool TryGetSprinklerAttachment(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment)
        {
            attachment = null;
            if (sprinkler.IsSprinkler() && sprinkler.heldObject.Value is StardewObject held && held.HasContextTag(ContentModId))
            {
                attachment = held;
                return true;
            }
            return false;
        }

        private static bool TryGetIntakeChest(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment, [NotNullWhen(true)] out Chest? intakeChest)
        {
            intakeChest = null;
            if (TryGetSprinklerAttachment(sprinkler, out attachment))
            {
                if (attachment.heldObject.Value is Chest intake)
                {
                    intakeChest = intake;
                    return true;
                }
            }
            return false;
        }

        private static void PlantFromIntakeChest(List<HoeDirt> dirtList, Chest intakeChest, int category, Func<HoeDirt, string, bool> plantFunction)
        {
            Inventory chestItems = intakeChest.Items;
            Item item;
            for (int i = 0; i < chestItems.Count; i++)
            {
                item = chestItems[i];
                // TODO: can improve perf here by checking whether item is valid for location overall, then skipping
                if (item == null || item.Category != category)
                    continue;
                foreach (HoeDirt dirt in dirtList)
                {
                    if (plantFunction(dirt, item.ItemId))
                    {
                        item.Stack--;
                        if (item.Stack <= 0)
                        {
                            chestItems[i] = null;
                            break;
                        }
                    }
                }
            }
        }

        private static bool RemotePlantFertilizer(HoeDirt dirt, string itemId)
        {
            string qItemId = ItemRegistry.QualifyItemId(itemId) ?? itemId;
            Farmer who = Game1.player;
            // TODO: find optimal player to do the planting?
            if (dirt.CanApplyFertilizer(itemId))
            {
                dirt.fertilizer.Value = qItemId;
                dirt.applySpeedIncreases(who);
                return true;
            }
            return false;
        }

        private static bool RemotePlantCrop(HoeDirt dirt, string itemId)
        {
            if (dirt.crop != null)
                return false;
            GameLocation location = dirt.Location;
            itemId = Crop.ResolveSeedId(itemId, location);
            if (!Crop.TryGetData(itemId, out CropData cropData) || cropData.Seasons.Count == 0)
                return false;
            Farmer who = Game1.player;
            // TODO: find optimal player to do the planting?
            Point tilePos = Utility.Vector2ToPoint(dirt.Tile);
            bool isGardenPot = location.objects.TryGetValue(dirt.Tile, out StardewObject obj) && obj is IndoorPot;
            bool isIndoorPot = isGardenPot && !location.IsOutdoors;
            if (!location.CanPlantSeedsHere(itemId, tilePos.X, tilePos.Y, isGardenPot, out string _deniedMsg))
                return false;
            Season season = location.GetSeason();
            if (isIndoorPot || location.SeedsIgnoreSeasonsHere() || !((!(cropData.Seasons?.Contains(season))) ?? true))
            {
                dirt.crop = new Crop(itemId, tilePos.X, tilePos.Y, location);
                Game1.stats.SeedsSown++;
                dirt.applySpeedIncreases(who);
                dirt.nearWaterForPaddy.Value = -1;
                if (dirt.hasPaddyCrop() && dirt.paddyWaterCheck())
                {
                    dirt.state.Value = 1;
                    dirt.updateNeighbors();
                }
                return true;
            }
            return false;
        }
    }
}
