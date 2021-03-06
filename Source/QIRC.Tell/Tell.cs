﻿/** 
 * .NET Bot for Internet Relay Chat (IRC)
 * Copyright (c) Dorian Stoll 2017
 * QIRC is licensed under the MIT License
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ChatSharp;
using ChatSharp.Events;
using QIRC.Configuration;
using QIRC.IRC;
using QIRC.Plugins;

namespace QIRC.Tell
{
    /// <summary>
    /// This is the implementation for the tell command. The bot will store messages for users and deliver
    /// them when the given user is online.
    /// </summary>
    public class Tell : IrcCommand
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
            return "tell";
        }

        /// <summary>
        /// Returns a description of the command
        /// </summary>
        public override String GetDescription()
        {
            return "Stores messages and delivers them when the specified user is online.";
        }

        /// <summary>
        /// The Parameters of the Command
        /// </summary>
        public override String[] GetParameters()
        {
            return new String[]
            {
                "channel", "The channel where the message should be delivered to.",
                "private", "Whether the message should get delivered privately.",
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
            return Settings.Read<String>("control") + GetName() + " Thomas I like your bot";
        }

        /// <summary>
        /// Here we run the command and evaluate the parameters
        /// </summary>
        public override void RunCommand(IrcClient client, ProtoIrcMessage message)
        {
            if (StartsWithParam("channel", message.Message))
            {
                String text = message.Message;
                String target = StripParam("channel", ref text);
                String[] split = text.Trim().Split(new Char[] { ' ' }, 2);
                foreach (String name in split[0].Split(','))
                {
                    String wildcard = "^" + Regex.Escape(name.Trim()).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                    TellData.Query.Connection.Insert(new TellData
                    {
                        channel = true,
                        channelName = target,
                        message = split[1],
                        pm = false,
                        source = message.IsChannelMessage && !BotController.GetChannel(message.Source).secret ? message.Source : "Private",
                        time = DateTime.UtcNow,
                        to = wildcard,
                        user = message.User
                    });
                }
            }
            else
            {
                String text = message.Message;
                Boolean pm = StartsWithParam("private", text);
                StripParam("private", ref text);
                String[] split = text.Trim().Split(new Char[] { ' ' }, 2);
                foreach (String name in split[0].Split(','))
                {
                    String wildcard = "^" + Regex.Escape(name.Trim()).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                    TellData.Query.Connection.Insert(new TellData
                    {
                        channel = false,
                        channelName = "",
                        message = split[1],
                        pm = pm,
                        source = message.IsChannelMessage && !BotController.GetChannel(message.Source).secret ? message.Source : "Private",
                        time = DateTime.UtcNow,
                        to = wildcard,
                        user = message.User
                    });
                }
            }
            BotController.SendMessage(client, "I'll redirect this as soon as they are around.", message.User, message.Source);
        }
    
        /// <summary>
        /// Deliver the messages from the tell command
        /// </summary>
        public override void OnPrivateMessageRecieved(IrcClient client, PrivateMessageEventArgs e)
        {
            List<TellData> toDelete = new List<TellData>();
            foreach (TellData tell in TellData.Query.ToList())
            {
                if (!Regex.IsMatch(e.PrivateMessage.User.Nick, tell.to, RegexOptions.IgnoreCase))
                    continue;
                if (tell.channel && tell.channelName != e.PrivateMessage.Source)
                    continue;
                String message = "[b]" + tell.user + "[/b] left a message for you in [b]" + tell.source + " [" + tell.time.ToString("dd.MM.yyyy HH:mm:ss") + "][/b]: \"" + tell.message + "\"";
                if (tell.pm)
                    BotController.SendMessage(client, message, e.PrivateMessage.User.Nick, e.PrivateMessage.User.Nick, true);
                else
                    BotController.SendMessage(client, message, e.PrivateMessage.User.Nick, e.PrivateMessage.Source);
                toDelete.Add(tell);
            }
            toDelete.ForEach(t => TellData.Query.Delete(t2 => t2.Index == t.Index));
        }
    }
}
