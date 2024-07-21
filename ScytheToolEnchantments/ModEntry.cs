using System.Reflection;
using System.Xml.Serialization;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using ScytheToolEnchantments.Framework;
using ScytheToolEnchantments.Framework.Integration;
using ScytheToolEnchantments.Framework.Enchantments;
using StardewValley.Enchantments;


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

            // helper.ConsoleCommands.Add(
            //     "enchant_scythe",
            //     "Forge an iridium scythe in the inventory with one of Harvester(1), Horticulturist(2), Palaeontologist(3), Reaper(4), or None(0).",
            //     ConsoleEnchantScythe
            // );
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
        }

        // public static void ConsoleEnchantScythe(string command, string[] args)
        // {
        //     if (!Context.IsWorldReady || args.Length < 1)
        //         return;
        //     // Harvester, Horticulturist, Palaeontologist, Reaper
        //     foreach (Item item in Game1.player.Items)
        //     {
        //         if (item != null && item.QualifiedItemId == "(W)66")
        //         {
        //             MeleeWeapon scythe = (MeleeWeapon)item;
        //             switch (args[0])
        //             {
        //                 case "1":
        //                     scythe.AddEnchantment(new GathererEnchantment());
        //                     break;
        //                 case "2":
        //                     scythe.AddEnchantment(new HorticulturistEnchantment());
        //                     break;
        //                 case "3":
        //                     scythe.AddEnchantment(new PalaeontologistEnchantment());
        //                     break;
        //                 case "4":
        //                     scythe.AddEnchantment(new ReaperEnchantment());
        //                     break;
        //                 default:
        //                     for (int i = scythe.enchantments.Count; i >= 0; i--)
        //                     {
        //                         BaseEnchantment prevEnchantment = scythe.enchantments[i];
        //                         if (!prevEnchantment.IsForge() && !prevEnchantment.IsSecondaryEnchantment())
        //                         {
        //                             prevEnchantment.UnapplyTo(scythe);
        //                             scythe.enchantments.RemoveAt(i);
        //                         }
        //                     }
        //                     Log($"Removed scythe enchantment");
        //                     return;
        //             }
        //             Log($"Added scythe enchantment {scythe.enchantments[0]}");
        //             return;
        //         }
        //     }
        // }
    }
}
