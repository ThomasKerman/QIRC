﻿/** 
 * .NET Bot for Internet Relay Chat (IRC)
 * Copyright (c) Dorian Stoll 2017
 * QIRC is licensed under the MIT License
 */

using ChatSharp;
using ChatSharp.Events;
using QIRC.Configuration;
using QIRC.IRC;
using QIRC.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using log4net.Config;
using QIRC.Constants;
using SQLite;

namespace QIRC
{
    /// <summary>
    /// This is the main Bot Controller. It loads the Bot and creates the <see cref="IrcClient"/>.
    /// After this is done, it is responsible for Comandline Access. 
    /// </summary>
    public class BotController
    {
        /// <summary>
        /// Logging
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The connection to the IRC Server. It handles the protocol
        /// implementation for us. Also, it gives us delegates for events.
        /// </summary>
        public static IrcClient client { get; set; }

        /// <summary>
        /// Whether there's an active IRC connection at the moment.
        /// </summary>
        public static Boolean isConnected { get; set; }

        /// <summary>
        /// Whether the Bot is still alive.
        /// </summary>
        public static Boolean isAlive { get; set; }

        /// <summary>
        /// The thread that manages the IRC Connection
        /// </summary>
        public static Thread ircThread { get; protected set; }

        /// <summary>
        /// The persistent data storage for the bot.
        /// </summary>
        public static SQLiteConnection Database { get; set; }

