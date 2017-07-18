﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu
{
    class Program : IConfigurable {

        public static char commandChar = '!';

        public static Command [ ] commands = new Command [ ] {
            new CCommandList (), new CSetColor (), new CRollTheDice (),
            new CFlipCoin (), new CRandomGame (), new CQuote (), new CEmbolden (),
            new CAddHeader (), new CShowHeaders (), new CKarma (), new CReport (),
            new VoiceCommands (), new EventCommands (), new UserSettingsCommands (), new DebugCommands (), new HangmanCommands (),
            new GameCommands (), new StrikeCommandSet (), new CAddEventGame (), new CRemoveEventGame (), new CHighlightEventGame (),
            new CAcceptYoungling (), new CReloadConfiguration (),
        };

        public static string dataPath = "";
        public static MessageControl messageControl = null;
        public static Clock clock;
        public static Karma karma;

        public static string commandSettingsDirectory = "Command Settings/";
        public static string chatlogDirectory = "ChatLogs/";
        public static string resourceDirectory = "Resources/";
        public static string eventDirectory = "Event/";
        public static string gitHubIgnoreType = ".botproperty";
        public static string avatarPath = "avatar.jpg";

        public static void Main (string[] args) {
            new Program ().ErrorCatcher (args);
        }

        public static DiscordSocketClient discordClient;

        private const int BOOT_WAIT_TIME = 5;
        private static DateTime bootedTime = new DateTime ();

        // SocketGuild data
        public static string mainTextChannelName = "";
        public static string dumpTextChannelName = "";
        public static string serverName = "";
        public static ulong serverID = 0;

        public static Phrase [ ] phrases = new Phrase [ ] {
            new Phrase ("Neat!", 0, 100, "Very!", 0, ""),
            new Phrase ("", 93732620998819840, 1, "*Allegedly...*", 0, ""),
            new Phrase ("wow!", 94089489227448320, 100, "INSANE AIR TIME!", 0, ""),
            new Phrase ("Thx fam", 0, 100, "No probs. We Gucci.", 0, ""),
            new Phrase ("<:Serviet:255721870828109824> Privet Comrades!", 172012092407152640, 100, "Privet, federal leader!", 0, ""),
            new Phrase ("<:Serviet:255721870828109824> Privet Comrades!", 0, 100, "Privet!", 0, ""),
            new Phrase ("Who is best gem?", 93732620998819840, 100, "*Obviously* <:Lapis:230346614064021505> ...", 0, ""),
            new Phrase ("Who is best gem?", 0, 100, "Obviously <:PeriWow:230381627669348353>", 0, ""),
            new Phrase ("https://www.reddit.com/", 93732620998819840, 100, "Wow, this is some very interesting conte- <:residentsleeper:257933177631277056> Zzz", 171667949155778561, ""),
            new Phrase ("", 174097001003220992, 2, "¯\\_(ツ)_/¯", 0, ""),
            new Phrase ("(╯°□°）╯︵ ┻━┻", 0, 100, "Please respect tables. ┬─┬ノ(ಠ_ಠノ)", 0, ""),
            new Phrase ("nice", 95463258181345280, 25, "Very nice!", 0, ""),
            new Phrase ("Neato", 0, 100, "", 0, "🌯"),
        };
        public static List<string> allowedDeletedMessages = new List<string>();

        // Feedback
        public static ulong authorID = 93744415301971968;

        public async Task ErrorCatcher (string[] args) {
            try {
                await new Program ().Start (args);
            } catch (Exception e) {
                ChatLogger.DebugLog (e.Message + "\n" + e.StackTrace);
            }
        }

        public void LoadConfiguration() {
            mainTextChannelName = BotConfiguration.GetSetting ("MainTextChannelName", "general");
            dumpTextChannelName = BotConfiguration.GetSetting ("DumpTextChannelName", "dump");
            serverName = BotConfiguration.GetSetting ("ServerName", "Discord Server");
            serverID = BotConfiguration.GetSetting<ulong> ("ServerID", 0);
            phrases = BotConfiguration.GetSetting ("ResponsePhrases", new Phrase [ ] { new Phrase ("Neat!", 0, 100, "Very!", 0, ""), new Phrase ("Neato", 0, 100, "", 0, "🌯") });
        }

        public async Task Start(string [ ] args) {

            // Linux specific test
            if (args.Length > 0) {
                dataPath = args [ 0 ] + "/data/";
            } else {
                dataPath = AppContext.BaseDirectory + "/data/";
            }

            dataPath = dataPath.Replace ('\\', '/');
            InitializeDirectories ();
            ChatLogger.Log ("Booting.. Datapath: " + dataPath);
            BotConfiguration.Initialize ();
            BotConfiguration.AddConfigurable (this);

            discordClient = new DiscordSocketClient ();
            messageControl = new MessageControl ();
            karma = new Karma ();

            UserConfiguration.Initialize ();
            LoadConfiguration ();
            clock = new Clock ();

            InitializeData ();
            InitializeCommands ();
            UserGameMonitor.Initialize ();

            bootedTime = DateTime.Now.AddSeconds (BOOT_WAIT_TIME);

            discordClient.MessageReceived += (e) => {

                ChatLogger.Log (Utility.GetChannelName (e) + " says: " + e.Content);
                if (e.Author.Id != discordClient.CurrentUser.Id && e.Content.Length > 0 && e.Content [ 0 ] == commandChar) {
                    string message = e.Content;

                    if (message.Length > 0) {

                        message = message.Substring (1);
                        string command = "";
                        List<string> arguments = Utility.ConstructArguments (message, out command);

                        FindAndExecuteCommand (e, command, arguments, commands);
                    }
                }

                FindPhraseAndRespond (e);

                if (e.Content.Length > 0 && e.Content [ 0 ] == commandChar) {
                    e.DeleteAsync ();
                    allowedDeletedMessages.Add (e.Content);
                }

                return Task.CompletedTask;
            };

            discordClient.UserJoined += async (e) => {
                Younglings.OnUserJoined (e);
                messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, "**" + e.Username + "** has joined this server. Bid them welcome or murder them in cold blood, it's really up to you.", true);

                string[] welcomeMessage = SerializationIO.LoadTextFile (dataPath + "welcomemessage" + gitHubIgnoreType);
                string combined = "";
                for (int i = 0; i < welcomeMessage.Length; i++) {
                    combined += welcomeMessage[i] + "\n";
                }

                await messageControl.SendMessage (e, combined);
            };

            discordClient.UserLeft += (e) => {
                messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, "**" + Utility.GetUserName (e) + "** has left the server. Don't worry, they'll come crawling back soon.", true);
                return Task.CompletedTask;
            };

            discordClient.UserVoiceStateUpdated += async (user, before, after) => {
                ChatLogger.Log ("User voice updated: " + user.Username);
                SocketGuild guild = (user as SocketGuildUser).Guild;

                if (after.VoiceChannel != null)
                    AutomatedVoiceChannels.allVoiceChannels [ after.VoiceChannel.Id ].OnUserJoined (user as SocketGuildUser);

                await AutomatedVoiceChannels.OnUserUpdated (guild, before.VoiceChannel, after.VoiceChannel);

                return;
            };

            discordClient.GuildMemberUpdated += async (before, after) => {
                SocketGuild guild = (before as SocketGuildUser).Guild;

                SocketGuildChannel channel = Utility.GetMainChannel ();
                await AutomatedVoiceChannels.OnUserUpdated (guild, before.VoiceChannel, after.VoiceChannel);

                if ((before as SocketGuildUser).Nickname != (after as SocketGuildUser).Nickname) {
                    messageControl.SendMessage (channel as SocketTextChannel, "**" + Utility.GetUserUpdateName (before as SocketGuildUser, after as SocketGuildUser, true) + "** has changed their nickname to **" + Utility.GetUserUpdateName (before as SocketGuildUser, after as SocketGuildUser, false) + "**", true);
                }
            };

            discordClient.UserUpdated += (before, after) => {
                ChatLogger.Log ("User " + before.Username + " updated.");

                SocketTextChannel channel = Utility.GetMainChannel () as SocketTextChannel;

                if (channel == null)
                    return Task.CompletedTask;

                if (before.Username != after.Username) {
                    messageControl.SendMessage (channel as SocketTextChannel, "**" + Utility.GetUserUpdateName (before as SocketGuildUser, after as SocketGuildUser, true) + "** has changed their name to **" + after.Username + "**", true);
                }

                return Task.CompletedTask;
            };

            discordClient.UserBanned += (e, guild) => {
                SocketChannel channel = Utility.GetMainChannel ();
                if (channel == null)
                    return Task.CompletedTask;

                messageControl.SendMessage (channel as SocketTextChannel, "**" + Utility.GetUserName (e as SocketGuildUser) + "** has been banned from this server, they will not be missed.", true);
                messageControl.SendMessage (e as SocketGuildUser, "Sorry to tell you like this, but you have been permabanned from Monster Mash. ;-;");

                return Task.CompletedTask;
            };

            discordClient.UserUnbanned += (e, guild) => {
                SocketChannel channel = Utility.GetMainChannel ();
                if (channel == null)
                    return Task.CompletedTask;

                messageControl.SendMessage (channel as SocketTextChannel, "**" + Utility.GetUserName (e as SocketGuildUser) + "** has been unbanned from this server, They are once more welcome in our glorious embrace.", true);
                messageControl.SendMessage (e as SocketGuildUser, "You have been unbanned from Monster Mash, we love you once more! :D");

                return Task.CompletedTask;
            };

            discordClient.MessageDeleted += (message, channel) => {
                if (channel == null)
                    return Task.CompletedTask;

                if (message.HasValue) {
                    if (!allowedDeletedMessages.Contains (message.Value.Content)) {
                        messageControl.SendMessage (channel as SocketTextChannel, "In order disallow *any* secrets except for admin secrets, I'd like to tell you that **" + Utility.GetUserName (message.Value.Author as SocketGuildUser) + "** just had a message deleted on **" + message.Value.Channel.Name + "**.", true);
                    } else {
                        allowedDeletedMessages.Remove (message.Value.Content);
                    }
                }

                return Task.CompletedTask;
            };

            discordClient.Ready += () => {
                ChatLogger.Log ("Bot is ready and running!");
                return Task.CompletedTask;
            };

            string token = SerializationIO.LoadTextFile (dataPath + "bottoken" + gitHubIgnoreType)[0];

            ChatLogger.Log ("Connecting to Discord..");
            await discordClient.LoginAsync (TokenType.Bot, token);
            await discordClient.StartAsync ();

            BotConfiguration.PostInit ();

            await Task.Delay (-1);
        }

        private static bool hasBooted = false;
        public static bool FullyBooted () {
            if (hasBooted)
                return hasBooted;

            if (Utility.GetServer () != null) {
                hasBooted = true;
                ChatLogger.Log ("Booted flag set to true.");
            }
            return hasBooted;
        }

        private void InitializeData () {
            AutomatedVoiceChannels.InitializeData ();
        }

        public static void InitializeDirectories () {
            CreateAbsentDirectory (dataPath + chatlogDirectory);
        }

        public static void CreateAbsentDirectory (string path) {
            if (!Directory.Exists (path))
                Directory.CreateDirectory (path);
        }

        public static void InitializeCommands () {
            for (int i = 0; i < commands.Length; i++) {
                commands[i].Initialize ();
            }
        }

        public static Command FindCommand (string commandName) {
            for (int i = 0; i < commands.Length; i++) {
                if (commands[i].command.ToUpper () == commandName.ToUpper ())
                    return commands[i];
            }
            return null;
        }

        public static bool FindAndExecuteCommand (SocketMessage e, string commandName, List<string> arguements, Command[] commandList) {
            for (int i = 0; i < commandList.Length; i++) {
                if (commandList[i].command == commandName) {
                    if (arguements.Count > 0 && arguements [ 0 ] == "?") {
                        Command command = commandList [ i ];
                        messageControl.SendMessage (e as SocketUserMessage, command.GetHelp (), false);
                    } else
                        commandList [ i ].ExecuteCommand (e as SocketUserMessage, arguements);
                    return true;
                }
            }

            return false;
        }

        public void FindPhraseAndRespond (SocketMessage e) {
            for (int i = 0; i < phrases.Length; i++) {
                if (phrases[i].CheckAndRespond (e))
                    return;
            }
        }
    }
}
