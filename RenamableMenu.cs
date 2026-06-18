using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Renamer;

public class RenamableMenu : IClickableMenu
{
    public const int slotsOnPage = 5;
    
    public readonly bool ShowBackground;
    
    public ClickableTextureComponent upButton = null!;
    public ClickableTextureComponent downButton = null!;
    public ClickableTextureComponent scrollBar = null!;
    public Rectangle scrollBarRunner;
    
    // Internally stores the current viewport width/height to get around `gameWindowSizeChanged` being unreliable with
    // the previous size.
    private int uiViewportWidth;
    private int uiViewportHeight;
    

    public readonly List<RenamableEntry> RenamableEntries;
    public bool showScrollbar => RenamableEntries.Count > slotsOnPage;
    public Rectangle DefaultSlotBounds => new (xPositionOnScreen + borderWidth, 0, width - borderWidth * 2, rowPosition(1) - rowPosition(0));
    /// <summary>The index of the <see cref="F:Renamer.RenamableEntries" /> entry shown at the top of the scrolled view.</summary>
    public int slotPosition;
    public bool currentlyScrolling;

    public RenamableMenu(int x, int y, int width, int height, bool showBackground = true)
        : base(x, y, width, height)
    {
        ShowBackground = showBackground;
        RenamableEntries = FindRenamables();
        CreateComponents();
        slotPosition = 0;
        setScrollBarToCurrentIndex();
        updateSlots();
        base.populateClickableComponentList();
        allClickableComponents.AddRange(RenamableEntries.Select(entry => entry.RenameButton));
        if (ShowBackground) initializeUpperRightCloseButton();
        
        uiViewportWidth = Game1.uiViewport.Width;
        uiViewportHeight = Game1.uiViewport.Height;
    }

    public List<RenamableEntry> FindRenamables()
    {
        List<RenamableEntry> entries = new List<RenamableEntry>();
        
        if (Game1.player.IsMainPlayer) entries.Add(new RenamableEntry(Game1.getFarm(), 0, DefaultSlotBounds));
        
        Utility.ForEachLocation(delegate(GameLocation location)
        {
            foreach (NPC character in location.characters)
            {
                switch (character)
                {
                    case Pet pet when !pet.hideFromAnimalSocialMenu.Value:
                    case Horse horse when !horse.hideFromAnimalSocialMenu.Value && horse.getOwner() == Game1.player:
                    case Child child when (Game1.player.IsMainPlayer || Game1.GetPlayer(child.idOfParent.Value) == Game1.player) && HaveMoreKidsIntegration.CanRename(child):
                        character.resetCachedDisplayName();
                        entries.Add(new RenamableEntry(character, entries.Count, DefaultSlotBounds));
                        break;
                }
            }
            return true;
        });
        
        if (Game1.player.mount != null)
        {
            entries.Add(new RenamableEntry(Game1.player.mount, entries.Count, DefaultSlotBounds));
        }
        return entries;
    }

