﻿/** 
 * .NET Bot for Internet Relay Chat (IRC)
 * Copyright (c) Dorian Stoll 2017
 * QIRC is licensed under the MIT License
 */

using System;
using ChatSharp;
using QIRC.Configuration;
using QIRC.IRC;
using QIRC.Plugins;

namespace QIRC.Roll
{
    /// <summary>
    /// This is the implementation for the roll command. The bot will read the given numbers
    /// and output randomly generated numbers based on that.
    /// </summary>
    public class Roll : IrcCommand
    {
        /// <summary>
        /// The Access Level that is needed to execute the command
        /// </summary>
        public override AccessLevel GetAccessLevel()
        {
            return AccessLevel.NORMAL;
        }

        /// <summary>
        /// The name of the command
        /// </summary>
        public override String GetName()
        {
            return "roll";
        }

        /// <summary>
        /// Returns a description of the command
        /// </summary>
        public override String GetDescription()
        {
            return "Generates random numbers.";
        }

        /// <summary>
        /// The Parameters of the Command
        /// </summary>
        public override String[] GetParameters()
        {
            return new String[]
            {
                "seed", "The seed that should be used for the random number generator."
            };
        }

        /// <summary>
        /// Whether the command can be used in serious channels.
        /// </summary>
        public override Boolean IsSerious()
        {
            return true;
        }

        /// <summary>
        /// An example for using the command.
        /// </summary>
        /// <returns></returns>
        public override String GetExample()
        {
            return Settings.Read<String>("control") + GetName() + " -seed:42 3d255";
        }

        /// <summary>
        /// Here we run the command and evaluate the parameters
        /// </summary>
        public override void RunCommand(IrcClient client, ProtoIrcMessage message)
        {
            if (StartsWithParam("seed", message.Message))
            {
                String text = message.Message;
                String seedS = StripParam("seed", ref text);
                Int32 seed = Int32.Parse(seedS);
                Random random = new Random(seed);
                Int32[] numbers = new Int32[] { 1, 6 };
                String[] splits = text.Split(new[] { 'd' }, 2);
                numbers[0] = String.IsNullOrWhiteSpace(splits[0]) ? numbers[0] : Math.Min(300, Math.Max(1, Int32.Parse(splits[0])));
                if (splits.Length == 2)
                    numbers[1] = String.IsNullOrWhiteSpace(splits[1]) ? numbers[1] : Math.Min(300, Math.Max(1, Int32.Parse(splits[1])));
                Int32[] results = new Int32[numbers[0]];
                for (Int32 i = 0; i < results.Length; i++)
                    results[i] = random.Next(1, numbers[1]);
                BotController.SendMessage(client, String.Join(", ", results), message.User, message.Source);
            }
            else
            {
                Random random = new Random();
                Int32[] numbers = new Int32[] { 1, 6 };
                String[] splits = message.Message.Split(new[] { 'd' }, 2);
                numbers[0] = String.IsNullOrWhiteSpace(splits[0]) ? numbers[0] : Math.Min(300, Math.Max(1, Int32.Parse(splits[0])));
                if (splits.Length == 2)
                    numbers[1] = String.IsNullOrWhiteSpace(splits[1]) ? numbers[1] : Math.Min(300, Math.Max(1, Int32.Parse(splits[1])));
                Int32[] results = new Int32[numbers[0]];
                for (Int32 i = 0; i < results.Length; i++)
                    results[i] = random.Next(1, numbers[1]);
                BotController.SendMessage(client, String.Join(", ", results), message.User, message.Source);
            }
        }
    }
}
