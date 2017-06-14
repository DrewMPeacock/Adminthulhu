﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;

namespace Adminthulhu {
    public class Clock {

        public static Thread timeThread;
        public DateTime lastMesauredTime;
        public int checkDelay = 1000; // The time between each check in milliseconds.

        public ulong [ ] chosenOnes = {
            94089489227448320,
            93744415301971968,
            93732620998819840,
            169870846507220992,
            174835518901583872
        };

        public SocketGuildUser [ ] lastTest = new SocketGuildUser [ 5 ];

        public IClockable [ ] clockables = new IClockable [ ] {
            new AutomatedEventHandling (), new AutomatedTextChannels (), new UserActivityMonitor (), new Birthdays (), new AutomatedWeeklyEvent (),
            new Strikes (), new AprilFools (), new Younglings (),
        };

        public Clock () {
            timeThread = new Thread (new ThreadStart (Initialize));
            timeThread.Start ();
            while (!timeThread.IsAlive) {
                ChatLogger.Log ("Initializing clock thread..");
            }
            Thread.Sleep (1);
        }

        public void Initialize () {
            DateTime now = DateTime.Now;
            foreach (IClockable c in clockables) {
                c.Initialize (now);
            }

            while (timeThread.IsAlive) {
                now = DateTime.Now;

                // This could possibly be improved with delegates, but I have no idea how they work.
                // Don't do anything before the server is ready.
                if (Utility.GetServer () != null) {

                    if (now.Second != lastMesauredTime.Second) {
                        foreach (IClockable c in clockables)
                            c.OnSecondPassed (now);
                    }

                    if (now.Minute != lastMesauredTime.Minute) {
                        foreach (IClockable c in clockables)
                            c.OnMinutePassed (now);
                    }

                    if (now.Hour != lastMesauredTime.Hour) {
                        foreach (IClockable c in clockables)
                            c.OnHourPassed (now);
                    }

                    if (now.Day != lastMesauredTime.Day) {
                        foreach (IClockable c in clockables)
                            c.OnDayPassed (now);
                    }
                }


                Thread.Sleep (checkDelay);
                lastMesauredTime = now;
            }
        }

        public static TimeSpan GetTimespan(string input) {
            int count;
            if (int.TryParse (input.Substring (0, input.Length - 1), out count)) {
                switch (input [ input.Length - 1 ]) {
                    case 'm':
                        return new TimeSpan (0, count, 0);

                    case 'h':
                        return new TimeSpan (count, 0, 0);

                    case 'd':
                        return new TimeSpan (count, 0, 0, 0);

                    case 'w':
                        return new TimeSpan (count * 7, 0, 0, 0, 0);
                }
            }
            throw new ArgumentException ("Input didn't work, dummy.");
        }
    }
}
