using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace HaveMoreKids;

public interface IKidEntry
{
    /// <summary>Whether this kid is adopted from an NPC</summary>
    public bool IsAdoptedFromNPC { get; }
}

public interface IHaveMoreKidsAPI
{
    #region Kid Info

    /// <summary>
    /// Set the display name for a child, works for generic or custom children but not adopted from NPC kids.
    /// Please use this instead of directly setting <see cref="Child.Name"/> and <see cref="Child.displayName"/>.
    /// </summary>
    /// <param name="kid"></param>
    /// <param name="newName"></param>
    public void SetChildDisplayName(Child kid, string newName);
    
    /// <summary>
    /// Return HMK's kid entry for a given child, which will be populated once a save is loaded.
    /// Generic children will not have an entry.
    /// </summary>
    /// <param name="kid"></param>
    /// <returns>Kid entry, or null if not available</returns>
    public IKidEntry? GetKidEntry(Child kid);

    #endregion
}