using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.ItemTypeDefinitions;
using StardewValley.GameData.Objects;
using StardewValley.Objects;
using StardewValley.Mods;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Inventories;
using StardewValley.TerrainFeatures;
using StardewValley.GameData.Crops;
using StardewObject = StardewValley.Object;
using Microsoft.Xna.Framework.Content;

namespace SprinklerAttachments.Framework
{
    /// <summary>
    /// Helper class for getting custom field and mod data specific to this mod
    /// </summary>
    internal sealed class ModFieldHelper
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
    /// Config fields
    /// </summary>
    internal class ModConfig
    {
        public bool RestrictKrobusStock { get; set; } = true;
        public bool WaterOnPlanting { get; set; } = true;
        public bool EnableForGardenPots { get; set; } = true;
        public bool InvisibleAttachments { get; set; } = false;
        public bool PlantOnChestClose { get; set; } = false;

        private void Reset()
        {
            RestrictKrobusStock = true;
            EnableForGardenPots = true;
            WaterOnPlanting = true;
            InvisibleAttachments = false;
            PlantOnChestClose = false;
        }

        public void Register(IModHelper helper, IManifest mod,
                             Integration.IContentPatcherAPI CP, Integration.IGenericModConfigMenuApi? GMCM)
        {
            CP.RegisterToken(mod, nameof(RestrictKrobusStock), () => { return new string[] { RestrictKrobusStock.ToString() }; });
            // CP.RegisterToken(mod, nameof(EnableForGardenPots), () => { return new string[] { EnableForGardenPots.ToString() }; });
            // CP.RegisterToken(mod, nameof(WaterOnPlanting), () => { return new string[] { WaterOnPlanting.ToString() }; });
            CP.RegisterToken(mod, nameof(InvisibleAttachments), () => { return new string[] { InvisibleAttachments.ToString() }; });
            if (GMCM == null)
                return;
            GMCM.Register(
                mod: mod,
                reset: () =>
                {
                    Reset();
                    helper.WriteConfig(this);
                },
                save: () => { helper.WriteConfig(this); },
                titleScreenOnly: false
            );
            GMCM.AddBoolOption(
                mod,
                getValue: () => { return RestrictKrobusStock; },
                setValue: (value) => { RestrictKrobusStock = value; },
                name: () => helper.Translation.Get("config.RestrictKrobusStock.name"),
                tooltip: () => helper.Translation.Get("config.RestrictKrobusStock.description")
            );
            GMCM.AddBoolOption(
                mod,
                getValue: () => { return WaterOnPlanting; },
                setValue: (value) => { WaterOnPlanting = value; },
                name: () => helper.Translation.Get("config.WaterOnPlanting.name"),
                tooltip: () => helper.Translation.Get("config.WaterOnPlanting.description")
            );
            GMCM.AddBoolOption(
                mod,
                getValue: () => { return EnableForGardenPots; },
                setValue: (value) => { EnableForGardenPots = value; },
                name: () => helper.Translation.Get("config.EnableForGardenPots.name"),
                tooltip: () => helper.Translation.Get("config.EnableForGardenPots.description")
            );
            GMCM.AddBoolOption(
                mod,
                getValue: () => { return InvisibleAttachments; },
                setValue: (value) => { InvisibleAttachments = value; },
                name: () => helper.Translation.Get("config.InvisibleAttachments.name"),
                tooltip: () => helper.Translation.Get("config.InvisibleAttachments.description")
            );
            GMCM.AddBoolOption(
                mod,
                getValue: () => { return PlantOnChestClose; },
                setValue: (value) => { PlantOnChestClose = value; },
                name: () => helper.Translation.Get("config.PlantOnChestClose.name"),
                tooltip: () => helper.Translation.Get("config.PlantOnChestClose.description")
            );
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
        public const string ContentModId = "mushymato.SprinklerAttachments.CP";
        private static Func<StardewObject, IEnumerable<Vector2>>? CompatibleGetSprinklerTiles;
        private static Integration.IBetterSprinklersApi? BetterSprinklersApi;
        public static ModConfig? Config;

        public static void SetUpModCompatibility(IModHelper helper)
        {
            foreach (string modId in Integration.IBetterSprinklersApi.ModIds)
            {
                if (helper.ModRegistry.IsLoaded(modId))
                {
                    ModEntry.Log($"Apply compatibility changes with BetterSprinklers ({modId})", LogLevel.Trace);
                    BetterSprinklersApi = helper.ModRegistry.GetApi<Integration.IBetterSprinklersApi>(modId);
                    if (BetterSprinklersApi != null)
                    {
                        CompatibleGetSprinklerTiles = GetSprinklerTiles_BetterSprinklersPlus;
                        return;
                    }
                }
            }
            // Vanilla
            CompatibleGetSprinklerTiles = GetSprinklerTiles_Vanilla;
        }

        public static void SetUpModConfigMenu(IModHelper helper, IManifest manifest)
        {
            Integration.IContentPatcherAPI? CP = helper.ModRegistry.GetApi<Integration.IContentPatcherAPI>("Pathoschild.ContentPatcher") ?? throw new ContentLoadException("Failed to get Content Patcher API");
            // IModInfo cpMod = helper.ModRegistry.Get(ContentModId) ?? throw new ContentLoadException($"Required content pack {ContentModId} not loaded");
            Config = helper.ReadConfig<ModConfig>();
            Integration.IGenericModConfigMenuApi? GMCM = helper.ModRegistry.GetApi<Integration.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            Config.Register(helper, manifest, CP, GMCM);
        }

        public static void ReadConfig(IModHelper helper)
        {
            helper.ModRegistry.IsLoaded(ContentModId);
        }

        private static List<Vector2> GetSprinklerTiles_BetterSprinklersPlus(StardewObject sprinkler)
        {
            if (BetterSprinklersApi == null)
            {
                return sprinkler.GetSprinklerTiles();
            }
            Dictionary<int, Vector2[]> allCoverage = (Dictionary<int, Vector2[]>)BetterSprinklersApi.GetSprinklerCoverage();
            // BetterSprinklerPlus uses ParentSheetIndex instead of itemId to check sprinkler
            if (allCoverage.TryGetValue(sprinkler.ParentSheetIndex, out Vector2[]? relCoverage) && relCoverage != null)
            {
                Vector2 origin = sprinkler.TileLocation;
                List<Vector2> realCoverage = new();
                foreach (Vector2 rel in relCoverage)
                {
                    realCoverage.Add(origin + rel);
                }
                return realCoverage;
            }
            return sprinkler.GetSprinklerTiles();
        }

        private static List<Vector2> GetSprinklerTiles_Vanilla(StardewObject sprinkler)
        {
            return sprinkler.GetSprinklerTiles();
        }

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
            if (ModFieldHelper.TryGetIntakeChestSize(data, out int? ret) && ret > 0 && attached.heldObject.Value == null)
            {
                Chest intakeChest = new();
                intakeChest.modData.Add(ModFieldHelper.Field_IntakeChestSize, ret.ToString());
                attached.heldObject.Value = intakeChest;
                intakeChest.mutex.Update(sprinkler.Location);
            }
            location.playSound("axe");
            sprinkler.heldObject.Value = attached;
            sprinkler.MinutesUntilReady = -1;

            return true;
        }