    public void CreateComponents()
    {
        upButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
        downButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
        scrollBar = new ClickableTextureComponent(new Rectangle(upButton.bounds.X + 12, upButton.bounds.Y + upButton.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
        scrollBarRunner = new Rectangle(scrollBar.bounds.X, upButton.bounds.Y + upButton.bounds.Height + 4, scrollBar.bounds.Width, height - 128 - upButton.bounds.Height - 8);
    }

    public void updateComponentBounds()
    {
        upButton.setPosition(xPositionOnScreen + width + 16, yPositionOnScreen + 64);
        downButton.setPosition(xPositionOnScreen + width + 16, yPositionOnScreen + height - 64);
        scrollBar.setPosition(upButton.bounds.X + 12, upButton.bounds.Y + upButton.bounds.Height + 4);
        scrollBarRunner.X = scrollBar.bounds.X;
        scrollBarRunner.Y = upButton.bounds.Y + upButton.bounds.Height + 4;
        
        if (ShowBackground) upperRightCloseButton.setPosition(xPositionOnScreen + width - 36, yPositionOnScreen - 8);
    }

    public RenamableEntry? GetEntry(int index)
    {
        if (index < 0 || index >= RenamableEntries.Count)
        {
            index = 0;
        }
        return RenamableEntries.Count == 0 ? null : RenamableEntries[index];
    }

    public override void snapToDefaultClickableComponent()
    {
        if (slotPosition < RenamableEntries.Count)
        {
            currentlySnappedComponent = RenamableEntries[slotPosition].RenameButton;
        }
        snapCursorToCurrentSnappedComponent();
    }

    public void updateSlots()
    {
        for (int i = 0; i < RenamableEntries.Count; i++)
        {
            RenamableEntries[i].SlotBounds.X = DefaultSlotBounds.X;
            RenamableEntries[i].SlotBounds.Y = rowPosition(i - 1);
            RenamableEntries[i].RecalculateBounds();
        }
        
        base.populateClickableComponentList();
        allClickableComponents.AddRange(RenamableEntries.Select(entry => entry.RenameButton));
    }

    protected void _SelectSlot(int index)
    {
        currentlySnappedComponent = RenamableEntries[index].RenameButton;
        if (index < slotPosition)
        {
            slotPosition = index;
        }
        else if (index >= slotPosition + slotsOnPage)
        {
            slotPosition = index - slotsOnPage + 1;
        }
        setScrollBarToCurrentIndex();
        updateSlots();
        if (Game1.options.snappyMenus && Game1.options.gamepadControls)
        {
            snapCursorToCurrentSnappedComponent();
        }
    }

    public void ConstrainSelectionToVisibleSlots()
    {
        if (currentlySnappedComponent != null && RenamableEntries.Count > currentlySnappedComponent.myID)
        {
            int index = currentlySnappedComponent.myID;
            if (index < slotPosition)
            {
                index = slotPosition;
            }
            else if (index >= slotPosition + slotsOnPage)
            {
                index = slotPosition + slotsOnPage - 1;
            }
            currentlySnappedComponent = RenamableEntries[index].RenameButton;
            if (Game1.options.snappyMenus && Game1.options.gamepadControls)
            {
                snapCursorToCurrentSnappedComponent();
            }
        }
    }

    public override void snapCursorToCurrentSnappedComponent()
    {
        if (currentlySnappedComponent != null && RenamableEntries.Count > currentlySnappedComponent.myID)
        {
            Game1.setMousePosition(currentlySnappedComponent.bounds.Right - currentlySnappedComponent.bounds.Width / 8, currentlySnappedComponent.bounds.Top + currentlySnappedComponent.bounds.Height / 4);
        }
        else
        {
            base.snapCursorToCurrentSnappedComponent();
        }
    }

    public override void applyMovementKey(int direction)
    {
        base.applyMovementKey(direction);
        if (RenamableEntries.Count > currentlySnappedComponent.myID)
        {
            _SelectSlot(currentlySnappedComponent.myID);
        }
        snapCursorToCurrentSnappedComponent();
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (currentlyScrolling)
        {
            int y2 = scrollBar.bounds.Y;
            scrollBar.bounds.Y = Math.Min(yPositionOnScreen + height - 64 - 12 - scrollBar.bounds.Height, Math.Max(y, yPositionOnScreen + upButton.bounds.Height + 20));
            float percentage = (y - scrollBarRunner.Y) / (float)scrollBarRunner.Height;
            slotPosition = Math.Min(RenamableEntries.Count - slotsOnPage, Math.Max(0, (int)(RenamableEntries.Count * percentage)));
            setScrollBarToCurrentIndex();
            if (y2 != scrollBar.bounds.Y)
            {
                Game1.playSound("shiny4");
            }
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        currentlyScrolling = false;
    }

    private void setScrollBarToCurrentIndex()
    {
        if (!showScrollbar) return;
        if (RenamableEntries.Count > 0)
        {
            scrollBar.bounds.Y = scrollBarRunner.Height / Math.Max(1, RenamableEntries.Count - slotsOnPage + 1) * slotPosition + upButton.bounds.Bottom + 4;
            if (slotPosition == RenamableEntries.Count - slotsOnPage)
            {
                scrollBar.bounds.Y = downButton.bounds.Y - scrollBar.bounds.Height - 4;
            }
        }
        updateSlots();
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (!showScrollbar) return;
        base.receiveScrollWheelAction(direction);
        if (direction > 0 && slotPosition > 0)
        {
            upArrowPressed();
            ConstrainSelectionToVisibleSlots();
            Game1.playSound("shiny4");
        }
        else if (direction < 0 && slotPosition < Math.Max(0, RenamableEntries.Count - slotsOnPage))
        {
            downArrowPressed();
            ConstrainSelectionToVisibleSlots();
            Game1.playSound("shiny4");
        }
    }

    public void upArrowPressed()
    {
        slotPosition--;
        updateSlots();
        upButton.scale = 3.5f;
        setScrollBarToCurrentIndex();
    }

    public void downArrowPressed()
    {
        slotPosition++;
        updateSlots();
        downButton.scale = 3.5f;
        setScrollBarToCurrentIndex();
    }

    /// <summary>Update the menu when the game window is resized. The <paramref name="oldBounds"/> cannot be trusted to be correct.</summary>
    /// <param name="oldBounds">The window's previous pixel size. Unreliable when changing from fullscreen (either type) to windowed.</param>
    /// <param name="newBounds">The window's new pixel size.</param>
    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        xPositionOnScreen = xPositionOnScreen - (uiViewportWidth / 2) + (newBounds.Width / 2);
        yPositionOnScreen = yPositionOnScreen - (uiViewportHeight / 2) + (newBounds.Height / 2);
        uiViewportWidth = newBounds.Width;
        uiViewportHeight = newBounds.Height;
        updateComponentBounds();
        updateSlots();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (showScrollbar)
        {
            if (upButton.containsPoint(x, y) && slotPosition > 0)
            {
                upArrowPressed();
                Game1.playSound("shwip");
                return;
            }
            if (downButton.containsPoint(x, y) && slotPosition < RenamableEntries.Count - slotsOnPage)
            {
                downArrowPressed();
                Game1.playSound("shwip");
                return;
            }
            if (scrollBar.containsPoint(x, y))
            {
                currentlyScrolling = true;
                return;
            }
            if (!downButton.containsPoint(x, y) && x > xPositionOnScreen + width && x < xPositionOnScreen + width + 128 && y > yPositionOnScreen && y < yPositionOnScreen + height)
            {
                currentlyScrolling = true;
                leftClickHeld(x, y);
                releaseLeftClick(x, y);
                return;
            }
        }
        
        for (int i = slotPosition; i < Math.Min(RenamableEntries.Count, slotPosition + slotsOnPage); i++)
        {
            if (RenamableEntries[i].ButtonBox.Contains(x, y))
            {
                if (ShowBackground) SetChildMenu(RenamableEntries[i].GetRenamingMenu());
                else
                {
                    RenamingMenu menu = RenamableEntries[i].GetRenamingMenu();
                    menu.previousMenu = Game1.activeClickableMenu;
                    menu.exitFunction = () => Game1.activeClickableMenu = menu.previousMenu;
                    Game1.activeClickableMenu = menu;
                }
            }
        }
        slotPosition = Math.Max(0, Math.Min(RenamableEntries.Count - slotsOnPage, slotPosition));
    }
    
    /// <summary>
    /// Handle closing the name entry submenu using the escape key.
    /// </summary>
    public void receiveEscKey()
    {
        if (GetChildMenu() is not RenamingMenu renamingMenu) return;
        renamingMenu._parentMenu = null;
        SetChildMenu(null);
    }
    
    public override void performHoverAction(int x, int y)
    {
        upButton.tryHover(x, y);
        downButton.tryHover(x, y);
        base.performHoverAction(x, y);
    }

    private void drawRenamableSlot(SpriteBatch b, int i)
    {
        RenamableEntry? entry = GetEntry(i);
        if (entry == null || i < 0)
        {
            return;
        }

        RenamableEntries[i].Sprite.draw(b, Color.White, 0.86f + RenamableEntries[i].Sprite.bounds.Y / 20000f, xOffset: entry.XOffset, yOffset: entry.YOffset);
        RenamableEntries[i].drawButton(b);
        float lineHeight = Game1.smallFont.MeasureString("W").Y;
        float russianOffsetY = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko) ? ((0f - lineHeight) / 2f) : 0f);
        int yOffset = 8;
        // ReSharper disable once PossibleLossOfFraction
        b.DrawString(Game1.dialogueFont, entry.DisplayName, new Vector2(xPositionOnScreen + borderWidth * 3 / 2 + 192 - 20 + 96 - (int)(Game1.dialogueFont.MeasureString(entry.DisplayName).X / 2f), RenamableEntries[i].SlotBounds.Y + 48 + yOffset + russianOffsetY - 20f), Game1.textColor);
    }

