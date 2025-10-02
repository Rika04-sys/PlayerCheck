using Rocket.API;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace PlayerCheckPlugin
{
    public class PlayerCheckManager
    {
        private const ushort DEFAULT_CHECK_UI_ID = 9106;
        private static readonly Guid DEFAULT_CHECK_UI_GUID = new Guid("2be5f64b-a8a9-468d-851f-f0641420f750");

        private Dictionary<ulong, ulong> _activeChecks = new Dictionary<ulong, ulong>();
        private Dictionary<ulong, DateTime> _checkStartTimes = new Dictionary<ulong, DateTime>();

        private EffectAsset _checkEffectAsset;

        public PlayerCheckManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            LoadEffectAsset();
        }

        public void Deinitialize()
        {
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;

            foreach (var targetSteamID in _activeChecks.Keys.ToList())
            {
                EndCheckForPlayer(targetSteamID);
            }
            _activeChecks.Clear();
            _checkStartTimes.Clear();
        }

        private void LoadEffectAsset()
        {
            try
            {
                var config = PlayerCheckPlugin.Instance.Configuration.Instance;
                Guid effectGUID;

                if (!string.IsNullOrEmpty(config.CheckUIEffectGUID) && Guid.TryParse(config.CheckUIEffectGUID, out effectGUID))
                {
                    _checkEffectAsset = Assets.find(effectGUID) as EffectAsset;
                }

                if (_checkEffectAsset == null)
                {
                    _checkEffectAsset = Assets.find(DEFAULT_CHECK_UI_GUID) as EffectAsset;
                }

                if (_checkEffectAsset != null)
                {
                    Logger.Log($"Check UI effect loaded: {_checkEffectAsset.name}");
                }
                else
                {
                    Logger.LogError("Failed to load check UI effect!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading effect asset: {ex.Message}");
            }
        }

        private void OnPlayerDisconnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            if (player == null) return;

            ulong steamID = player.CSteamID.m_SteamID;

            if (_activeChecks.ContainsKey(steamID))
            {
                EndCheckForPlayer(steamID);
            }

            var checksByModerator = _activeChecks.Where(x => x.Value == steamID).ToList();
            foreach (var check in checksByModerator)
            {
                EndCheckForPlayer(check.Key);
            }
        }

        private bool IsModerator(UnturnedPlayer player)
        {
            var config = PlayerCheckPlugin.Instance.Configuration.Instance;
            return player != null && player.HasPermission(config.ModeratorPermission);
        }

        public bool StartCheck(UnturnedPlayer moderator, UnturnedPlayer target)
        {
            if (moderator == null || target == null) return false;

            var config = PlayerCheckPlugin.Instance.Configuration.Instance;

            if (!IsModerator(moderator))
            {
                UnturnedChat.Say(moderator, "You don't have permission to use this command.", Color.red);
                return false;
            }

            ulong moderatorSteamID = moderator.CSteamID.m_SteamID;
            ulong targetSteamID = target.CSteamID.m_SteamID;

            if (_activeChecks.ContainsKey(targetSteamID))
            {
                UnturnedChat.Say(moderator, $"Player {target.CharacterName} is already under check.", Color.red);
                return false;
            }

            if (moderatorSteamID == targetSteamID && !config.AllowSelfCheck)
            {
                UnturnedChat.Say(moderator, "You cannot check yourself.", Color.red);
                return false;
            }

            if (IsModerator(target) && !config.AllowModeratorCheck)
            {
                UnturnedChat.Say(moderator, "You cannot check another moderator.", Color.red);
                return false;
            }

            _activeChecks[targetSteamID] = moderatorSteamID;
            _checkStartTimes[targetSteamID] = DateTime.Now;

            ShowCheckUI(target);

            UnturnedChat.Say(moderator, $"You started checking player {target.CharacterName}", Color.green);
            UnturnedChat.Say(target, $"Moderator {moderator.CharacterName} started checking you. Use /contact to communicate.", Color.yellow);
            return true;
        }

        public bool CancelCheck(UnturnedPlayer moderator, UnturnedPlayer target = null)
        {
            if (moderator == null) return false;

            if (!IsModerator(moderator))
            {
                UnturnedChat.Say(moderator, "You don't have permission to use this command.", Color.red);
                return false;
            }

            ulong moderatorSteamID = moderator.CSteamID.m_SteamID;

            if (target == null)
            {
                var checksByModerator = _activeChecks.Where(x => x.Value == moderatorSteamID).ToList();
                if (checksByModerator.Count == 0)
                {
                    UnturnedChat.Say(moderator, "You don't have any active checks.", Color.red);
                    return false;
                }

                foreach (var check in checksByModerator)
                {
                    EndCheckForPlayer(check.Key, true);
                }

                UnturnedChat.Say(moderator, "All your checks have been cancelled.", Color.green);
                return true;
            }
            else
            {
                ulong targetSteamID = target.CSteamID.m_SteamID;

                if (!_activeChecks.TryGetValue(targetSteamID, out ulong actualModeratorSteamID) ||
                    actualModeratorSteamID != moderatorSteamID)
                {
                    UnturnedChat.Say(moderator, $"Player {target.CharacterName} is not under your check.", Color.red);
                    return false;
                }

                EndCheckForPlayer(targetSteamID, true);
                UnturnedChat.Say(moderator, $"Check for player {target.CharacterName} has been cancelled.", Color.green);
                return true;
            }
        }

        public bool EndCheck(UnturnedPlayer moderator, UnturnedPlayer target = null)
        {
            if (moderator == null) return false;

            if (!IsModerator(moderator))
            {
                UnturnedChat.Say(moderator, "You don't have permission to use this command.", Color.red);
                return false;
            }

            ulong moderatorSteamID = moderator.CSteamID.m_SteamID;

            if (target == null)
            {
                var checksByModerator = _activeChecks.Where(x => x.Value == moderatorSteamID).ToList();
                if (checksByModerator.Count == 0)
                {
                    UnturnedChat.Say(moderator, "You don't have any active checks.", Color.red);
                    return false;
                }

                foreach (var check in checksByModerator)
                {
                    EndCheckForPlayer(check.Key, false);
                }

                UnturnedChat.Say(moderator, "All your checks have been completed.", Color.green);
                return true;
            }
            else
            {
                ulong targetSteamID = target.CSteamID.m_SteamID;

                if (!_activeChecks.TryGetValue(targetSteamID, out ulong actualModeratorSteamID) ||
                    actualModeratorSteamID != moderatorSteamID)
                {
                    UnturnedChat.Say(moderator, $"Player {target.CharacterName} is not under your check.", Color.red);
                    return false;
                }

                EndCheckForPlayer(targetSteamID, false);
                UnturnedChat.Say(moderator, $"Check for player {target.CharacterName} has been completed.", Color.green);
                return true;
            }
        }

        public bool SendContactMessage(UnturnedPlayer sender, string message)
        {
            if (sender == null || string.IsNullOrEmpty(message)) return false;

            ulong senderSteamID = sender.CSteamID.m_SteamID;

            if (!_activeChecks.TryGetValue(senderSteamID, out ulong moderatorSteamID))
            {
                UnturnedChat.Say(sender, "You are not under check.", Color.red);
                return false;
            }

            UnturnedPlayer moderator = UnturnedPlayer.FromCSteamID(new CSteamID(moderatorSteamID));
            if (moderator == null)
            {
                UnturnedChat.Say(sender, "Moderator is currently unavailable.", Color.red);
                return false;
            }

            UnturnedChat.Say(moderator, $"[CHECK] {sender.CharacterName}: {message}", Color.cyan);
            UnturnedChat.Say(sender, $"Message sent to moderator: {message}", Color.yellow);
            return true;
        }

        private void ShowCheckUI(UnturnedPlayer target)
        {
            if (target == null || _checkEffectAsset == null) return;

            try
            {
                HideCheckUI(target);

                var config = PlayerCheckPlugin.Instance.Configuration.Instance;
                ushort effectID = config.CheckUIEffectID;

                EffectManager.sendUIEffect(effectID, (short)effectID, target.SteamPlayer().transportConnection, true);

                EffectManager.sendUIEffectText((short)effectID, target.SteamPlayer().transportConnection, true,
                    "Check_Status", "You are under check");
                EffectManager.sendUIEffectText((short)effectID, target.SteamPlayer().transportConnection, true,
                    "Check_Instruction", "Use /contact to communicate with moderator");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error showing check UI: {ex.Message}");
            }
        }

        private void HideCheckUI(UnturnedPlayer target)
        {
            if (target == null) return;

            try
            {
                var config = PlayerCheckPlugin.Instance.Configuration.Instance;
                ushort effectID = config.CheckUIEffectID;

                EffectManager.askEffectClearByID(effectID, target.SteamPlayer().transportConnection);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error hiding check UI: {ex.Message}");
            }
        }

        private void EndCheckForPlayer(ulong targetSteamID, bool isCancelled = false)
        {
            if (!_activeChecks.TryGetValue(targetSteamID, out ulong moderatorSteamID))
                return;

            UnturnedPlayer targetPlayer = UnturnedPlayer.FromCSteamID(new CSteamID(targetSteamID));
            UnturnedPlayer moderatorPlayer = UnturnedPlayer.FromCSteamID(new CSteamID(moderatorSteamID));

            if (targetPlayer != null)
            {
                HideCheckUI(targetPlayer);

                string endMessage = isCancelled ? "Check has been cancelled." : "Check has been completed.";
                UnturnedChat.Say(targetPlayer, endMessage, isCancelled ? Color.yellow : Color.green);
            }

            if (moderatorPlayer != null && targetPlayer != null)
            {
                string endMessage = isCancelled ?
                    $"Check for player {targetPlayer.CharacterName} has been cancelled." :
                    $"Check for player {targetPlayer.CharacterName} has been completed.";

                UnturnedChat.Say(moderatorPlayer, endMessage, isCancelled ? Color.yellow : Color.green);
            }

            _activeChecks.Remove(targetSteamID);
            _checkStartTimes.Remove(targetSteamID);
        }

        public bool IsPlayerUnderCheck(ulong steamID)
        {
            return _activeChecks.ContainsKey(steamID);
        }

        public ulong GetPlayerModerator(ulong targetSteamID)
        {
            return _activeChecks.TryGetValue(targetSteamID, out ulong moderatorSteamID) ? moderatorSteamID : 0;
        }

        public bool IsPlayerModerator(ulong steamID)
        {
            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(new CSteamID(steamID));
            return IsModerator(player);
        }
    }
}