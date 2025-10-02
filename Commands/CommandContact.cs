using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerCheckPlugin.Commands
{
    public class CommandContact : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer uCaller = (UnturnedPlayer)caller;

            if (command.Length == 0)
            {
                UnturnedChat.Say(uCaller, "Usage: /contact <message>", Color.red);
                return;
            }

            string message = string.Join(" ", command);

            bool success = PlayerCheckPlugin.Instance.CheckManager.SendContactMessage(uCaller, message);
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "contact";
        public string Help => "Sends a message to moderator during check";
        public string Syntax => "<message>";
        public List<string> Aliases => new List<string> { "c" };
        public List<string> Permissions => new List<string>();
    }
}