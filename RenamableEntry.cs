using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace Renamer;

public record RenamableEntry
{
    public const string buttonLabel = "Rename"; 
    
    public readonly object Renamable;

    public string DisplayName;
    public string ActualName;
    
    public readonly Texture2D Texture = null!;

    public Rectangle TextureSourceRect;

    public readonly int XOffset = 0;
    public readonly int YOffset = 0;

    public readonly int RowIndex;

    public Rectangle SlotBounds;
    private static readonly Point buttonSize = new ((int)Game1.dialogueFont.MeasureString(buttonLabel).X + 64, 68);
    public Rectangle ButtonBox => new (SlotBounds.Location + new Point(448 + 32 + 12, 24), buttonSize);
    public readonly ClickableComponent RenameButton;
    public readonly ClickableTextureComponent Sprite;
    
    // Have to use the netfield here
    [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible")]
    public RenamableEntry(Character character, int i, Rectangle slotBounds)
    {
        Renamable = character;
        DisplayName = character.displayName;
        ActualName = character.displayName;
        RowIndex = i;
        SlotBounds = slotBounds;
        RenameButton = GetButton();

        character.name.fieldChangeVisibleEvent += delegate
        {
            character.resetCachedDisplayName();
            ActualName = character.displayName;
            DisplayName = character.displayName;
        };

        switch (character)
        {
            case Horse horse:
                Texture = horse.Sprite.Texture;
                TextureSourceRect = new Rectangle(0, horse.Sprite.SourceRect.Height * 2 - 26, horse.Sprite.SourceRect.Width, 24);
                YOffset = 16;
                Sprite = GetSprite();
                break;
            case Pet pet:
                Texture = pet.Sprite.Texture;
                TextureSourceRect = new Rectangle(0, pet.Sprite.SourceRect.Height * 2 - 24, pet.Sprite.SourceRect.Width, 24);
                YOffset = 8;
                Sprite = GetSprite();
                break;
            case Child child:
                Texture = child.Sprite.Texture;
                TextureSourceRect = child.getMugShotSourceRect();
                switch (child.Age)
                {
                    case 0:
                        XOffset = 24;
                        YOffset = 28;
                        break;
                    case 1:
                        XOffset = 20;
                        YOffset = 12;
                        break;
                    case 2:
                        XOffset = 16;
                        YOffset = 32;
                        break;
                    case 3:
                        XOffset = 32;
                        YOffset = 12;
                        break;
                }
                break;
        }
        
        Sprite = GetSprite();
    }

    public RenamableEntry(Farm farm, int i, Rectangle slotBounds)
    {
        Renamable = farm;
        DisplayName = farm.DisplayName;
        ActualName = Game1.player.farmName.Value;
        Texture = Game1.whichFarm == 7 ? Game1.content.Load<Texture2D>(Game1.whichModFarm.IconTexture) : Game1.mouseCursors;
        TextureSourceRect = Game1.whichFarm switch
        {
            0 => new Rectangle(0, 324, 22, 20),
            1 => new Rectangle(22, 324, 22, 20),
            2 => new Rectangle(44, 324, 22, 20),
            3 => new Rectangle(66, 324, 22, 20),
            4 => new Rectangle(88, 324, 22, 20),
            5 => new Rectangle(0, 345, 22, 20),
            6 => new Rectangle(22, 345, 22, 20),
            _ => new Rectangle(0, 0, 22, 20)
        };
        XOffset = 20;
        YOffset = 20;
        RowIndex = i;
        SlotBounds = slotBounds;
        RenameButton = GetButton();
        Sprite = GetSprite();
    }
    
    private ClickableComponent GetButton()
    {
        return new ClickableComponent(ButtonBox, buttonLabel)
        {
            myID = RowIndex,
            downNeighborID = RowIndex + 1,
            upNeighborID = RowIndex <= 0 ? 12342 : RowIndex - 1
        };
    }

    private ClickableTextureComponent GetSprite()
    {
        Rectangle bounds = new Rectangle(SlotBounds.X + 4, SlotBounds.Y, SlotBounds.Width + 4, 64);
        return new ClickableTextureComponent(RowIndex.ToString(), bounds, null, "", Texture, TextureSourceRect, 4f);
    }

    public void RecalculateBounds()
    {
        Sprite.bounds = new Rectangle(SlotBounds.X + 4, SlotBounds.Y, SlotBounds.Width + 4, 64);
        RenameButton.bounds = ButtonBox;
    }

    public RenamingMenu GetRenamingMenu()
    {
        string title = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1236");
        return new RenamingMenu(Rename, title, ActualName);
    }

    public void Rename(string newName)
    {
        if (newName.Length <= 0)
        {
            return;
        }

        if (Renamable is NPC npc)
        {
            bool changed;
            do
            {
                changed = false;
                Utility.ForEachCharacter(n =>
                {
                    if (n == npc || n.Name != newName) return true;
                    newName += " ";
                    changed = true;
                    return false;
                });
            } 
            while (changed);
        }

        string target = "";
        string oldName = DisplayName;
        switch (Renamable)
        {
            case Child child:
                if (HaveMoreKidsIntegration.IsHMKLoaded)
                {
                    HaveMoreKidsIntegration.HaveMoreKidsAPI!.SetChildDisplayName(child, newName);
                }
                else
                {
                    child.Name = newName;
                    child.displayName = newName;
                }
                DisplayName = child.displayName;
                ActualName = child.displayName;
                target = "child";
                break;
            case Horse horse:
                if (Game1.player.horseName.Value == horse.Name || Game1.player.horseName.Value == null)
                {
                    Game1.player.horseName.Value = newName;
                }
                horse.Name = newName;
                horse.displayName = newName;
                DisplayName = newName;
                ActualName = newName;
                target = "horse";
                break;
            case Pet pet:
                pet.Name = newName;
                pet.displayName = newName;
                DisplayName = newName;
                ActualName = newName;
                target = "pet";
                break;
            case Farm farm:
                Game1.player.farmName.Value = newName;
                farm.DisplayName = null;
                DisplayName = farm.DisplayName!;
                ActualName = newName;
                target = "farm";
                break;
        }
        
        ModEntry.MMonitor.Log($"Renamed {target} \"{oldName}\" -> \"{DisplayName}\".", LogLevel.Info);
        
        (Game1.activeClickableMenu.GetChildMenu() ?? Game1.activeClickableMenu).exitThisMenu();
    }

    public void drawButton(SpriteBatch b)
    {
        float drawLayer = 0.8f - ButtonBox.Y * 1E-06f;
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), ButtonBox.X, ButtonBox.Y, ButtonBox.Width, ButtonBox.Height, Color.White, 4f, true, drawLayer);
        Vector2 stringCenter = Game1.dialogueFont.MeasureString(buttonLabel) / 2f;
        stringCenter.X = (int)(stringCenter.X / 4f) * 4;
        stringCenter.Y = (int)(stringCenter.Y / 4f) * 4;
        Utility.drawTextWithShadow(b, buttonLabel, Game1.dialogueFont, new Vector2(ButtonBox.Center.X, ButtonBox.Center.Y) - stringCenter, Game1.textColor , 1f, drawLayer + 1E-06f, -1, -1, 0f);
    }
}