        /// <summary>
        /// This function is executed when the Program starts.
        /// Here we load everything we need and create the <see cref="IrcClient"/>
        /// </summary>
        /// <param name="args">Commandline arguments</param>
        public static void Main(String[] args)
        {
            // Init log4net
            XmlConfigurator.Configure();

            // We are alive
            isAlive = true;

            // Load the database
            Database = new SQLiteConnection(Paths.data);

            // Load the settings of the Bot
            PluginManager.Load();
            PluginManager.Invoke("Load");
            Settings.Load();

            // Call OnAwake
            PluginManager.Invoke("Awake");

            // Connect to the IRC
            ircThread = new Thread(Connect);
            ircThread.Start();

            // Command line handler goes here
            while (isAlive)
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Creates the connection to the server and saves it into <see cref="client"/>
        /// It also sets various flags that are needed for the bot to function properly.
        /// </summary>
        public static void Connect()
        {
            // Identification
            IrcUser user = new IrcUser(Settings.Read<String>("name"), Settings.Read<String>("name"), Settings.Read<String>("password"), "QIRC - A friendly IRC bot!");

            // Grab settings
            String host = Settings.Read<String>("host");
            Int32 port = Settings.Read<Int32>("port");
            Boolean useSSL = Settings.Read<Boolean>("useSSL");

            // Create the IrcClient
            client = new IrcClient(String.Join(":", host, port), user, useSSL);
            client.Encoding = Encoding.UTF8;
            client.Settings.ModeOnJoin = true;

            // Add delegates
            client.ChannelListRecieved += ChannelListRecieved;
            client.ChannelTopicReceived += ChannelTopicReceived;
            client.ConnectionComplete += ConnectionComplete;
            client.ModeChanged += ModeChanged;
            client.MOTDPartRecieved += MOTDPartRecieved;
            client.MOTDRecieved += MOTDRecieved;
            client.NetworkError += NetworkError;
            client.NickChanged += NickChanged;
            client.NickInUse += NickInUse;
            client.NoticeRecieved += NoticeRecieved;
            client.PrivateMessageRecieved += PrivateMessageRecieved;
            client.RawMessageRecieved += RawMessageRecieved;
            client.RawMessageSent += RawMessageSent;
            client.ServerInfoRecieved += ServerInfoRecieved;
            client.UserJoinedChannel += UserJoinedChannel;
            client.UserKicked += UserKicked;
            client.UserPartedChannel += UserPartedChannel;
            client.UserQuit += UserQuit;
            client.WhoIsReceived += WhoIsReceived;

            // Connect to IRC
            client.ConnectAsync();
            PluginManager.Invoke("Connect", host, port, client.User.Nick, useSSL);

            // Set isConnected to false, which is funny since the function is named Connect, but hey!
            isConnected = false;
        }

        /// <summary>
        /// Channel List Event
        /// </summary>
        private static void ChannelListRecieved(Object sender, ChannelEventArgs e)
        {
            PluginManager.Invoke("ChannelListRecieved", client, e);
        }

        /// <summary>
        /// Channel Topic Event
        /// </summary>
        private static void ChannelTopicReceived(Object sender, ChannelTopicEventArgs e)
        {
            PluginManager.Invoke("ChannelTopicReceived", client, e);
        }

        /// <summary>
        /// Connection Complete Event
        /// </summary>
        private static void ConnectionComplete(Object sender, EventArgs e)
        {
            isConnected = true;
            PluginManager.Invoke("ConnectionComplete", client);

            // Join Channels
            foreach (ProtoIrcChannel channel in Settings.Read<List<ProtoIrcChannel>>("channels"))
                JoinChannel(channel, false);
        }

        /// <summary>
        /// Mode Changed Event
        /// </summary>
        private static void ModeChanged(Object sender, ModeChangeEventArgs e)
        {
            PluginManager.Invoke("ModeChanged", client, e);
        }

        /// <summary>
        /// MOTD Part Recieved Event
        /// </summary>
        private static void MOTDPartRecieved(Object sender, ServerMOTDEventArgs e)
        {
            PluginManager.Invoke("MOTDPartRecieved", client, e);
        }

        /// <summary>
        /// MODT Recieved Event
        /// </summary>
        private static void MOTDRecieved(Object sender, ServerMOTDEventArgs e)
        {
            PluginManager.Invoke("MOTDRecieved", client, e);
        }

        /// <summary>
        /// Network Error Event
        /// </summary>
        private static void NetworkError(Object sender, SocketErrorEventArgs e)
        {
            PluginManager.Invoke("NetworkError", client, e);
            
            // Bye o/
            client.Quit();
            Environment.Exit((Int32)e.SocketError);
        }

        /// <summary>
        /// Nick Changed Event
        /// </summary>
        private static void NickChanged(Object sender, NickChangedEventArgs e)
        {
            PluginManager.Invoke("NickChanged", client, e);
        }

        /// <summary>
        /// Nick in Use Event
        /// </summary>
        private static void NickInUse(Object sender, ErronousNickEventArgs e)
        {
            PluginManager.Invoke("NickInUse", client, e);
        }

        /// <summary>
        /// Notice Recieved Event
        /// </summary>
        private static void NoticeRecieved(Object sender, IrcNoticeEventArgs e)
        {
            PluginManager.Invoke("NoticeRecieved", client, e);
        }

        /// <summary>
        /// Private Message Event
        /// </summary>
        private static void PrivateMessageRecieved(Object sender, PrivateMessageEventArgs e)
        {
            // Commands
            String control = Settings.Read<String>("control");
            ProtoIrcMessage msg = new ProtoIrcMessage(e);
            if (msg.Message.StartsWith(control))
            {
                HandleCommand(msg, client, afterWhoIs: () =>
                {
                    PluginManager.Invoke("PrivateMessageRecieved", client, e);
                    if (e.PrivateMessage.IsChannelMessage)
                        PluginManager.Invoke("ChannelMessageRecieved", client, e);
                    else
                        PluginManager.Invoke("UserMessageRecieved", client, e);
                });
            }
            else
            {
                PluginManager.Invoke("PrivateMessageRecieved", client, e);
                if (e.PrivateMessage.IsChannelMessage)
                    PluginManager.Invoke("ChannelMessageRecieved", client, e);
                else
                    PluginManager.Invoke("UserMessageRecieved", client, e);
            }
        }

        /// <summary>
        /// Raw Message Recieved Event
        /// </summary>
        private static void RawMessageRecieved(Object sender, RawMessageEventArgs e)
        {
            PluginManager.Invoke("RawMessageRecieved", client, e);
        }

        /// <summary>
        /// Raw Message Sent Event
        /// </summary>
        private static void RawMessageSent(Object sender, RawMessageEventArgs e)
        {
            PluginManager.Invoke("RawMessageSent", client, e);
        }

        /// <summary>
        /// Server Info Recieved Event
        /// </summary>
        private static void ServerInfoRecieved(Object sender, SupportsEventArgs e)
        {
            PluginManager.Invoke("ServerInfoRecieved", client, e);
        }

        /// <summary>
        /// User Joined Channel Event
        /// </summary>
        private static void UserJoinedChannel(Object sender, ChannelUserEventArgs e)
        {
            PluginManager.Invoke("UserJoinedChannel", client, e);
        }

        /// <summary>
        /// User Kicked Event
        /// </summary>
        private static void UserKicked(Object sender, KickEventArgs e)
        {
            if (e.Kicked.Nick == Settings.Read<String>("name") && e.Kicker.Nick != "*status") // ZNC madness
                LeaveChannel(e.Channel.Name);
            PluginManager.Invoke("UserKicked", client, e);
        }

        /// <summary>
        /// User Parted Channel Event
        /// </summary>
        private static void UserPartedChannel(Object sender, ChannelUserEventArgs e)
        {
            PluginManager.Invoke("UserPartedChannel", client, e);
        }

        /// <summary>
        /// User Quit Event
        /// </summary>
        private static void UserQuit(Object sender, UserEventArgs e)
        {
            PluginManager.Invoke("UserQuit", client, e);
        }

        /// <summary>
        /// WhoIs Recieved Event
        /// </summary>
        private static void WhoIsReceived(Object sender, WhoIsReceivedEventArgs e)
        {
            PluginManager.Invoke("WhoIsReceived", client, e);
        }

        /// <summary>
        /// Joins a channel on the IRC
        /// </summary>
        public static void JoinChannel(ProtoIrcChannel channel, Boolean addToCFG = true)
        {
            // If we aren't connected, we cant join. Same if the channel is null
            if (!isConnected || channel == null)
                return;

            // We don't need empty junk
            if (String.IsNullOrWhiteSpace(channel.name))
                return;

            // Join
            String name = channel.name;
            String password = channel.password;
            if (String.IsNullOrWhiteSpace(password))
                client.JoinChannel(name);
            else
                client.JoinChannel(name, password);

            // Add it to the cfg
            if (addToCFG)
            {
                List<ProtoIrcChannel> list = Settings.Read<List<ProtoIrcChannel>>("channels");
                list.Add(channel);
                Settings.Write("channels", list);
            }
        }

        /// <summary>
        /// Leaves a channel on the IRC
        /// </summary>
        public static void LeaveChannel(String channel, String reason = "")
        {
            // If we aren't connected, we cant join.
            if (!isConnected)
                return;

            // We don't need empty junk
            if (String.IsNullOrWhiteSpace(channel))
                return;

            // Leave
            try
            {
                if (String.IsNullOrWhiteSpace(reason))
                    client.PartChannel(channel);
                else
                    client.PartChannel(channel, reason);
            }
            catch
            {

            }

            // Edit the cfg
            List<ProtoIrcChannel> list = Settings.Read<List<ProtoIrcChannel>>("channels");
            list.RemoveAll(c => String.Equals(c.name, channel, StringComparison.InvariantCultureIgnoreCase));
            Settings.Write("channels", list);
        }

        /// <summary>
        /// Handles an incoming command
        /// </summary>
        public static void HandleCommand(ProtoIrcMessage message, IrcClient client, Boolean commandLine = false, Action afterWhoIs = null)
        {
            String control = Settings.Read<String>("control");
            message.Message = message.Message.Remove(0, control.Length);
            IrcUser user = client.Users[message.User];

            // We need to figure out of we know this user. Whois him.
            client.WhoIs(user.Nick, whoIs =>
            {
                try
                {
                    // Go through all the commands
                    foreach (IrcCommand command in PluginManager.commands)
                    {
                        // Get the name of the supplied command and continue if it doesn't matches
                        String cmd = message.Message.Split(' ')[0];
                        if (command.IsNamed(cmd))
                        {
                            message.Message = message.Message.Remove(0, cmd.Length).Trim();
                            AccessLevel level = AccessLevel.NORMAL;

                            // Figure out which role the user has in the channel (if the message was received in a channel)
                            if (message.IsChannelMessage)
                            {
                                try
                                {
                                    IrcChannel channel = client.Channels[message.Source];
                                    if (user.ChannelModes[channel] == 'o' || user.ChannelModes[channel] == 'O')
                                        level = AccessLevel.OPERATOR;
                                    else if (user.ChannelModes[channel] == 'v' || user.ChannelModes[channel] == 'V')
                                        level = AccessLevel.VOICE;
                                }
                                catch
                                {
                                    // ignored
                                }
                            }

                            // Maybe the user is a bot admin, which is independent on the channel status. Check that.
                            List<ProtoIrcAdmin> admins = Settings.Read<List<ProtoIrcAdmin>>("admins");
                            if (admins.Count(a => String.Equals(a.name, whoIs.LoggedInAs, StringComparison.InvariantCultureIgnoreCase)) == 1)
                            {
                                ProtoIrcAdmin admin = admins.FirstOrDefault(a => String.Equals(a.name, whoIs.LoggedInAs, StringComparison.InvariantCultureIgnoreCase));
                                if (admin != null && admin.root)
                                    level = AccessLevel.ROOT;
                                else if (admin != null)
                                    level = AccessLevel.ADMIN;
                            }
                            message.level = level;

                            // Does the user have the permission to call this command?
                            if (CheckPermission(command.GetAccessLevel(), level) || commandLine)
                            {
                                // Check the status of the current channel. If the channel is marked as serious, don't execute the command if it isn't marked as serious
                                if (message.IsChannelMessage)
                                {
                                    List<ProtoIrcChannel> channels = Settings.Read<List<ProtoIrcChannel>>("channels");
                                    ProtoIrcChannel channel = channels.FirstOrDefault(c => String.Equals(c.name, message.Source, StringComparison.InvariantCultureIgnoreCase));
                                    if (channel.serious && !command.IsSerious()) return;
                                }

                                // Speak English, bot!
                                Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                                try
                                {
                                    if (IrcCommand.ExecuteCheck.And(new Tuple<IrcUser, IrcCommand>(user, command)) || CheckPermission(AccessLevel.ADMIN, level))
                                        command.RunCommand(client, message);
                                }
                                catch (Exception e)
                                {
                                    SendMessage(client, e.Message, message.User, message.Source);
                                    log.Error(e.Message, e);
                                }
                            }
                            else
                                SendMessage(client, "You don't have the permission to use this command! Only " + command.GetAccessLevel() + " can use this command! You are " + level + ".", message.User, message.Source);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    SendMessage(client, "ChatSharp broke. Please contact your local doctor.", message.User, message.Source);
                    log.Error(e.Message, e);
                }

                // Run actions that should get executed after the WhoIs
                afterWhoIs?.Invoke();
            });
        }

        /// <summary>
        /// Sends a message to the IRC
        /// </summary>
        public static ProtoIrcMessage SendMessage(IrcClient client, String message, String from, String to, Boolean noname = false)
        {
            // We don't need empty stuff
            if (String.IsNullOrWhiteSpace(message))
                return new ProtoIrcMessage();

            // Format the message nicely
            message = Formatter.Format(message);

            // Split it into parts so nothing gets lost
            String[] splits = SplitInParts(message, 400).ToArray();

            // Loop through theese parts and send them to the server
            if (!to.StartsWith("#")) to = from;
            Int32 maxMsgs = Settings.Read<Int32>("maxMessages");
            
            // Warning
            if (splits.Length > maxMsgs)
            {
                String stateChan = Settings.Read<String>("stateChannel");
                if (!String.IsNullOrWhiteSpace(stateChan))
                    SendMessage(client, from + " issued a command that triggered a " + message.Length + " chars response.", "", stateChan, true);
            } 

            for (Int32 j = 0; j < Math.Min(maxMsgs, splits.Length); j++)
            {
                try
                {
                    if (j == 0 && !noname)
                        client.SendMessage(from + ": " + splits[j], to);
                    else
                        client.SendMessage(splits[j], to);
                }
                catch (Exception e)
                {
                    client.SendMessage(from + ": " + e.Message, to);
                }
            }

            // Do internal stuff
            ProtoIrcMessage proto = new ProtoIrcMessage()
            {
                IsChannelMessage = to.StartsWith("#"),
                Message = message,
                Source = to.StartsWith("#") ? to : client.User.Nick,
                User = client.User.Nick
            };
            PluginManager.Invoke("MessageSent", client, proto);
            return proto;
        }

        /// <summary>
        /// Splits a string into multiple chunks
        /// </summary>
        protected static IEnumerable<String> SplitInParts(String s, Int32 partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (Int32 i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

        /// <summary>
        /// Sends an action to the IRC
        /// </summary>
        public static ProtoIrcMessage SendAction(IrcClient client, String message, String to)
        {
            message = Formatter.Format(message);
            client.SendAction(message, to);
            ProtoIrcMessage proto = new ProtoIrcMessage()
            {
                IsChannelMessage = to.StartsWith("#"),
                Message = "ACTION" + message,
                Source = to.StartsWith("#") ? to : client.User.Nick,
                User = client.User.Nick
            };
            PluginManager.Invoke("MessageSent", client, proto);
            return proto;
        }

        /// <summary>
        /// Checks if two Access Levels are compatible
        /// </summary>
        public static Boolean CheckPermission(AccessLevel required, AccessLevel given)
        {
            if (required == AccessLevel.NORMAL)
                return true;
            else if (required == AccessLevel.VOICE)
                return given == AccessLevel.VOICE || given == AccessLevel.OPERATOR || given == AccessLevel.ADMIN || given == AccessLevel.ROOT;
            else if (required == AccessLevel.OPERATOR)
                return given == AccessLevel.OPERATOR || given == AccessLevel.ADMIN || given == AccessLevel.ROOT;
            else if (required == AccessLevel.ADMIN)
                return given == AccessLevel.ADMIN || given == AccessLevel.ROOT;
            else if (required == AccessLevel.ROOT)
                return given == AccessLevel.ROOT;
            return false;
        }

        public static ProtoIrcChannel GetChannel(String name)
        {
            List<ProtoIrcChannel> list = Settings.Read<List<ProtoIrcChannel>>("channels");
            return list.Find(c => String.Equals(c.name, name, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    /// <summary>
    /// The various access levels
    /// </summary>
    public enum AccessLevel
    {
        NORMAL,
        VOICE,
        OPERATOR,
        ADMIN,
        ROOT
    }
}
