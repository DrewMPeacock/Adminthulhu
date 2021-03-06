﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Rest;
using Newtonsoft.Json;

namespace Adminthulhu {
    public class Voice : IConfigurable {

        public static Dictionary<ulong, VoiceChannel> defaultChannels = new Dictionary<ulong, VoiceChannel> ();
        public static Dictionary<ulong, VoiceChannel> allVoiceChannels = new Dictionary<ulong, VoiceChannel> ();
        public static List<string> awaitingChannels = new List<string> ();

        public static int addChannelsIndex = 2;
        public static int fullChannels = 0;
        public static bool autoAddChannels = false;
        public static bool autoRenameChannels = false;
        public static bool shortenChannelNames = false;
        public static bool enableVoiceChannelTags = false;
        public static ulong musicBotID = 0;
        public static ulong internationalRoleID = 0;
        public static ulong younglingRoleID = 0;
        public static string postRebootChannelName = "ERROR - REBOOT CHANNEL;RBC";
        public static bool randomizeNameQueue = false;
        public static Dictionary<string, string> gameNameReplacements = new Dictionary<string, string> (); 

        public static string [ ] extraChannelNames = new string [ ] {
            "Gorgeous Green",
            "Pathetic Pink",
            "Adorable Amber",
            "Flamboyant Flamingo",
            "Cringy Cyan",
            "Ordinary Orange",
            "Unreal Umber",
            "Godlike Gold",
            "Zealot Zaffre",
            "Violent Violet",
            "Creepy Cardinal",
            "Salty Salmon",
            "Wanking White",
            "Indeginous Indigo",
            "Laughing Lime",
            "Mangled Magenta"
        };

        public static Dictionary<string, bool> nameQueue = new Dictionary<string, bool>();
        public static List<IVoiceChannel> temporaryChannels = new List<IVoiceChannel> ();
        public static VoiceChannel afkChannel = null;

