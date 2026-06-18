using StardewModdingAPI.Utilities;

namespace Renamer;

public class ModConfig
{
    public KeybindList KeyBinds { get; set; } = KeybindList.Parse("OemSemicolon, ControllerBack");
}