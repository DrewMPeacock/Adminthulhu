﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Discord;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.IO;

namespace Adminthulhu {
    public class AutoPatcher : IClockable, IConfigurable {

        public static string url = "https://raw.githubusercontent.com/Lomztein/Adminthulhu/master/Compiled/";

        public bool doAutoPatch = false;
        public ulong announcePatchAvailabilityChannelID = 0;
        public ulong askToPatchChannelID = 0;

        public Task Initialize(DateTime time) {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
            return Task.CompletedTask;
        }

        public Task OnDayPassed(DateTime time) {
            CheckForPatch ();
            return Task.CompletedTask;
        }

        public Task OnHourPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed(DateTime time) {
            return Task.CompletedTask;
        }

        public async void CheckForPatch() {
            string basePath = AppContext.BaseDirectory + "/";
            if (!Directory.Exists (basePath + "/patcher/")) {
                Logging.Log (Logging.LogType.WARNING, "Patcher application not located dispite autopatching being activated.");
                return;
            }

            using (HttpClient client = new HttpClient ()) {

                string localVersion = "";
                try {
                    localVersion = SerializationIO.LoadTextFile (basePath + "version.txt") [ 0 ];
                } catch { }

                string version = await client.GetStringAsync (url + "version.txt");

                if (localVersion != version) {
                    // A new patch is available.
                    try {
                        string changelog = await client.GetStringAsync (url + "changelog.txt");
                        SocketTextChannel channel = Utility.GetServer ().GetChannel (announcePatchAvailabilityChannelID) as SocketTextChannel;
                        if (channel != null)
                            Program.messageControl.SendMessage (channel as ISocketMessageChannel,"Adminthulhu Bot Version " + version + "\n```" + changelog + "```", true);

                        if (doAutoPatch) {
                            Patch ();
                        } else {
                            try {
                                SocketTextChannel askChannel = Utility.GetServer ().GetChannel (askToPatchChannelID) as SocketTextChannel;
                                Program.messageControl.AskQuestion (askChannel.Id, "A new patch for me has become available, should I install?", delegate () {
                                    Patch ();
                                });
                            } catch (Exception e) {
                                Logging.Log (Logging.LogType.EXCEPTION, e.Message + " - " + e.StackTrace);
                            }
                        }
                    } catch (Exception e) {
                        Logging.Log (Logging.LogType.EXCEPTION, e + " - " + e.StackTrace);
                    }
                }
            }
        }

        public static void Patch() {
            try {
                string baseDirectory = AppContext.BaseDirectory;
                string command = "dotnet " + baseDirectory + "/patcher/AdminthulhuPatcher.dll " + url + " " + baseDirectory + "/";
                string executable = RuntimeInformation.IsOSPlatform (OSPlatform.Windows) ? "CMD.exe" : "/bin/bash";

                Process patcher = new Process ();
                patcher.StartInfo.FileName = executable;
                patcher.StartInfo.CreateNoWindow = false;
                patcher.StartInfo.RedirectStandardInput = true;

                if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
                    patcher.Start ();
                    patcher.StandardInput.WriteLine (command);
                } else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
                    patcher.StartInfo.Arguments = $"-c \"{command}\""; // Oh I'm gonna be using that dollersign more.
                    patcher.Start ();
                }
                // No MacOS support currently, because who in their right mind would run a bot on a Mac? TODO: Add MacOS support somehow.
                // Possibly pack this into a library for future use.

                Environment.Exit (0);
            } catch (Exception e) {
                Logging.Log (Logging.LogType.EXCEPTION, e + " - " + e.StackTrace);
                throw;
            }
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public void LoadConfiguration() {
            doAutoPatch = BotConfiguration.GetSetting ("Patcher.DoAutoPatch", "", doAutoPatch);
            announcePatchAvailabilityChannelID = BotConfiguration.GetSetting ("Patcher.AnnouncePatchAvailabilityChannelID", "", announcePatchAvailabilityChannelID);
            askToPatchChannelID = BotConfiguration.GetSetting ("Patcher.AskToPatchChannelID", "", askToPatchChannelID);
        }
    }

    public class CCheckPatch : Command {
        public CCheckPatch() {
            command = "patch";
            shortHelp = "Try to patch bot.";
            longHelp = "Check for a new patch, and install if available.";
            argumentNumber = 0;
            isAdminOnly = true;
            catagory = Catagory.Admin;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                AutoPatcher.Patch ();
            }
            return Task.CompletedTask;
        }
    }
}
