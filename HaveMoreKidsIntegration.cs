using HaveMoreKids;
using StardewValley.Characters;

namespace Renamer;

internal static class HaveMoreKidsIntegration
{
    internal static bool IsHMKLoaded;
    internal static IHaveMoreKidsAPI? HaveMoreKidsAPI;

    public static void Initialize(IHaveMoreKidsAPI? api, bool isHMKLoaded)
    {
        IsHMKLoaded = isHMKLoaded;
        if (!IsHMKLoaded) return;

        if (api is null)
        {
            IsHMKLoaded = false;
            throw new TypeLoadException("Could not find HaveMoreKids API despite HaveMoreKids being loaded.");
        }

        HaveMoreKidsAPI = api;
    }

    public static bool CanRename(Child child)
    {
        return !IsHMKLoaded || HaveMoreKidsAPI?.GetKidEntry(child) is not { IsAdoptedFromNPC: true };
    }
}