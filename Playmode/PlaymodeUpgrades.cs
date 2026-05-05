using FS_LevelEditor.Playmode.Patches;
using FS_LevelEditor.SaveSystem;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor.Playmode
{
    public static class PlaymodeUpgrades
    {
        public static List<UpgradeSaveData> targetUpgradesData;
        public static int totalUpgradeCount = 0;

        public static void ResetAllUpgradeEffects(bool allowJetpack)
        {
            // Force all upgrade-driven effects OFF/0 before applying data
            Controls.m_hasDodgeSkill = false;
            Controls.m_currentDodgeLevel = 0;

            Controls.m_hasSprintSkill = false;

            if (TimeManipulator.Instance)
                TimeManipulator.Instance.SetInPlayerPosession(false);

            if (!allowJetpack || allowJetpack)
                Controls.m_currentJetpackUpgradeLevel = 0; // level 0 by default regardless; allowJetpack only gates enabling later

            Controls.m_currentHealthUpgradeLevel = 0;
            Controls.m_currentSpeedUpgradeLevel = 0;
            Controls.m_currentTaserCapacityUpgradeLevel = 0;

            Controls.m_currentHealthBackpackLevel = 0;
            Controls.m_currentTaserBackpackLevel = 0;
            Controls.m_currentTaserPowerUpgradeLevel = 0;
            Controls.m_currentStealthUpgradeLevel = 0;
            Controls.m_currentAimStabilizerLevel = 0;
            Controls.m_currentHoverUpgradeLevel = 0;
            Controls.m_currentScopeLevel = 0;
            Controls.m_currentSafeLandingLevel = 0;
            Controls.m_currentUVFlashlightLevel = 0;
            Controls.m_currentScannerLevel = 0;
            Controls.DisableInfraredFlashlight();
        }

        public static void ApplyUpgrades(List<UpgradeSaveData> upgrades, bool allowJetpack = true)
        {
            targetUpgradesData = upgrades;

            // Always reset all effects first. Missing entries remain disabled.
            ResetAllUpgradeEffects(allowJetpack);

            totalUpgradeCount = 0;

            // If no upgrades provided by level data, leave everything disabled
            if (upgrades == null)
            {
                // Set StatsManager.totalUpgradesCount to 0 when no upgrades
                StatsManager.totalUpgradesCount = 0;
                return;
            }

            foreach (var upgrade in upgrades)
            {
                #region Safe Check Active And Level States
                if (!allowJetpack && upgrade.type == UpgradeType.JETPACK)
                {
                    upgrade.active = false;
                    upgrade.level = 0;
                }
                else
                {
                    upgrade.level = Math.Clamp(upgrade.level, 1, LevelData.GetUpgradeMaxLevel(upgrade.type));
                    upgrade.active = upgrade.active && upgrade.level > 0;

                    if (!upgrade.active)
                        upgrade.level = 0;
                }
                #endregion

                if (!upgrade.active)
                    continue;

                switch (upgrade.type)
                {
                    case UpgradeType.DODGE:
                        Controls.m_hasDodgeSkill = upgrade.active;
                        Controls.m_currentDodgeLevel = upgrade.level;
                        break;
                    case UpgradeType.SPRINT:
                        Controls.m_hasSprintSkill = upgrade.active;
                        break;
                    case UpgradeType.HYPER_SPEED:
                        TimeManipulator.Instance.SetInPlayerPosession(upgrade.active);
                        break;
                    case UpgradeType.JETPACK:
                        if (allowJetpack)
                            Controls.m_currentJetpackUpgradeLevel = upgrade.level;
                        break;
                    case UpgradeType.HEALTH:
                        Controls.m_currentHealthUpgradeLevel = upgrade.level;
                        break;
                    case UpgradeType.SPEED:
                        Controls.m_currentSpeedUpgradeLevel = upgrade.level;
                        break;
                    case UpgradeType.TASER_CAPACITY:
                        Controls.m_currentTaserCapacityUpgradeLevel = upgrade.level;
                        break;
                    case UpgradeType.HEALTH_BACKPACK:
                        Controls.m_currentHealthBackpackLevel = upgrade.level;
                        break;
                    case UpgradeType.TASER_BACKPACK:
                        Controls.m_currentTaserBackpackLevel = upgrade.level;
                        break;
                    case UpgradeType.TASER_POWER:
                        Controls.m_currentTaserPowerUpgradeLevel = upgrade.level;
                        break;
                    case UpgradeType.STEALTH:
                        Controls.m_currentStealthUpgradeLevel = upgrade.level;
                        break;
                    case UpgradeType.AIM_STABILIZER:
                        Controls.m_currentAimStabilizerLevel = upgrade.level;
                        break;
                    case UpgradeType.HOVER:
                        Controls.m_currentHoverUpgradeLevel = upgrade.level;
                        break;
                    case UpgradeType.SCOPE:
                        Controls.m_currentScopeLevel = upgrade.level;
                        break;
                    case UpgradeType.SAFE_LANDING:
                        Controls.m_currentSafeLandingLevel = upgrade.level;
                        break;
                    case UpgradeType.UV_FLASHLIGHT:
                        Controls.m_currentUVFlashlightLevel = upgrade.level;
                        if (upgrade.level > 0 && upgrade.active)
                            Controls.EnableInfraredFlashlight();
                        break;
                    case UpgradeType.SCANNER:
                        Controls.m_currentScannerLevel = upgrade.level;
                        break;
                }

                totalUpgradeCount += upgrade.level;
            }

            StatsManager.totalUpgradesCount = Math.Max(totalUpgradeCount, 0); // Ensure it's exactly 0 if no upgrades.

            Controls.RefreshUpgradeVariables();
            GunController.Instance.RefreshTaserModules();

            Controls.Instance.currentHP = Controls.Instance.currentMaxHP; // Heal to full after upgrades have been applied.

            UpgradePatches.Init();
        }
    }
}
