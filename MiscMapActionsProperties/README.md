# Misc Map Actions & Properties

Adds a few map related features, no strong design theme just whatever I happen to want.

See `[CP] MMAP Examples` for samples.

## Map Property

#### mushymato.MMAP_BuildingEntry <x> <y>

- For use in building maps.
- Changes where the player arrives on entry, away from the default 1 tile above first warp.

## Tile Data

#### mushymato.MMAP_AnimalSpot

- For use in animal building maps.
- Changes what tiles the animals will start in.
- If building has less AnimalSpot tiles than animals, the remaining animals get random spots.
- 1 AnimalSpot tile will get 1 animal, 2 AnimalSpot next to each other means 2 animals get to start around that area.
- The spawn point of the animal is based on their top left tile, for 2x2 tile animals it's best to put this tile prop top left of where you want them to go.

### Action

#### mushymato.MMAP_ShowConstruct <builder>

- Opens the construction menu for specified builder (`Robin` or `Wizard` in vanilla)

#### mushymato.MMAP_ShowConstructForCurrent <builder>

- Opens the construction menu for the current area.
- Does nothing if the current area is not buildable.

## Data/Locations CustomFields

#### mushymato.MMAP/HoeDirt.texture: <texture>

- Location CustomFields, for use in places with tillable soil.
- Changes the appearance of tilled soil in that location.
- Texture should follow vanilla format of 3 sets of 16 tiles: tilled, watered overlay, paddy overlay
- See `[CP] Vulkan Farm Cave` and `[PIF] Vulkan Cave` for example.
