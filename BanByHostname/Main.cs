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
    [ApiVersion(1, 17)]
    public class BanByHostname : TerrariaPlugin
    {
        string path = Path.Combine(TShock.SavePath, "BannedHostnames.json");
        Config Config = new Config();
        bool hostbans = true;
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
                return new Version("1.1");
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

            if (Config.BannedHostnames.Count == 0) { hostbans = false; }

            Commands.ChatCommands.Add(new Command("banhost.use", Hostname, "hostname"));
        }
        private async void OnJoin(JoinEventArgs e)
        {
            string ip = TShock.Players[e.Who].IP;
            string plrhost;
            plrhost = await GetHost(ip);
            if (string.IsNullOrEmpty(plrhost))
            {
                Log.ConsoleError("Could not find hostname for " + TShock.Players[e.Who].Name + ".");
                Log.Warn("Could not find hostname for " + TShock.Players[e.Who].Name + ".");
                return;
            }
            Config.Read(path);
            List<BannedHost> bannedhosts = Config.BannedHostnames;
            foreach (BannedHost host in bannedhosts)
            {
                if (plrhost.Contains(host.hostname))
                {
                    TShock.Players[e.Who].Disconnect("You are banned: " + host.reason + ".");
                }
            }
        }
        async void Hostname(CommandArgs e)
        {
            if (string.IsNullOrEmpty(e.Parameters[0]) || e.Parameters.Count == 0)
            {
                e.Player.SendInfoMessage("No subcommand entered. Proper parameters: ban, banhost, check, unban, viewlist.");
                return;
            }
            switch (e.Parameters[0].ToLower())
            {
                case "ban":
                    {
                        if (!e.Player.Group.HasPermission("banhost.ban") && !e.Player.Group.HasPermission("banhost.*"))
                        {
                            e.Player.SendErrorMessage("You do not have permission to execute this command!");
                            return;
                        }
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
                        var plr = players[0];
                        string host;
                        host = await GetHost(plr.IP);
                        if (string.IsNullOrEmpty(host))
                        {
                            Log.ConsoleError("Could not find hostname for " + plr.Name + ".");
                            Log.Warn("Could not find hostname for " + plr.Name + ".");
                            return;
                        }                        
                        string reason;
                        if (e.Parameters.Count == 2)
                        {
                            reason = "Misbehavior";
                        }
                        else if (string.IsNullOrEmpty(e.Parameters[2]))
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
                        //TShock.Utils.Kick(plr, "You have been banned: " + reason); //doesn't work?
                        hostbans = true;
                        break;
                    }
                case "banhost":
                    {
                        if (!e.Player.Group.HasPermission("banhost.ban") && !e.Player.Group.HasPermission("banhost.*"))
                        {
                            e.Player.SendErrorMessage("You do not have permission to execute this command!");
                            return;
                        }
                        string reason;
                        if (e.Parameters.Count == 3)
                        {
                            reason = "Misbehavior";
                        }
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
                        hostbans = true;
                        break;
                    }
                case "check":
                case "view":
                    {
                        if (!e.Player.Group.HasPermission("banhost.view") && !e.Player.Group.HasPermission("banhost.*"))
                        {
                            e.Player.SendErrorMessage("You do not have permission to execute this command!");
                            return;
                        }
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
                            string host;
                            host = await GetHost(plr.IP);
                            if (string.IsNullOrEmpty(host))
                            {
                                Log.ConsoleError("Could not find hostname for " + plr.Name + ".");
                                Log.Warn("Could not find hostname for " + plr.Name + ".");
                                e.Player.SendInfoMessage("Could not find hostname for " + plr.Name);
                                return;
                            }  
                            e.Player.SendInfoMessage(plr.Name + "'s hostname is \"" + host + "\".");
                            return;
                        }
                        break;
                    }
                case "unban":
                case "delete":
                    {
                        if (!e.Player.Group.HasPermission("banhost.remove") && !e.Player.Group.HasPermission("banhost.*"))
                        {
                            e.Player.SendErrorMessage("You do not have permission to execute this command!");
                            return;
                        }
                        Config.Read(path);
                        List<BannedHost> bannedhosts = Config.BannedHostnames;
                        string host = e.Parameters[1];
                        foreach (BannedHost ban in bannedhosts)
                        {
                            if (ban.hostname == host)
                            {
                                Config.BannedHostnames.Remove(ban);
                                Config.Write(path);
                                e.Player.SendInfoMessage("Successfully removed the ban on hostname \"" + host + "\".");
                                return;
                            }
                        }
                        e.Player.SendInfoMessage("No bans exist for the hostname \"" + host + "\".");

                        if (Config.BannedHostnames == null) { hostbans = false; }
                        break;
                    }
                case "viewlist":
                case "checklist":
                    {
                        if (!e.Player.Group.HasPermission("banhost.ban") && !e.Player.Group.HasPermission("banhost.*"))
                        {
                            e.Player.SendErrorMessage("You do not have permission to execute this command!");
                            return;
                        }
                        if (!hostbans)
                        {
                            e.Player.SendInfoMessage("No hostnames have been banned.");
                            return;
                        }

                        else
                        {
                            Config.Read(path);
                            List<BannedHost> bannedhosts = Config.BannedHostnames;
                            StringBuilder builder = new StringBuilder();
                            foreach (BannedHost host in bannedhosts)
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
                        e.Player.SendInfoMessage("Invalid subcommand entered. Proper parameters: ban, banhost, check, unban, viewlist.");
                        break;
                    }
            }
        }
        async Task<string> GetHost(string ip)
        {
            return await Task.Run(() =>
            {
                try
                {
                    System.Net.IPHostEntry host;
                    host = System.Net.Dns.GetHostEntry(ip);

                    if (string.IsNullOrEmpty(host.HostName)) { return null; }
                    return host.HostName;
                }
                catch
                {
                    return null;
                }
            });
        }
    }
}
