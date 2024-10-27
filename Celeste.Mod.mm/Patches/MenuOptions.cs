#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
// pragma balls lmao

using FMOD.Studio;
using Celeste.Mod.Core;
using Celeste.Mod;
using Microsoft.Xna.Framework;

namespace Celeste {
    class patch_MenuOptions  {

        public static extern TextMenu orig_Create(bool inGame = false, EventInstance snapshot = null);
        public static TextMenu Create(bool inGame = false, EventInstance snapshot = null) {
            
            // Create the original options menu
            patch_TextMenu menu = (patch_TextMenu)orig_Create(inGame, snapshot);

            // Get the index of the photosensitive mode option
            int photosensitiveIndex = menu.GetItems().FindIndex(item =>
                item.GetType() == typeof(TextMenu.OnOff) && ((TextMenu.OnOff) item).Label == Dialog.Clean("OPTIONS_DISABLE_FLASH"));

            // Create all of our submenu options
            TextMenu.Item distort = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_COREMODULE_PSDISTORT"), CoreModule.Settings.PhotosensitivityDistortOverride)
                .Change(value => {
                    CoreModule.Settings.PhotosensitivityDistortOverride = value;
                });

            TextMenu.Item glitch = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_COREMODULE_PSGLITCH"), CoreModule.Settings.PhotosensitivityGlitchOverride)
                .Change(value => {
                    CoreModule.Settings.PhotosensitivityGlitchOverride = value;
                });

            TextMenu.Item lightning = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_COREMODULE_PSLIGHTNING"), CoreModule.Settings.PhotosensitivityLightningOverride)
                .Change(value => {
                    CoreModule.Settings.PhotosensitivityLightningOverride = value;
                });

            TextMenu.Item screenFlash = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_COREMODULE_PSSCREENFLASH"), CoreModule.Settings.PhotosensitivityScreenFlashOverride)
                .Change(value => {
                    CoreModule.Settings.PhotosensitivityScreenFlashOverride = value;
                });

            TextMenu.Item textHighlight = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_COREMODULE_PSTEXTHIGHLIGHT"), CoreModule.Settings.PhotosensitivityTextHighlightOverride)
                .Change(value => {
                    CoreModule.Settings.PhotosensitivityTextHighlightOverride = value;
                });

            // Put all the options into a big submenu
            TextMenuExt.SubMenu submenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTIONS_COREMODULE_PSOPTIONS"), false)
                .Add(distort)
                .Add(glitch)
                .Add(lightning)
                .Add(screenFlash)
                .Add(textHighlight);

            // Create a master switch that toggles the submenu to replace the existing photosensitive mode option
            TextMenu.Item masterSwitch = new TextMenu.OnOff(Dialog.Clean("OPTIONS_DISABLE_FLASH"), Settings.Instance.DisableFlashes)
                .Change(value => {
                    Settings.Instance.DisableFlashes = value;
                    submenu.Disabled = !value;
                });

            // Remove the existing photosensitive menu and replace it with our master switch
            menu.Remove(menu.GetItems()[photosensitiveIndex]);
            menu.Insert(photosensitiveIndex, masterSwitch);

            // Insert the submenu at the correct point
            menu.Insert(photosensitiveIndex + 1, submenu);

            // TODO: Give each item a description

            // Send back the menu
            return menu;
        }
    }
}
