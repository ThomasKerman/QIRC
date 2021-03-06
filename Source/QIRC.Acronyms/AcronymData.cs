﻿/** 
 * .NET Bot for Internet Relay Chat (IRC)
 * Copyright (c) Dorian Stoll 2017
 * QIRC is licensed under the MIT License
 */

using System;
using QIRC.Serialization;
using SQLite;

namespace QIRC.Acronyms
{
    /// <summary>
    /// Stores the data for one acronym
    /// </summary>
    public class AcronymData : Storage<AcronymData>
    {
        [PrimaryKey, Unique, NotNull]
        public String Short { get; set; }

        [NotNull]
        public String Explanation { get; set; }

        public AcronymData() { }

        public AcronymData(String shortname, String explanation)
        {
            Short = shortname;
            Explanation = explanation;
        }
    }
}