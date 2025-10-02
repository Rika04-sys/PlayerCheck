using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerCheckPlugin.Commands
{
    public class CommandEndCheck : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer uCaller = (UnturnedPlayer)caller;

            UnturnedPlayer target = null;
            if (command.Length > 0)
            {
                target = UnturnedPlayer.FromName(command[0]);
                if (target == null && ulong.TryParse(command[0], out ulong steamID))
                {
                    target = UnturnedPlayer.FromCSteamID(new CSteamID(steamID));
                }

                if (target == null)
                {
                    UnturnedChat.Say(uCaller, "Player not found.", Color.red);
                    return;
                }
            }

            bool success = PlayerCheckPlugin.Instance.CheckManager.EndCheck(uCaller, target);
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "endcheck";
        public string Help => "Ends a player check";
        public string Syntax => "[player name/steamID]";
        public List<string> Aliases => new List<string> { "echeck" };
        public List<string> Permissions => new List<string> { "PlayerCheck.Moderator" };
    }
}