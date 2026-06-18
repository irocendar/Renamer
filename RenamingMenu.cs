using StardewValley.Menus;

namespace Renamer;

public class RenamingMenu : NamingMenu
{
    public IClickableMenu? previousMenu;
    public RenamingMenu(doneNamingBehavior b, string title, string? defaultName = null) : base(b, title, defaultName)
    {
    }
}