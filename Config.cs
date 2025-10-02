using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
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
    public class PlayerCheckPlugin : RocketPlugin<PlayerCheckConfiguration>
    {
        public static PlayerCheckPlugin Instance { get; private set; }
        public PlayerCheckManager CheckManager;

        protected override void Load()
        {
            Instance = this;
            CheckManager = new PlayerCheckManager();

            Logger.Log("=== PlayerCheck Loaded ===");
            Logger.Log("Commands:");
            Logger.Log("/check <player> - Start player check");
            Logger.Log("/cancelcheck [player] - Cancel check");
            Logger.Log("/endcheck [player] - End check");
            Logger.Log("/contact <message> - Contact moderator during check");
            Logger.Log("By Rika");
            Logger.Log("==================================");
        }

        protected override void Unload()
        {
            CheckManager?.Deinitialize();
            CheckManager = null;
            Instance = null;

            Logger.Log("PlayerCheck Plugin Unloaded");
        }
    }

    public class PlayerCheckConfiguration : IRocketPluginConfiguration
    {
        public ushort CheckUIEffectID { get; set; }
        public string CheckUIEffectGUID { get; set; }
        public string ModeratorPermission { get; set; }
        public bool AllowSelfCheck { get; set; }
        public bool AllowModeratorCheck { get; set; }

        public void LoadDefaults()
        {
            CheckUIEffectID = 9106;
            CheckUIEffectGUID = "2be5f64b-a8a9-468d-851f-f0641420f750";
            ModeratorPermission = "PlayerCheck.Moderator";
            AllowSelfCheck = false;
            AllowModeratorCheck = false;
        }
    }
}