    private int rowPosition(int i)
    {
        int j = i - slotPosition;
        int rowHeight = 112;
        return yPositionOnScreen + borderWidth + 160 + 4 + j * rowHeight;
    }

    public override void draw(SpriteBatch b)
    {
        if (ShowBackground)
        {
            if (!Game1.options.showMenuBackground && !Game1.options.showClearBackgrounds)
            {
                b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            }
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, speaker: false, drawOnlyBox: true);
        }
        b.End();
        b.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, Utility.ScissorEnabled);
        if (RenamableEntries.Count > 0)
        {
            drawHorizontalPartition(b, yPositionOnScreen + borderWidth + 128 + 4, small: true);
        }
        if (RenamableEntries.Count > 1)
        {
            drawHorizontalPartition(b, yPositionOnScreen + borderWidth + 192 + 32 + 20, small: true);
        }
        if (RenamableEntries.Count > 2)
        {
            drawHorizontalPartition(b, yPositionOnScreen + borderWidth + 320 + 36, small: true);
        }
        if (RenamableEntries.Count > 3)
        {
            drawHorizontalPartition(b, yPositionOnScreen + borderWidth + 384 + 32 + 52, small: true);
        }
        for (int i = slotPosition; i < slotPosition + slotsOnPage && i < RenamableEntries.Count; i++)
        {
            if (GetEntry(i) != null)
            {
                drawRenamableSlot(b, i);
            }
        }
        Rectangle newClip = b.GraphicsDevice.ScissorRectangle;
        newClip.Y = Math.Max(0, rowPosition(4 - RenamableEntries.Count));
        newClip.Height -= newClip.Y;
        if (newClip.Height > 0)
        {
            int heightOverride = ((RenamableEntries.Count >= slotsOnPage) ? (-1) : ((108 + RenamableEntries.Count) * RenamableEntries.Count));
            drawVerticalPartition(b, xPositionOnScreen + 448 + 12, small: true, -1, -1, -1, heightOverride);
        }

        if (showScrollbar)
        {
            upButton.draw(b);
            downButton.draw(b);
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), scrollBarRunner.X, scrollBarRunner.Y, scrollBarRunner.Width, scrollBarRunner.Height, Color.White, 4f);
            scrollBar.draw(b);
        }
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        
        base.draw(b);
        
        if (!Game1.options.hardwareCursor)
        {
            drawMouse(b, ignore_transparency: true);
        }
    }
}