namespace FullInventoryToolbar
{
    public interface IFullInventoryToolbarApi
    {
        /// <summary>
        /// Return max number of items allowed for toolbar currently.
        /// This can change at game runtime if player changes config.
        /// </summary>
        /// <returns>max number of items allowed for toolbar</returns>
        public int GetToolbarMax();
    }
}
