﻿/** 
 * .NET Bot for Internet Relay Chat (IRC)
 * Copyright (c) Dorian Stoll 2017
 * QIRC is licensed under the MIT License
 */

using System;
using ChatSharp;
using QIRC.Configuration;
using QIRC.Plugins;

namespace QIRC.NickServ
{
    /// <summary>
    /// This is the IrcPlugin that loads information about NickServ and 
    /// uses them to log into NickServ
    /// </summary>
    public class NickServ : IrcPlugin
    {
        /// <summary>
        /// Creates the prefab for the loaded info
        /// </summary>
        public override void OnLoad()
        {
            SettingsFile file = null;
            Settings.GetFile("connection", ref file);
            file.Add("NickServ", new NickServInfo());
        }

        /// <summary>
        /// When we are connected, log in
        /// </summary>
        /// <param name="client"></param>
        public override void OnConnectionComplete(IrcClient client)
        {
            NickServInfo info = Settings.Read<NickServInfo>("NickServ");
            client.SendMessage("identify " + info.name + " " + info.password, "NickServ");
        }

        /// <summary>
        /// The connection info
        /// </summary>
        public class NickServInfo
        {
            public String name = "";
            public String password = "";
        }
    }
}