        /// <summary>
        /// Handle chest mutex updates
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
                    if (Config?.PlantOnChestClose ?? false)
                    {
                        ApplySowing(sprinkler);
                    }
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
            if (originalRadius >= 0 &&
                TryGetSprinklerAttachment(sprinkler, out StardewObject? attachment) &&
                ItemRegistry.GetData(attachment.QualifiedItemId)?.RawData is ObjectData data &&
                ModFieldHelper.IsPressurize(data))
                return originalRadius + 1;
            return originalRadius;
        }

        /// <summary>
        /// If the chest has <see cref="ModFieldHelper.Field_IntakeChestSize"/> set, return it; else return original value.
        /// <seealso cref="Chest.GetActualCapacity"/>
        /// </summary>
        /// <param name="intakeChest">Chest object instance</param>
        /// <param name="originalValue">vanilla capacity <see cref="Chest.GetActualCapacity"/></param>
        /// <returns>int capacity for chest</returns>
        public static int GetActualCapacity(Chest intakeChest, int originalValue)
        {
            if (ModFieldHelper.TryGetIntakeChestSize(intakeChest.modData, out int? ret))
                return (int)ret;
            return originalValue;
        }

        public static void ApplySowingToAllSprinklers(bool verbose = false)
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
                            if (verbose) ModEntry.Log($"Try ApplySowing on sprinkler {pair.Value.Name} ({pair.Key}:{location})", LogLevel.Debug);
                            ApplySowing(pair.Value);
                        }
                    }
                    location.objects.Unlock();
                }
            }
        }

        public static bool DrawAttachment(StardewObject sprinkler, SpriteBatch spriteBatch, int x, int y, float alpha)
        {
            if (TryGetSprinklerAttachment(sprinkler, out StardewObject? attachment))
            {
                Vector2 offset = Vector2.Zero;
                Rectangle bounds = sprinkler.GetBoundingBoxAt(x, y);
                if (!(Config?.InvisibleAttachments ?? false))
                {
                    ParsedItemData parsedData = ItemRegistry.GetDataOrErrorItem(attachment.QualifiedItemId);
                    Rectangle sourceRect = parsedData.GetSourceRect(1);
                    sourceRect.Height += 2; // add 2 since sprites for this mod are 18 tall
                    if (ItemRegistry.GetData(attachment.QualifiedItemId)?.RawData is ObjectData data)
                        offset = ModFieldHelper.GetOverlayOffset(data);
                    spriteBatch.Draw(
                        parsedData.GetTexture(),
                        Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((sprinkler.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((sprinkler.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0)) + offset),
                        sourceRect,
                        Color.White * alpha, 0f, new Vector2(8f, 8f), (sprinkler.scale.Y > 1f) ? sprinkler.getScale().Y : 4f,
                        sprinkler.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                        (float)(sprinkler.isPassable() ? bounds.Top : bounds.Bottom) / 10000f + 1E-05f
                    );
                }
                if (sprinkler.SpecialVariable == 999999)
                {
                    if (offset.Y != 0)
                    {
                        Torch.drawBasicTorch(spriteBatch, (float)(x * 64) - 2f, y * 64 - 32, (float)bounds.Bottom / 10000f + 1E-06f);
                    }
                    else
                    {
                        Torch.drawBasicTorch(spriteBatch, (float)(x * 64) - 2f, y * 64 - 32 + 12, (float)(bounds.Bottom + 2) / 10000f);

                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Open intake chest menu.
        /// </summary>
        /// <param name="chest">intake chest</param>
        /// <param name="data">object data of attachment that holds chest</param>
        private static void ShowIntakeChestMenu(Chest chest, ObjectData data)
        {
            InventoryMenu.highlightThisItem highlightFunction;
            if (ModFieldHelper.TryGetIntakeChestAcceptCategory(data, out List<int>? ret))
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
            GameLocation location = sprinkler.Location;
            dirtList = new();
            foreach (Vector2 current in (CompatibleGetSprinklerTiles ?? GetSprinklerTiles_Vanilla)(sprinkler))
            {
                HoeDirt candidate;
                if ((Config?.EnableForGardenPots ?? true) &&
                     (location.getObjectAtTile((int)current.X, (int)current.Y) is IndoorPot ||
                      location.getObjectAtTile((int)current.X, (int)current.Y, ignorePassables: true) is IndoorPot))
                {
                    if (location.getObjectAtTile((int)current.X, (int)current.Y) is IndoorPot pot1)
                    {
                        candidate = pot1.hoeDirt.Value;
                    }
                    // special case carpets
                    else if (location.getObjectAtTile((int)current.X, (int)current.Y, ignorePassables: true) is IndoorPot pot2)
                    {
                        candidate = pot2.hoeDirt.Value;
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (location.terrainFeatures.TryGetValue(current, out var terrain) && terrain is HoeDirt dirt)
                {
                    candidate = dirt;
                }
                else
                {
                    continue;
                }
                if (candidate.crop == null || !candidate.HasFertilizer())
                {
                    dirtList.Add(candidate);
                }
            }
            return dirtList.Count > 0;
        }

        public static void ApplySowing(StardewObject sprinkler)
        {
            if (TryGetIntakeChest(sprinkler, out StardewObject? attachment, out Chest? intakeChest) &&
                ItemRegistry.GetData(attachment.QualifiedItemId)?.RawData is ObjectData data &&
                ModFieldHelper.IsSowing(data) &&
                TryGetOpenHoedDirtAroundSprinkler(sprinkler, out List<HoeDirt>? dirtList) &&
                intakeChest.Items.Count > 0 &&
                intakeChest.Items[0] != null)
            {
                PlantFromIntakeChest(dirtList, intakeChest, StardewObject.fertilizerCategory, RemotePlantFertilizer);
                PlantFromIntakeChest(dirtList, intakeChest, StardewObject.SeedsCategory, RemotePlantCrop);
            }
        }


        public static bool TryGetSprinklerAttachment(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment)
        {
            attachment = null;
            if (sprinkler.IsSprinkler() && sprinkler.heldObject.Value is StardewObject held && held.HasContextTag(ContentModId))
            {
                attachment = held;
                return true;
            }
            return false;
        }

        public static bool TryGetIntakeChest(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment, [NotNullWhen(true)] out Chest? intakeChest)
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
                // water any newly planted crops, so they will sprout on day begin
                else if ((Config?.WaterOnPlanting ?? true) &&
                         !(dirt.Location.doesTileHavePropertyNoNull((int)dirt.Tile.X, (int)dirt.Tile.Y, "NoSprinklers", "Back") == "T") &&
                         dirt.state.Value != 2)
                {
                    dirt.state.Value = 1;
                }
                return true;
            }
            return false;
        }
    }
}
