using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Newtonsoft.Json;
using System.Net;


namespace BanByHostname
{
    [ApiVersion(1, 16)]
    public class BanByHostname : TerrariaPlugin
    {
        string path = Path.Combine(TShock.SavePath, "BanByHostname.json");
        Config Config = new Config();
        public override string Name
        {
            get
            {
                return "BanByHostname";
            }
        }
        public override string Author
        {
            get
            {
                return "Loganizer.";
            }
        }
        public override string Description
        {
            get
            {
                return "Bans players by their hostname.";
            }
        }
        public override Version Version
        {
            get
            {
                return new Version("1.0");
            }
        }
        public BanByHostname(Main game)
            : base(game)
        {
            Order = 1;
        }
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            }
            base.Dispose(disposing);
        }
        public void OnInitialize(EventArgs args)
        {
            if (!File.Exists(path))
            {
                Config.Write(path);
            }
            Config = Config.Read(path);
            Commands.ChatCommands.Add(new Command("banhost.use", Hostname, "hostname"));
        }
        private void OnJoin(JoinEventArgs e)
        {
            string ip = TShock.Players[e.Who].IP;
            string plrhost = GetHost(ip);
            Config.Read(path);
            foreach (BannedHost host in Config.BannedHostnames)
            {
                if (host.hostname.Contains(plrhost))
                {
                    TShock.Players[e.Who].Disconnect("You are banned: " + host.reason + ".");
                }
            }
        }
        void Hostname(CommandArgs e)
        {
            switch (e.Parameters[0].ToLower())
            {
                case "ban":
                    {
                        List<TSPlayer> players = TShock.Utils.FindPlayer(e.Parameters[1]);
                        if (players.Count == 0)
                        {
                            e.Player.SendErrorMessage("No player found by that name!");
                            return;
                        }
                        else if (players.Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(e.Player, players.Select(p => p.Name));
                            return;
                        }
                        else if (players.Count == 1)
                        {
                            var plr = players[0];
                            string host = GetHost(plr.IP);
                            
                            string reason;
                            if (string.IsNullOrEmpty(e.Parameters[2]))
                            {
                                reason = "Misbehavior";
                            }
                            else
                            {
                                reason = e.Parameters[2];
                            }
                            BannedHost ban = new BannedHost(host, reason);
                            Config.BannedHostnames.Add(ban);
                            Config.Write(path);
                            e.Player.SendInfoMessage("Banned " + plr.Name + "'s hostname: \"" + host + "\".");                       
                            e.Player.SendInfoMessage("Reason: " + reason);
                            TShock.Utils.Kick(plr, "You have been banned: " + reason);
                            return;
                        }
                        break;
                    }
                case "banhost":
                    {
                        string reason;
                        if (string.IsNullOrEmpty(e.Parameters[2]))
                        {
                            reason = "Misbehavior";
                        }
                        else
                        {
                            reason = e.Parameters[2];
                        }
                        BannedHost ban = new BannedHost(e.Parameters[1], reason);
                        Config.BannedHostnames.Add(ban);
                        Config.Write(path);
                        e.Player.SendInfoMessage("Successfully banned hostname \"" + e.Parameters[1] + "\" for " + reason + ".");
                        break;
                    }
                case "check":
                case "view":
                    {
                        List<TSPlayer> players = TShock.Utils.FindPlayer(e.Parameters[1]);
                        if (players.Count == 0)
                        {
                            e.Player.SendErrorMessage("No player found by that name!");
                            return;
                        }
                        else if (players.Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(e.Player, players.Select(p => p.Name));
                            return;
                        }
                        else if (players.Count == 1)
                        {
                            var plr = players[0];
                            e.Player.SendInfoMessage(plr.Name + "'s hostname is \"" + GetHost(plr.IP) + "\".");
                            return;
                        }
                        break;
                    }
                case "unban":
                case "delete":
                    {
                        Config.Read(path);
                        string host = e.Parameters[1];
                        foreach (BannedHost ban in Config.BannedHostnames)
                        {
                            if(ban.hostname == host)
                            {
                                Config.BannedHostnames.Remove(ban);
                                Config.Write(path);
                                e.Player.SendInfoMessage("Successfully removed the ban on hostname \"" + host + "\".");
                                return;
                            }
                        }
                        e.Player.SendInfoMessage("No bans exist for the hostname \"" + host + "\".");
                        break;
                    }
                case "viewlist":
                case "checklist":
                    {
                        Config.Read(path);
                        if (Config.BannedHostnames.Count == 0)
                        {
                            e.Player.SendInfoMessage("No hostnames have been banned.");
                            return;
                        }
                        else
                        {
                            StringBuilder builder = new StringBuilder();
                            foreach (BannedHost host in Config.BannedHostnames)
                            {
                                builder.Append(host.hostname).Append(", ");
                            }
                            e.Player.SendInfoMessage("Banned hostnames: " + builder);
                            break;
                        }
                    }
                case "":
                case null:
                default:
                    {
                        e.Player.SendInfoMessage("Invalid  or no command entered. Proper parameters: ban, banhost, check, unban, viewlist.");
                    }
                    break;
            }
        }
        string GetHost(string ip)
        {
            System.Net.IPHostEntry host;
            host = System.Net.Dns.GetHostEntry(ip);
            return host.HostName;
        }
    }
}
