using GenericModConfigMenu;
using HaveMoreKids;
using JetBrains.Annotations;
using Leclair.Stardew.BetterGameMenu;
using LeFauxMods.Common.Integrations.IconicFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace Renamer
{
    [UsedImplicitly]
    internal sealed class ModEntry : Mod
    {
        internal static IMonitor MMonitor = null!;
        internal static ITranslationHelper TranslationHelper = null!;
        internal static IBetterGameMenuApi? BetterGameMenuApi;
        internal static IGenericModConfigMenuApi? GenericModConfigMenuApi;

        private static FakeManifest? GMCMManifest;
        private static ModConfig Config = null!;

        public const string MENU_ID = "irocendar.renamer";
        
        public override void Entry(IModHelper helper)
        {
            MMonitor = Monitor;
            TranslationHelper = helper.Translation;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            
            Config = helper.ReadConfig<ModConfig>();
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Config.KeyBinds.JustPressed())
            {
                if (Game1.activeClickableMenu is RenamableMenu renamableMenu)
                {
                    if (renamableMenu.GetChildMenu() is null) Game1.exitActiveMenu();
                    return;
                }
                Game1.activeClickableMenu = new RenamableMenu(
                    Game1.uiViewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2,
                    Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2,
                    800 + IClickableMenu.borderWidth * 2 - 64 - 16, 600 + IClickableMenu.borderWidth * 2);
            }
            else if (e.Button is SButton.Escape or SButton.ControllerB)
            {
                if (Game1.activeClickableMenu is RenamableMenu renamableMenu &&
                    renamableMenu.GetChildMenu() is not null)
                {
                    renamableMenu.receiveEscKey();
                    Helper.Input.Suppress(SButton.Escape);
                }
                else if (Game1.activeClickableMenu is RenamingMenu renamingMenu)
                {
                    renamingMenu.exitFunction();
                    Helper.Input.Suppress(SButton.Escape);
                }
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            GenericModConfigMenuApi = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (GenericModConfigMenuApi is not null)
            {
                GMCMManifest = new FakeManifest(ModManifest, TranslationHelper);
                Helper.Events.Content.LocaleChanged += GMCMManifest.OnLocaleChanged;
                GenericModConfigMenuApi.Register(GMCMManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));
                
                GenericModConfigMenuApi.AddKeybindList(
                    GMCMManifest,
                    () => Config.KeyBinds,
                    value => Config.KeyBinds = value,
                    () => TranslationHelper.Get("Key"));
            }
            
            IHaveMoreKidsAPI? api = Helper.ModRegistry.GetApi<IHaveMoreKidsAPI>("mushymato.HaveMoreKids");
            HaveMoreKidsIntegration.Initialize(api, Helper.ModRegistry.IsLoaded("mushymato.HaveMoreKids"));

            BetterGameMenuApi = Helper.ModRegistry.GetApi<IBetterGameMenuApi>("leclair.bettergamemenu");
            BetterGameMenuApi?.RegisterTab(
                MENU_ID,
                (int) VanillaTabOrders.Animals + 5,
                () => TranslationHelper.Get("ModName"),
                () => (BetterGameMenuApi.CreateDraw(Helper.ModContent.Load<Texture2D>("assets/icon.png"), new Rectangle(0, 0, 10, 16), scale: 4f), true),
                1,
                bgm => new RenamableMenu(
                    bgm.xPositionOnScreen,
                    Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2,
                    800 + IClickableMenu.borderWidth * 2 - 64 - 16, 600 + IClickableMenu.borderWidth * 2,
                    false
                ),
                getWidth: _ => 800 + IClickableMenu.borderWidth * 2 - 64 - 16
            );

            IIconicFrameworkApi? ifApi = Helper.ModRegistry.GetApi<IIconicFrameworkApi>("furyx639.ToolbarIcons");
            ifApi?.AddToolbarIcon(
                Helper.ModContent.GetInternalAssetName("assets/icon_large.png").Name,
                new Rectangle(0, 0, 16, 16),
                () => TranslationHelper.Get("ModName"),
                null,
                () => Game1.activeClickableMenu = new RenamableMenu(
                    Game1.uiViewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2,
                    Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2,
                    800 + IClickableMenu.borderWidth * 2 - 64 - 16, 
                    600 + IClickableMenu.borderWidth * 2));
        }
    }
}