using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerCheckPlugin.Commands
{
    public class CommandCheck : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer uCaller = (UnturnedPlayer)caller;

            if (command.Length == 0)
            {
                UnturnedChat.Say(uCaller, "Usage: /check <player name/steamID>", Color.red);
                return;
            }

            UnturnedPlayer target = UnturnedPlayer.FromName(command[0]);
            if (target == null)
            {
                if (ulong.TryParse(command[0], out ulong steamID))
                {
                    target = UnturnedPlayer.FromCSteamID(new CSteamID(steamID));
                }

                if (target == null)
                {
                    UnturnedChat.Say(uCaller, "Player not found.", Color.red);
                    return;
                }
            }

            bool success = PlayerCheckPlugin.Instance.CheckManager.StartCheck(uCaller, target);
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "check";
        public string Help => "Starts checking a player";
        public string Syntax => "<player name/steamID>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "PlayerCheck.Moderator" };
    }
}