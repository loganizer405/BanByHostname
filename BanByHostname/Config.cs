﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace BanByHostname
{
    public class Config
    {

       //define public variables here
        public List<BannedHost> BannedHostnames = new List<BannedHost>();

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Config Read(string path)
        {
            if (!File.Exists(path))
            {
                return new Config();
            }
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }
    }
}
