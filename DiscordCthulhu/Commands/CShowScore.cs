﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CShowScore : Command {

        public CShowScore () {
            command = "showscore";
            name = "Show Score";
            help = "Shows you your own score.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {

            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Program.messageControl.SendMessage (e, "Your current score is " + Program.scoreCollection.GetScore (e.Author.Username));
            }
            return Task.CompletedTask;
        }
    }
}
