using AirmenMod;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WebAPI
{
    public class WebAPI
    {
        public static Queue<ChatMessage> ChatMessagesQueue;
        public static ulong ChatMessageUid = 0;

        public WebAPI()
        {
            Console.WriteLine("Web API created!");
            ChatMessagesQueue = new Queue<ChatMessage>();

            var server = new AsyncHttpServer(portNumber: 7788);

            Task.Run(() =>
            {
                try
                {
                    server.Start();
                    Console.WriteLine("SERVER: " + "started");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Source);
                    Console.WriteLine(ex.StackTrace);
                }
            });

            Players.ListenOnPlayerJoin(OnPlayerJoin);
            Players.ListenOnPlayerLeave(OnPlayerLeave);
            ChatMessages.ListenOnChatMessage(OnChatMessage);
        }

        public void OnPluginShutdown()
        {

        }

        public void OnPluginReload()
        {

        }

        public void OnPluginStop()
        {

        }

        public void OnPluginStart()
        {
            Console.WriteLine("Started!");
        }

        public static void OnPlayerJoin(Player p)
        {
            ChatMessagesQueue.Enqueue(new ChatMessage
            {
                steamId = p.GetSteamId().ToString(),
                timestamp = GetTimestamp(DateTime.Now),
                uid = ChatMessageUid++,
                alias = p.GetAlias(),
                join = true
            });
        }

        public static void OnPlayerLeave(Player p)
        {
            ChatMessagesQueue.Enqueue(new ChatMessage
            {
                steamId = p.GetSteamId().ToString(),
                timestamp = GetTimestamp(DateTime.Now),
                uid = ChatMessageUid++,
                alias = p.GetAlias(),
                leave = true
            });
        }

        public static ulong GetTimestamp(DateTime value)
        {
            return ulong.Parse(value.ToString("yyyyMMddHHmmss"));
        }

        public static void OnChatMessage(string m, Player sender, int team, Vector3? from)
        {
            ulong steamId = 0;

            if (sender != null)
            {
                steamId = sender.GetSteamId();
            }

            ChatMessagesQueue.Enqueue(new ChatMessage
            {
                message = m,
                steamId = steamId.ToString(),
                team = team,
                position = from.ToString(),
                timestamp = GetTimestamp(DateTime.Now),
                uid = ChatMessageUid++
            });

            while (ChatMessagesQueue.Count > 40)
            {
                ChatMessagesQueue.Dequeue();
            }
        }
    }

    public class AsyncHttpServer
    {
        private readonly HttpListener _listener;

        public AsyncHttpServer(int portNumber)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(string.Format("http://+:{0}/", portNumber));
        }

        public static List<Vector3> GetCirclePoints(int numPoints, float radius, Vector3 center, float yoffset = 0)
        {
            List<Vector3> points = new List<Vector3>();

            double slice = 2 * Math.PI / numPoints;

            for (int i = 0; i < numPoints; i++)
            {
                double angle = slice * i;
                points.Add(new Vector3((float)(center.x + radius * Math.Cos(angle)), Math.Min(center.y + yoffset, 390),
                    (float)(center.z + radius * Math.Sin(angle))));
            }

            return points;
        }

        public async Task SendSuccess(HttpListenerContext ctx, bool success = true)
        {
            var s = success ? "true" : "false";

            ctx.Response.Headers.Add("content-type: application/json; charset=UTF-8");

            using (var sw = new StreamWriter(ctx.Response.OutputStream))
            {
                await sw.WriteAsync("{\"success\": " + s + "}");
                await sw.FlushAsync();
            }
        }

        public async Task WriteJson(HttpListenerContext ctx, object response)
        {
            ctx.Response.Headers.Add("content-type: application/json; charset=UTF-8");

            try
            {

                using (var sw = new StreamWriter(ctx.Response.OutputStream))
                {
                    await sw.WriteAsync(JsonConvert.SerializeObject(response));
                    await sw.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Source);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public async Task Start()
        {
            _listener.Start();

            while (true)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();

                    //Console.Out.WriteLine("Client connected...");
                    //Console.Out.WriteLine("Serving file: '{0}'", ctx.Request.RawUrl);

                    var url = ctx.Request.RawUrl;

                    ctx.Response.Headers.Add("x-content-type-options: nosniff");
                    ctx.Response.Headers.Add("x-xss-protection:1; mode=block");
                    ctx.Response.Headers.Add("x-frame-options:DENY");
                    ctx.Response.Headers.Add("cache-control:no-store, no-cache, must-revalidate");
                    ctx.Response.Headers.Add("pragma:no-cache");
                    ctx.Response.Headers.Add("Server", "jl");

                    if (url.StartsWith("/api/kick/"))
                    {
                        var p = Players.GetBySteamId(url.Split('/')[3]);

                        if (p != null)
                        {
                            p.Kick();
                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url.StartsWith("/api/kill/"))
                    {
                        var p = Players.GetBySteamId(url.Split('/')[3]);

                        if (p != null)
                        {
                            p.Kill();
                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url == "/api/settings")
                    {
                        await WriteJson(ctx, ServerSettings.ToDict());
                    }
                    else
                    if (url.StartsWith("/api/add_ip_moderator/"))
                    {
                        ServerSettings.AddModerator(url.Split('/')[3]);
                        await SendSuccess(ctx);
                    }
                    else
                    if (url.StartsWith("/api/add_ip_admin/"))
                    {
                        ServerSettings.AddAdmin(url.Split('/')[3]);
                        await SendSuccess(ctx);
                    }
                    else
                    if (url.StartsWith("/api/add_ip_ban/"))
                    {
                        ServerSettings.AddBan(url.Split('/')[3]);
                        await SendSuccess(ctx);
                    }
                    else
                    if (url.StartsWith("/api/add_steam_moderator/"))
                    {
                        await SendSuccess(ctx,
                            ServerSettings.AddSteamModerator(url.Split('/')[3]));
                    }
                    else
                    if (url.StartsWith("/api/add_steam_admin/"))
                    {
                        await SendSuccess(ctx,
                            ServerSettings.AddSteamAdmin(url.Split('/')[3]));
                    }
                    else
                    if (url.StartsWith("/api/add_steam_ban/"))
                    {
                        await SendSuccess(ctx,
                            ServerSettings.AddSteamBan(url.Split('/')[3]));
                    }
                    else
                    if (url.StartsWith("/api/add_banned_part/"))
                    {
                        await SendSuccess(ctx,
                            ServerSettings.AddBannedPart(url.Split('/')[3]));
                    }
                    else
                    if (url.StartsWith("/api/remove_ip_moderator/"))
                    {
                        ServerSettings.RemoveModerator(url.Split('/')[3]);
                        await SendSuccess(ctx);
                    }
                    else
                    if (url.StartsWith("/api/remove_ip_admin/"))
                    {
                        ServerSettings.RemoveAdmin(url.Split('/')[3]);
                        await SendSuccess(ctx);
                    }
                    else
                    if (url.StartsWith("/api/remove_ip_ban/"))
                    {
                        ServerSettings.RemoveBan(url.Split('/')[3]);
                        await SendSuccess(ctx);
                    }
                    else
                    if (url.StartsWith("/api/remove_steam_moderator/"))
                    {
                        await SendSuccess(ctx,
                            ServerSettings.RemoveSteamModerator(url.Split('/')[3]));
                    }
                    else
                    if (url.StartsWith("/api/remove_steam_admin/"))
                    {
                        await SendSuccess(ctx,
                            ServerSettings.RemoveSteamAdmin(url.Split('/')[3]));
                    }
                    else
                    if (url.StartsWith("/api/remove_steam_ban/"))
                    {
                        await SendSuccess(ctx,
                            ServerSettings.RemoveSteamBan(url.Split('/')[3]));
                    }
                    else
                    if (url.StartsWith("/api/remove_banned_part/"))
                    {
                        await SendSuccess(ctx,
                            ServerSettings.RemoveBannedPart(url.Split('/')[3]));
                    }
                    else
                    if (url.StartsWith("/api/pteleport/"))
                    {

                        Vector json;
                        using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                        {
                            var _json = reader.ReadToEnd();
                            json = JsonConvert.DeserializeObject<Vector>(_json);
                        }

                        var p = Players.GetBySteamId(url.Split('/')[3]);

                        if (p != null)
                        {
                            p.Teleport(new Vector3(json.x, json.y, json.z));
                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url.StartsWith("/api/steleport/"))
                    {
                        Vector json;
                        using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                        {
                            var _json = reader.ReadToEnd();
                            json = JsonConvert.DeserializeObject<Vector>(_json);
                        }

                        var s = Ships.GetShipById(url.Split('/')[3]);

                        if (s != null)
                        {
                            s.Teleport(new Vector3(json.x, json.y, json.z));
                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url == "/api/send_chat_message")
                    {
                        Message json;
                        using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                        {
                            var _json = reader.ReadToEnd();
                            json = JsonConvert.DeserializeObject<Message>(_json);
                        }

                        ChatMessages.SendServerMessage("Server: " + json.message, null, -1);

                        WebAPI.ChatMessagesQueue.Enqueue(new ChatMessage
                        {
                            steamId = "69",
                            message = json.message,
                            timestamp = WebAPI.GetTimestamp(DateTime.Now),
                            uid = WebAPI.ChatMessageUid++,
                            alias = "Server",
                            team = -1
                        });

                        await SendSuccess(ctx);
                    }
                    else
                    if (url.StartsWith("/api/repair_ship/"))
                    {
                        var s = Ships.GetShipById(url.Split('/')[3]);

                        if (s != null)
                        {
                            s.RepairShip();
                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url.StartsWith("/api/refill_tools/"))
                    {
                        var s = Ships.GetShipById(url.Split('/')[3]);

                        if (s != null)
                        {
                            s.RefillTools();
                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url.StartsWith("/api/refill_scrap/"))
                    {
                        var s = Ships.GetShipById(url.Split('/')[3]);

                        if (s != null)
                        {
                            s.RefillScrap();
                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url == "/api/refill_all_scrap")
                    {
                        foreach (var ship in Ships.GetShips())
                        {
                            if (!ship.IsAi())
                                ship.RefillScrap();
                        }

                        await SendSuccess(ctx);
                    }
                    else
                    if (url == "/api/refill_all_tools")
                    {
                        foreach (var ship in Ships.GetShips())
                        {
                            if (!ship.IsAi())
                                ship.RefillTools();
                        }

                        await SendSuccess(ctx);
                    }
                    else
                    if (url == "/api/repair_all")
                    {
                        foreach (var ship in Ships.GetShips())
                        {
                            if (!ship.IsAi())
                                ship.RepairShip();
                        }

                        await SendSuccess(ctx);
                    }
                    else
                    if (url.StartsWith("/api/heal/"))
                    {
                        var p = Players.GetBySteamId(url.Split('/')[3]);

                        if (p != null)
                        {
                            p.ResetHp();
                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url == "/api/heal_all")
                    {
                        foreach (var player in Players.GetPlayers())
                        {
                            if (player.IsReady()) player.ResetHp();
                        }

                        await SendSuccess(ctx);
                    }
                    else
                    if (url.StartsWith("/api/teleport_all_players/"))
                    {
                        var players = Players.GetPlayers();
                        var p = Players.GetBySteamId(url.Split('/')[3]);

                        if (players.Count() > 1 && p != null && p.IsReady())
                        {
                            var points = GetCirclePoints(players.Count, 15, p.GetPosition(), 20);

                            for (var i = 0; i < players.Count; i++)
                            {
                                if (players[i].IsReady() && players[i].GetSteamId() != p.GetSteamId())
                                {
                                    players[i].Teleport(points[i]);
                                }
                            }

                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url.StartsWith("/api/teleport_all_ships/"))
                    {
                        var ships = Ships.GetShips();
                        var s = Ships.GetShipById(url.Split('/')[3]);

                        if (ships.Count() > 1 && s != null)
                        {
                            var points = GetCirclePoints(ships.Count, 140, s.GetPosition(), 50);

                            for (var i = 0; i < ships.Count; i++)
                            {
                                if (ships[i].IsAirship() && !ships[i].IsAi() && ships[i].GetId() != s.GetId())
                                {
                                    ships[i].Teleport(points[i]);
                                }
                            }

                            await SendSuccess(ctx);
                        }
                        else
                        {
                            await SendSuccess(ctx, false);
                        }
                    }
                    else
                    if (url == "/api/players")
                    {
                        var response = new PlayersResponse();

                        foreach (var player in Players.GetPlayers())
                        {
                            response.players.Add(player.ToDict());
                        }

                        await WriteJson(ctx, response);
                    }
                    else if (url == "/api/chat_messages")
                    {
                        try
                        {
                            ctx.Response.Headers.Add("content-type: application/json; charset=UTF-8");

                            using (var sw = new StreamWriter(ctx.Response.OutputStream))
                            {
                                var arr = WebAPI.ChatMessagesQueue.ToArray();

                                if (arr.Length == 0)
                                {
                                    await sw.WriteAsync("[]");
                                }
                                else
                                {
                                    await sw.WriteAsync(JsonConvert.SerializeObject(arr));
                                }
                                await sw.FlushAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.Source);
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                    else
                    if (url == "/api/ships")
                    {
                        var response = new ShipsResponse();

                        foreach (var ship in Ships.GetShips())
                        {
                            response.ships.Add(ship.ToDict());
                        }

                        await WriteJson(ctx, response);
                    }
                    else
                    {
                        ctx.Response.Headers.Add("content-type: application/json; charset=UTF-8");
                        using (var sw = new StreamWriter(ctx.Response.OutputStream))
                        {
                            await sw.WriteAsync("{}");
                            await sw.FlushAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Source);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        public async Task Stop()
        {
            await Console.Out.WriteLineAsync(
                "Stopping server...");

            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }
    public class ChatMessage
    {
        public string message;
        public string steamId;
        public int team;
        public ulong timestamp;
        public string position;
        public ulong uid;
        public bool join;
        public bool leave;
        public string alias;
    }

    public class Vector
    {
        public float x;
        public float y;
        public float z;
    }

    public class Message
    {
        public string message;
    }

    public class Delta
    {
        public float amount;
    }

    public class ShipsResponse
    {
        public List<Dictionary<string, dynamic>> ships;

        public ShipsResponse()
        {
            ships = new List<Dictionary<string, dynamic>>();
        }
    }

    public class PlayersResponse
    {
        public List<Dictionary<string, dynamic>> players { get; set; }

        public PlayersResponse()
        {
            players = new List<Dictionary<string, dynamic>>();
        }
    }
}
