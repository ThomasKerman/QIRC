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

namespace QIRC.Say
{
    /// <summary>
    /// This is the implementation for the say command. The bot will grab the words
    /// behind the control sequence and output it. Pretty basic
    /// </summary>
    public class Say : IrcCommand
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
            return "say";
        }

        /// <summary>
        /// Returns a description of the command
        /// </summary>
        public override String GetDescription()
        {
            return "Outputs the given text to the given channel.";
        }

        /// <summary>
        /// The Parameters of the Command
        /// </summary>
        public override String[] GetParameters()
        {
            return new String[]
            {
                "to", "The Channel where the bot should send the message to."
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
            return Settings.Read<String>("control") + GetName() + " -to:#botwar Hi, I'm the new one.";
        }

        /// <summary>
        /// Here we run the command and evaluate the parameters
        /// </summary>
        public override void RunCommand(IrcClient client, ProtoIrcMessage message)
        {
            if (StartsWithParam("to", message.Message))
            {
                String text = message.Message;
                String target = StripParam("to", ref text);
                BotController.SendMessage(client, text, target, target, true);
            }
            else
            {
                BotController.SendMessage(client, message.Message, message.User, message.Source, true);
            }
        }
    }
}
