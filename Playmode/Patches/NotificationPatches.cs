using HarmonyLib;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.ShowNotification))]
    public static class NotificationPatches
    {
        public static bool FlashlightNotifications { get; set; } = true;
        public static bool CubeDestroyedNotifications { get; set; } = true;
        static bool Prefix(InGameUIManager.NotificationType _type)
        {
            if (PlayModeController.Instance && (_type == InGameUIManager.NotificationType.CubeDestroyed || _type == InGameUIManager.NotificationType.CubeReinitialized || _type == InGameUIManager.NotificationType.CubeRespawned))
            {
                return CubeDestroyedNotifications;
            }
            else if(PlayModeController.Instance && (_type == InGameUIManager.NotificationType.FlashlightNotOperational || _type == InGameUIManager.NotificationType.FlashlightOperational))
            {
                return FlashlightNotifications;
            }
            return true;
        }
    }
}