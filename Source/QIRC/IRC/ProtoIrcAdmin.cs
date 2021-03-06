﻿/** 
 * .NET Bot for Internet Relay Chat (IRC)
 * Copyright (c) Dorian Stoll 2017
 * QIRC is licensed under the MIT License
 */
 
using System;

namespace QIRC.IRC
{
    /// <summary>
    /// The loadable definitions for an IRC bot admin
    /// </summary>
    public class ProtoIrcAdmin
    {
        /// <summary>
        /// The name of the admin. Must be the WHOIS name!
        /// </summary>
        public String name { get; set; }

        /// <summary>
        /// Whether the Admin has complete access
        /// </summary>
        public Boolean root { get; set; }
    }
}
