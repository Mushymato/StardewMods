using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;

namespace BirbGlow;

public class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif
    private static IMonitor? mon;
    internal static string lightCF = null!;

    private static readonly NPCLightWatcher lightWatcher = new();

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        lightCF = ModManifest.UniqueID;

        helper.Events.Player.Warped += OnWarped;
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        Log(e.NewLocation.NameOrUniqueName);
        lightWatcher.Location = e.NewLocation;
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

/// <summary>
/// Shenanigans for watching building chest changes.
/// Use with WeakReference or ConditionalWeakTable;
/// </summary>
internal sealed class NPCLightWatcher
{
    private GameLocation? location = null;
    private ConditionalWeakTable<NPC, NPCLight> lightSources = [];
    public GameLocation? Location
    {
        get => location;
        set
        {
            if (location != null)
            {
                foreach (var kv in lightSources)
                {
                    kv.Value.Teardown();
                }
                lightSources.Clear();
            }
            location = value;
            if (location != null)
            {
                foreach (NPC npc in location.characters)
                {
                    SetupNPCLight(npc);
                }
                location.characters.OnValueAdded += SetupNPCLight;
                location.characters.OnValueRemoved -= TeardownNPCLight;
            }
        }
    }

    private void SetupNPCLight(NPC npc)
    {
        if (
            (npc.GetData()?.CustomFields?.TryGetValue(ModEntry.lightCF, out string? glowRadiusStr) ?? false)
            && int.TryParse(glowRadiusStr, out int radius)
        )
        {
            string lightId = $"{ModEntry.lightCF}_{npc.Name}";
            NPCLight npcLight = lightSources.GetValue(
                npc,
                (id) => new(npc, new LightSource(lightId, 1, npc.Position, radius))
            );
            npcLight.Setup();
        }
    }

    private void TeardownNPCLight(NPC npc)
    {
        if (lightSources.TryGetValue(npc, out NPCLight? npcLight))
            npcLight.Teardown();
    }
}

internal sealed record NPCLight(NPC Npc, LightSource Light) : IDisposable
{
    ~NPCLight() => Teardown();

    bool toreDown = false;

    public void Dispose()
    {
        Teardown();
        GC.SuppressFinalize(this);
    }

    internal void Setup()
    {
        if (toreDown)
            return;
        Game1.currentLightSources.Add(Light.Id, Light);
        Npc.position.fieldChangeVisibleEvent += RepositionLight;
    }

    internal void Teardown()
    {
        if (toreDown)
            return;
        Game1.currentLightSources.Remove(Light.Id);
        Npc.position.fieldChangeVisibleEvent -= RepositionLight;
        toreDown = true;
    }

    private void RepositionLight(NetPosition field, Vector2 oldValue, Vector2 newValue)
    {
        Utility.repositionLightSource(Light.Id, newValue);
    }
}
