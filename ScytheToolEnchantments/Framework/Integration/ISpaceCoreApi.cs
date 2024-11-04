namespace ScytheToolEnchantments.Framework.Integration;

public interface ISpaceCoreApi
{
    /// Must have [XmlType("Mods_SOMETHINGHERE")] attribute (required to start with "Mods_")
    void RegisterSerializerType(Type type);
}

