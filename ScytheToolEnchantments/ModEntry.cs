using StardewModdingAPI;
using StardewModdingAPI.Events;
using ScytheToolEnchantments.Framework;
using ScytheToolEnchantments.Framework.Integration;
using ScytheToolEnchantments.Framework.Enchantments;


namespace ScytheToolEnchantments
{
    public class ModEntry : Mod
    {
        private static IMonitor? mon;
        internal static ModConfig? Config;

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            mon = Monitor;
            GamePatches.Patch(ModManifest);
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        public static void Log(string msg, LogLevel level = LogLevel.Debug)
        {
            mon!.Log(msg, level);
        }

        public override object GetApi()
        {
            return new ScytheToolEnchantmentsApi();
        }

        public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            RegisterEnchantmentTypes();
            Config = Helper.ReadConfig<ModConfig>();
            Config.Register(Helper, ModManifest);
        }

        private void RegisterEnchantmentTypes()
        {
            ISpaceCoreApi? spacecoreApi = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            // foreach (TypeInfo typeInfo in typeof(ScytheEnchantment).Assembly.DefinedTypes)
            // {
            //     if (typeInfo.GetCustomAttribute<XmlTypeAttribute>() == null || !typeInfo.IsAssignableTo(typeof(ScytheEnchantment)))
            //         continue;
            //     spacecoreApi!.RegisterSerializerType(typeInfo.AsType());
            // }
            spacecoreApi!.RegisterSerializerType(typeof(PalaeontologistEnchantment));
            spacecoreApi!.RegisterSerializerType(typeof(GathererEnchantment));
            spacecoreApi!.RegisterSerializerType(typeof(HorticulturistEnchantment));
            spacecoreApi!.RegisterSerializerType(typeof(ReaperEnchantment));
            spacecoreApi!.RegisterSerializerType(typeof(CrescentEnchantment));
        }
    }
}