        public static VoiceChannelTag [ ] voiceChannelTags = {
            new VoiceChannelTag ("MusicBotPresent", "🎵", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Find (x => x.Id == musicBotID) != null; } ),
            new VoiceChannelTag ("InternationalMemberPresent", "🌎", delegate (VoiceChannelTag.ActionData data) {
                SocketRole internationalRole = Utility.GetServer ().GetRole (internationalRoleID);
                data.hasTag = Utility.ForceGetUsers (data.channel.id).Find (x => x.Roles.Contains (internationalRole)) != null;  }),
            new VoiceChannelTag ("ChannelLocked", "🔒", delegate (VoiceChannelTag.ActionData data) { data.hasTag = data.channel.IsLocked (); }, delegate (VoiceChannel channel) { channel.Lock (0, false); channel.CheckLocker (); } ),
            //new VoiceChannelTag ("🍞", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Where (x => x.Id == 110406708299329536).Count () != 0; } ),
            new VoiceChannelTag ("ChannelLooking", "🎮", delegate (VoiceChannelTag.ActionData data) { data.hasTag = data.channel.status == VoiceChannel.VoiceChannelStatus.Looking; }, delegate (VoiceChannel channel) { channel.SetStatus (VoiceChannel.VoiceChannelStatus.Looking, false); }),
            new VoiceChannelTag ("ChannelFull", "❌", delegate (VoiceChannelTag.ActionData data) { data.hasTag = data.channel.status == VoiceChannel.VoiceChannelStatus.Full; }, delegate (VoiceChannel channel) { channel.SetStatus (VoiceChannel.VoiceChannelStatus.Full, false); }),
            new VoiceChannelTag ("ContainsEventMembers", "📆", delegate (VoiceChannelTag.ActionData data) {
                DiscordEvents.Event evt = null;
                data.hasTag = DiscordEvents.ContainsEventMembers ( out evt, Utility.ForceGetUsers (data.channel.id).ToArray ()); }),
            new VoiceChannelTag ("BirthdayCelebratorPresent", "🍰", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Find (x => Birthdays.IsUsersBirthday (x)) != null; }),
            new VoiceChannelTag ("StreamingCurrently","📹", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Where (x => x.Game.HasValue).Where (x => x.Game.Value.StreamType > 0).Count () != 0; }),
            new VoiceChannelTag ("ChannelIsLit","🔥", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Where (x => x.GuildPermissions.Administrator).Count () >= 3; }),
            new VoiceChannelTag ("YounglingPresent", "👶", delegate (VoiceChannelTag.ActionData data) {
                SocketRole younglingRole = Utility.GetServer ().GetRole (younglingRoleID);
                data.hasTag = Utility.ForceGetUsers (data.channel.id).Find (x => x.Roles.Contains (younglingRole)) != null;  }),
        };

        public void LoadConfiguration() {
            ResetData ();
            extraChannelNames = BotConfiguration.GetSetting<string [ ]> ("Voice.ExtraChannelNames", this, new string [ ] { "EXTRA_CHANNEL_1;EXTRA_CHANNEL_SHORT_NAME_1", "EXTRA_CHANNEL_1;EXTRA_CHANNEL_SHORT_NAME_2" });
            loadedChannels = BotConfiguration.GetSetting ("Voice.DefaultChannels", this, new VoiceChannel [ ] { new VoiceChannel (0, "DEFAULT_CHANNEL_NAME_1;SHORT_NAME_1", 0), new VoiceChannel (1, "DEFAULT_CHANNEL_NAME_2;SHORT_NAME_2", 0) });
            afkChannel = BotConfiguration.GetSetting ("Voice.AFKChannel", this, new VoiceChannel (2, "AFK_CHANNEL_NAME", int.MaxValue - 1));
            autoAddChannels = BotConfiguration.GetSetting ("Voice.AutoAddChannels", this, autoAddChannels);
            autoRenameChannels = BotConfiguration.GetSetting ("Voice.AutoRenameChannels", this, autoRenameChannels);
            shortenChannelNames = BotConfiguration.GetSetting ("Voice.ShortenChannelNames", this, shortenChannelNames);
            enableVoiceChannelTags = BotConfiguration.GetSetting ("Voice.ChannelTagsEnabled", this, enableVoiceChannelTags);
            younglingRoleID = BotConfiguration.GetSetting ("Roles.YounglingID", this, younglingRoleID);
            internationalRoleID = BotConfiguration.GetSetting ("Roles.InternationalID", this, internationalRoleID);
            musicBotID = BotConfiguration.GetSetting ("Misc.MusicBotID", this, musicBotID);
            postRebootChannelName = BotConfiguration.GetSetting ("Voice.PostRebootChannelName", this, postRebootChannelName);
            randomizeNameQueue = BotConfiguration.GetSetting ("Voice.RandomizeNameQueue", this, randomizeNameQueue);

            gameNameReplacements.Add ("Game name 1", "Game name replacement 1");
            gameNameReplacements.Add ("Game name 2", "Game name replacement 2");
            gameNameReplacements = BotConfiguration.GetSetting ("Voice.GameNameReplacements", this, gameNameReplacements);

            foreach (VoiceChannelTag tag in voiceChannelTags) {
                tag.enabled = BotConfiguration.GetSetting ("Voice.Tags." + tag.name + ".Enabled", this, tag.enabled);
                tag.tagEmoji = BotConfiguration.GetSetting ("Voice.Tags." + tag.name + ".Emoji", this, tag.tagEmoji);
            }

            foreach (VoiceChannel channel in loadedChannels) {
                AddDefaultChannel (channel);
            }
            addChannelsIndex = defaultChannels.Count;
            AddDefaultChannel (afkChannel);

            for (int i = 0; i < extraChannelNames.Length; i++) {
                nameQueue.Add (extraChannelNames [ i ], false);
            }
        }

        public class TemporaryChannelsChecker : IClockable {
            public Task Initialize(DateTime time) {
                return Task.CompletedTask;
            }

            public Task OnDayPassed(DateTime time) {
                return Task.CompletedTask;
            }

            public Task OnHourPassed(DateTime time) {
                return Task.CompletedTask;
            }

            public Task OnMinutePassed(DateTime time) {
                TestAndRemoveTemporaryChannels ();
                return Task.CompletedTask;
            }

            public Task OnSecondPassed(DateTime time) {
                return Task.CompletedTask;
            }
        }

        public static void ResetData() {
            allVoiceChannels = new Dictionary<ulong, VoiceChannel> ();
            defaultChannels = new Dictionary<ulong, VoiceChannel> ();
            awaitingChannels = new List<string> ();
            afkChannel = null;
            hasChecked = false;
            nameQueue = new Dictionary<string, bool> ();
        }

        public static VoiceChannel [ ] loadedChannels;
        public static void InitializeData() {
            Voice configurable = new Voice ();
            configurable.LoadConfiguration ();
            BotConfiguration.AddConfigurable (configurable);
            PostConnectInit ();
        }

        public static async Task PostConnectInit() {
            await Utility.AwaitFullBoot ();
            AddMissingChannels (Utility.GetServer ());
        }

        public static async Task OnUserUpdated(SocketGuild guild, SocketVoiceChannel before, SocketVoiceChannel after) {
            // Maybe, just maybe put these into a single function. Oh shit I just did.
            if (Program.FullyBooted ()) {
                try {
                    if (before != null || after != null) { // Only do something if the updated user is in a voice channel.
                        if (autoAddChannels) {
                            AddMissingChannels (guild);
                            await CheckFullAndAddIf (guild);
                            RemoveLeftoverChannels (guild);
                        }

                        await UpdateVoiceChannel (before);
                    await UpdateVoiceChannel (after);
                    }
                } catch (Exception e) {
                    Logging.Log (Logging.LogType.EXCEPTION, e.Message + " - " + e.StackTrace);
                }
            }
        }

        private static void AddDefaultChannel(ulong id, string name, int index) {
            VoiceChannel newChannel = new VoiceChannel (id, name, index);

            defaultChannels.Add (id, newChannel);
            allVoiceChannels.Add (id, newChannel);
        }
        
        private static void AddDefaultChannel(VoiceChannel channel) {
            defaultChannels.Add (channel.id, channel);
            allVoiceChannels.Add (channel.id, channel);
        }

        public static async Task UpdateVoiceChannel(SocketVoiceChannel voice) {
            Game highestGame = new Game ("", "", StreamType.NotStreaming);

            if (voice != null && allVoiceChannels.ContainsKey (voice.Id)) {

                VoiceChannel voiceChannel = allVoiceChannels [ voice.Id ];
                List<SocketGuildUser> users = Utility.ForceGetUsers (voice.Id);

                if (voiceChannel.ignore)
                    return;

                if (voice.Users.Count () == 0) {
                    voiceChannel.Reset ();
                } else {
                    if (voiceChannel.desiredMembers > 0) {
                        if (users.Count >= voiceChannel.desiredMembers)
                            voiceChannel.SetStatus (VoiceChannel.VoiceChannelStatus.Full, false);
                        else
                            voiceChannel.SetStatus (VoiceChannel.VoiceChannelStatus.Looking, false);
                    }
                }

                Dictionary<Game, int> numPlayers = new Dictionary<Game, int> ();
                foreach (SocketGuildUser user in users) {

                    if (UserConfiguration.GetSetting<bool> (user.Id, "AutoLooking") && users.Count == 1)
                        voiceChannel.SetStatus (VoiceChannel.VoiceChannelStatus.Looking, false);

                    if (user.Game.HasValue && user.IsBot == false) {
                        if (numPlayers.ContainsKey (user.Game.Value)) {
                            numPlayers [ user.Game.Value ]++;
                        } else {
                            numPlayers.Add (user.Game.Value, 1);
                        }
                    }

                }

                int highest = int.MinValue;

                for (int i = 0; i < numPlayers.Count; i++) {
                    KeyValuePair<Game, int> value = numPlayers.ElementAt (i);

                    if (value.Value > highest) {
                        highest = value.Value;
                        highestGame = value.Key;
                    }
                }

                if (enableVoiceChannelTags)
                    GetTags (voiceChannel);

                string tagsString = "";
                foreach (VoiceChannelTag tag in voiceChannel.currentTags) {
                    tagsString += tag.tagEmoji;
                }
                tagsString += tagsString.Length > 0 ? " " : "";

                string [ ] splitVoice = voiceChannel.name.Split (';');
                string possibleShorten = shortenChannelNames && splitVoice.Length > 1 ? splitVoice [ 1 ] : splitVoice [ 0 ];
                int mixedLimit = highest >= 2 ? 2 : Utility.ForceGetUsers (voice.Id).Count == 1 ? int.MaxValue : 1; // Nested compact if statements? What could go wrong!

                string gameName = numPlayers.Where (x => x.Value >= mixedLimit).Count () > mixedLimit ? "Mixed Games" : gameNameReplacements.ContainsKey (highestGame.Name) ? gameNameReplacements[highestGame.Name] : highestGame.Name; // What even is this

                string newName;
                if (autoRenameChannels) {
                    newName = gameName != "" ? tagsString + possibleShorten + " - " + gameName : tagsString + splitVoice [ 0 ];
                } else {
                    newName = tagsString + splitVoice [ 0 ];
                }

                if (voiceChannel.customName != "")
                    newName = tagsString + possibleShorten + " - " + voiceChannel.customName;

                // Trying to optimize API calls here, just to spare those poor souls at the Discord API HQ stuff
                if (voice.Name != newName) {
                    Logging.Log (Logging.LogType.BOT, "Channel name updated: " + newName);
                    await voice.ModifyAsync ((delegate (VoiceChannelProperties properties) {
                        properties.Name = newName;
                    }));
                }
                voiceChannel.CheckLocker ();
            }
        }

        public static string GetAvailableExtraName(bool autoUse) {
            string found = null;
            if (randomizeNameQueue) {
                List<string> possibilities = nameQueue.Where (x => x.Value == false).ToDictionary (x => x.Key).Keys.ToList ();
                if (possibilities.Count > 0) {
                    Random random = new Random ();
                    found = possibilities [ random.Next (0, possibilities.Count) ];
                }
            } else {
                foreach (var pair in nameQueue) {
                    if (pair.Value == false) {
                        found = pair.Key;
                        break;
                    }
                }
            }
            
            if (found != null)
                nameQueue [ found ] = true;

            return found;
        }

        public static void TestAndRemoveTemporaryChannels() {
            foreach (var pair in allVoiceChannels) {
                VoiceChannel voiceChannel = pair.Value;

                if (voiceChannel.lifeTime.Ticks > 0) {
                    if (voiceChannel.creationTime.Add (voiceChannel.lifeTime) < DateTime.Now && Utility.ForceGetUsers (voiceChannel.id).Count == 0) {
                        defaultChannels.Remove (voiceChannel.id);
                        allVoiceChannels.Remove (voiceChannel.id);
                        voiceChannel.GetChannel ().DeleteAsync ();
                        return;
                    }
                }
            }
        }

        private static bool hasChecked = false;
        public static void AddMissingChannels(SocketGuild server) {
            if (hasChecked)
                return;

            foreach (SocketVoiceChannel channel in server.VoiceChannels) {
                if (!allVoiceChannels.ContainsKey (channel.Id)) {
                    // Attempt to find a matching name from namelist.

                    List<string> toTest = new List<string> ();
                    toTest.Add (channel.Name.Substring (channel.Name.IndexOf (' ') + 1));
                    toTest.Add (channel.Name);

                    string withoutTags = channel.Name;
                    string found = postRebootChannelName;
                    foreach (string x in extraChannelNames) {
                        string [ ] possibilities = x.Split (';');
                        SoftStringComparer comparer = new SoftStringComparer ();

                        foreach (string name in toTest) {
                            if (possibilities.Length > 0 ? comparer.Equals (possibilities [ 1], name) || possibilities[0] == name : possibilities [ 0 ] == name) {
                                found = x;
                                break;
                            }
                        }

                    }

                    nameQueue [ found ] = true;
                    allVoiceChannels.Add (channel.Id, new VoiceChannel (channel.Id, found, allVoiceChannels.Count));
                }

                foreach (VoiceChannelTag tag in voiceChannelTags) {
                    if (channel.Name.Contains (tag.tagEmoji))
                        try {
                            tag.setTrue?.Invoke (allVoiceChannels [ channel.Id ]);
                        } catch (Exception e) {
                            Logging.Log (Logging.LogType.EXCEPTION, e.Message);
                            throw;
                        }
                }
            }

            hasChecked = true;
        }

        public static void RemoveLeftoverChannels(SocketGuild server) {
            List<SocketVoiceChannel> toDelete = new List<SocketVoiceChannel> ();

            foreach (SocketVoiceChannel channel in server.VoiceChannels) {
                if (!defaultChannels.ContainsKey (channel.Id) && channel.Users.Count () == 0) {
                    toDelete.Add (channel);
                }
            }

            for (int i = IsDefaultFull () ? 1 : 0; i < toDelete.Count; i++) {
                SocketVoiceChannel channel = toDelete [ i ];
                string channelName = allVoiceChannels [ channel.Id ].name;
                if (channelName != postRebootChannelName && nameQueue.ContainsKey (channelName)) {
                    nameQueue [ channelName ] = false;
                }

                temporaryChannels.Remove (channel);
                allVoiceChannels.Remove (channel.Id);
                channel.DeleteAsync ();
            }

            ResetVoiceChannelPositions (server);
        }

        private static void ResetVoiceChannelPositions(SocketGuild e) {
            IEnumerable<VoiceChannel> channels = allVoiceChannels.Values.ToList ();

            foreach (VoiceChannel channel in channels) {
                if (channel.ignore)
                    continue;

                int sunkenPos = channel.position;
                while (!ChannelAtPosition (channels, sunkenPos - 1)) {
                    sunkenPos--;
                }
                channel.position = sunkenPos;
            }
        }

        public static bool ChannelAtPosition(IEnumerable<VoiceChannel> channels, int position) {
            if (position < 0)
                return true;

            bool result = false;
            foreach (VoiceChannel channel in channels) {
                if (channel.position == position)
                    result = true;
            }

            return result;
        }

        public static bool IsDefaultFull() {
            IEnumerable<VoiceChannel> defaultChannelsList = defaultChannels.Values.ToList ();
            foreach (VoiceChannel channel in defaultChannelsList) {
                if (!channel.ignore && Utility.ForceGetUsers (channel.id).Count () == 0)
                    return false;
            }

            return true;
        }

        public static async Task CheckFullAndAddIf(SocketGuild server) {
            List<VoiceChannel> channels = allVoiceChannels.Values.ToList ();
            int count = channels.Where(x => !x.ignore).Count ();

            fullChannels = 0;
            foreach (VoiceChannel cur in channels) {
                if (cur.ignore)
                    continue;

                if (cur.GetChannel () != null) {
                    SocketVoiceChannel channel = cur.GetChannel ();
                    if (Utility.ForceGetUsers (channel.Id).Count != 0) {
                        fullChannels++;
                    }
                }
            }

            // If the amount of full channels are more than or equal to the amount of channels, add a new one.
            if (fullChannels == count) {
                if (nameQueue.Count > 0 && awaitingChannels.Count == 0) {
                    string channelName = GetAvailableExtraName (true);

                    RestVoiceChannel channel;
                    try {
                        Logging.Log (Logging.LogType.BOT, "Creating new voice channel: " + channelName);
                        Task<RestVoiceChannel> createTask = server.CreateVoiceChannelAsync (channelName.Split(';')[0]);
                        channel = await createTask;
                    } catch (Exception e) {
                        throw e;
                    }

                    int channelPos = temporaryChannels.Count + addChannelsIndex;

                    temporaryChannels.Add (channel);
                    allVoiceChannels.Add (channel.Id, new VoiceChannel (channel.Id, channelName, channelPos));
                    awaitingChannels.Remove (channelName);

                    IEnumerable<VoiceChannel> allChannels = allVoiceChannels.Values.ToList ();
                    foreach (VoiceChannel loc in allChannels) {
                        if (loc.position >= channelPos && loc.GetChannel () as IVoiceChannel != channel)
                            loc.position++;
                    }
                }
            }
        }

        public static SocketVoiceChannel GetEmptyChannel(SocketGuild server) {
            foreach (SocketVoiceChannel channel in server.VoiceChannels) {
                if (channel.Users.Count () == 0)
                    return channel;
            }

            return null;
        }

        public static void GetTags(VoiceChannel channel) {
            List<VoiceChannelTag> tags = voiceChannelTags.ToList ();

            for (int i = 0; i < tags.Count; i++) {
                VoiceChannelTag curTag = tags [ i ];

                VoiceChannelTag.ActionData data = new VoiceChannelTag.ActionData (channel);
                try {
                    if (curTag.enabled)
                        curTag.run (data);
                }catch (Exception e) {
                    Logging.DebugLog (Logging.LogType.EXCEPTION, e.StackTrace);
                }

                if (data.hasTag) {
                    if (!channel.currentTags.Contains (curTag))
                        channel.currentTags.Add (curTag);
                } else {
                    if (channel.currentTags.Contains (curTag))
                        channel.currentTags.Remove (curTag);
                }
            }
        }

        public static async Task<VoiceChannel> CreateTemporaryChannel(string channelName, TimeSpan lifeTime) {
            RestVoiceChannel channel = await Utility.GetServer ().CreateVoiceChannelAsync (channelName);
            VoiceChannel newVoice = new VoiceChannel (channel.Id, channelName, allVoiceChannels.Count);
            newVoice.lifeTime = lifeTime;
            AddTemporaryChannel (channel.Id, newVoice);
            return allVoiceChannels [ channel.Id ];
        }

        public static void AddTemporaryChannel(ulong id, VoiceChannel newVoice) {
            defaultChannels.Add (id, newVoice);
            allVoiceChannels.Add (id, newVoice);
        }

        public static void RemoveTemporaryChannel(ulong id) {
            defaultChannels.Remove (id);
            allVoiceChannels.Remove (id);
        }

        public class VoiceChannelTag {
            public string tagEmoji = "🍞";
            [JsonIgnore] public Action<ActionData> run;
            [JsonIgnore] public Action<VoiceChannel> setTrue;
            [JsonIgnore] public string name = "";
            public bool enabled = false;

            public VoiceChannelTag(string _name, string emoji, Action<ActionData> command, Action<VoiceChannel> _setTrue = null) {
                name = _name;
                tagEmoji = emoji;
                run = command;
                setTrue = _setTrue;
            }

            public class ActionData {
                public bool hasTag = false;
                public VoiceChannel channel;

                public ActionData (VoiceChannel c) {
                    channel = c;
                }
            }
        }

        public class VoiceChannel {

            public enum VoiceChannelStatus {
                None, Looking, Full
            }

            public ulong id;
            public string name;
            public int position;
            public bool lockable = true;
            public bool ignore;
            [JsonIgnore] public VoiceChannelStatus status = VoiceChannelStatus.None;
            [JsonIgnore] public uint desiredMembers = 0;
            [JsonIgnore] public string customName = "";

            [JsonIgnore] public List<VoiceChannelTag> currentTags = new List<VoiceChannelTag> ();

            [JsonIgnore] public ulong lockerID;
            [JsonIgnore] public List<ulong> allowedUsers = new List<ulong>();

            // Stuff used only for tempoary channels.
            [JsonIgnore] public DateTime creationTime = DateTime.Now;
            [JsonIgnore] public TimeSpan lifeTime = new TimeSpan (0);

            public VoiceChannel (ulong _id, string n, int pos) {
                id = _id;
                name = n;
                position = pos;
                //channel = ch;
            }

            public SocketVoiceChannel GetChannel () {
                SocketChannel channel = Utility.GetServer ().GetChannel (id);
                return channel as SocketVoiceChannel;
            }

            public string GetName() {
                return name.Split (';') [ 0 ];
            }

            public bool IsLocked () {
                return allowedUsers.Count != 0;
            }

            public void Lock (ulong lockingUser, bool update) {
                if (lockable) {
                    lockerID = lockingUser;

                    List<ulong> alreadyIn = new List<ulong> ();
                    foreach (SocketGuildUser user in GetChannel ().Users) {
                        alreadyIn.Add (user.Id);
                    }

                    allowedUsers = alreadyIn;
                    if (update)
                        UpdateVoiceChannel (GetChannel ());
                }
            }

            public void Unlock (bool update) {
                allowedUsers = new List<ulong> ();
                lockerID = 0;
                if (update)
                    UpdateVoiceChannel (GetChannel ());
            }

            public void Reset() {
                Unlock (false);
                status = VoiceChannelStatus.None;
                desiredMembers = 0;
                SetCustomName ("", false);
            }

            public bool InviteUser (SocketGuildUser sender, SocketGuildUser user ) {
                if (!allowedUsers.Contains (user.Id) && IsLocked ()) {
                    allowedUsers.Add (user.Id);
                    Program.messageControl.SendMessage (user, "**" + Utility.GetUserName (sender) + "** has invited you to join the locked channel **" + GetName () + "** on **" + Program.serverName + "**.");
                    return true;
                }
                return false;
            }

            public void RequestInvite ( SocketGuildUser requester ) {
                if (IsLocked ()) {
                    Program.messageControl.AskQuestion (GetLocker ().Id, "**" + Utility.GetUserName (requester) + "** on **" + Program.serverName + "** requests access to your locked voice channel.",
                        delegate () {
                            allowedUsers.Add (requester.Id);
                            Program.messageControl.SendMessage (requester, "Your request to join **" + GetName () + "** has been accepted.");
                            Program.messageControl.SendMessage (GetLocker (), "Succesfully accepted request.");
                        } );
                }
            }

            public void OnUserJoined(SocketGuildUser user) {
                if (IsLocked ()) {
                    if (!allowedUsers.Contains (user.Id)) {
                        user.ModifyAsync (delegate (GuildUserProperties properties) {
                            properties.Channel = afkChannel.GetChannel ();
                        });
                        Program.messageControl.SendMessage (user, "You've automatically been moved to the AFK channel, since channel **" + GetName () + "** is locked by " + Utility.GetUserName (GetLocker ()));
                    }
                }
            }

            public void UpdateLockPermissions() {
                if (IsLocked ()) {
                    GetChannel ().ModifyAsync (delegate (VoiceChannelProperties properties) {
                        properties.UserLimit = allowedUsers.Count;
                    });
                } else {
                    GetChannel ().ModifyAsync (delegate (VoiceChannelProperties properties) {
                        properties.UserLimit = 0;
                    });
                }
            }

            public void Kick ( SocketGuildUser user ) {
                allowedUsers.Remove (user.Id);
                OnUserJoined (user);
            }

            public SocketGuildUser GetLocker () {
                return Utility.GetServer ().GetUser (lockerID);
            }

            public void ToggleStatus (VoiceChannelStatus stat) {
                if (desiredMembers > 0) {
                    SetDesiredMembers (0);
                    SetStatus (stat, true);
                    return;
                }

                status = status == stat ? VoiceChannelStatus.None : stat;
                UpdateVoiceChannel (GetChannel ());
            }

            public void SetStatus(VoiceChannelStatus stat, bool update) {
                status = stat;
                if (update)
                    UpdateVoiceChannel (GetChannel ());
            }

            public void SetDesiredMembers(uint number) {
                desiredMembers = number;
                UpdateVoiceChannel (GetChannel ());
            }

            public void SetCustomName(string n, bool update) {
                customName = n;
                if (update)
                    UpdateVoiceChannel (GetChannel ());
            }

            public void CheckLocker () {
                bool containsLocker = false;
                foreach (SocketGuildUser user in GetChannel ().Users) {
                    if (user.Id == lockerID) {
                        containsLocker = true;
                        break;
                    }
                }

                if (IsLocked () && !containsLocker) {
                    if (lockerID == 0) {
                        Program.messageControl.SendMessage (GetLocker (), "Due to this bot rebooting, and therefore resetting locking data, you have been given locking authority over voice channel **" + GetName () + "**.");
                    } else {
                        Program.messageControl.SendMessage (GetLocker (), "Since the previous locker left, or the , you are now the new locker of voice channel **" + GetName () + "**.");
                    }
                    lockerID = GetChannel ().Users.ElementAt (0).Id;
                }
            }
        }
    }
}
