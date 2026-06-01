using System.Reflection;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace TOR_WarSails
{
    /// <summary>
    /// Entry point for the TOR War Sails bridge module.
    ///
    /// This module assumes the War Sails DLC is owned and enabled (it is a hard
    /// <c>DependedModule</c> in SubModule.xml, so the launcher enforces its presence).
    /// The runtime check below is a belt-and-suspenders sanity guard that surfaces a
    /// clear message if the DLC is somehow missing, rather than failing obscurely later.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        // TODO: confirm the exact War Sails module Id from its SubModule.xml in a War Sails install.
        private const string WarSailsModuleId = "WarSails";

        /// <summary>Harmony instance for this module's patches (mirrors TOR_Core's pattern).</summary>
        public static Harmony HarmonyInstance { get; private set; }

        private bool _warSailsPresent;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            _warSailsPresent = IsModulePresent(WarSailsModuleId);

            // Apply all [HarmonyPatch] types declared in this assembly. Patches go under the
            // Patches namespace and are picked up automatically here.
            HarmonyInstance = new Harmony("mod.harmony.theoldrealms.warsails");
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            Debug.Print($"[TOR_WarSails] Bridge module loaded. War Sails present: {_warSailsPresent}.");
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (_warSailsPresent)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=tor_warsails_loaded}The Old Realms: War Sails integration is active.").ToString(),
                    Colors.Green));
            }
            else
            {
                // Should be unreachable while War Sails is a hard DependedModule, but warn clearly if it ever happens.
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=tor_warsails_missing}TOR War Sails bridge is enabled but the War Sails DLC was not found. Naval features are disabled.").ToString(),
                    Colors.Red));
            }
        }

        private static bool IsModulePresent(string moduleId)
        {
            return !string.IsNullOrEmpty(moduleId) && ModuleHelper.GetModuleInfo(moduleId) != null;
        }
    }
}
