﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CRollTheDice : Command {

        public CRollTheDice () {
            command = "rtd";
            name = "Roll the Dice";
            argHelp = "<maxnumber>";
            help = "Rolls a dice that returns a number between one and " + argHelp + ".";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Random random = new Random ();
                int number;

                if (int.TryParse (arguments[0], out number)) {
                    Program.messageControl.SendMessage(e, "You rolled " + (random.Next(number) + 1).ToString());
                }
            }
            return Task.CompletedTask;
        }
    }
}
