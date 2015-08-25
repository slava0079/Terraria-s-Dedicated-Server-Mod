﻿using System;
using System.Linq;
using Microsoft.Xna.Framework;
using TDSM.API.Logging;


#if Full_API
using Terraria;
using System.Collections.Generic;
#endif

namespace TDSM.API
{
    public static class Tools
    {
        public static void NotifyAllPlayers(string message, Color color, bool writeToConsole = true) //, SendingLogger Logger = SendingLogger.CONSOLE)
        {
#if Full_API
            foreach (var player in Main.player)
            {
                if (player != null && player.active)
                    NetMessage.SendData((int)Packet.PLAYER_CHAT, player.whoAmI, -1, message, 255 /* PlayerId */, color.R, color.G, color.B);
            }

            if (writeToConsole)
                ProgramLog.Log(message);
#endif
        }

        public static void NotifyAllOps(string message, bool writeToConsole = true) //, SendingLogger Logger = SendingLogger.CONSOLE)
        {
#if Full_API
            foreach (var player in Main.player)
            {
                if (player != null && player.active && player.Op)
                    NetMessage.SendData((int)Packet.PLAYER_CHAT, player.whoAmI, -1, message, 255 /* PlayerId */, 176f, 196, 222f);
            }

            if (writeToConsole)
                ProgramLog.Log(message);
#endif
        }
        #if Full_API
        /// <summary>
        /// Gets a specified Online Player
        /// Input name must already be cleaned of spaces
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Player GetPlayerByName(string name)
        {
            string lowercaseName = name.ToLower();
            foreach (Player player in Main.player)
            {
                if (player != null && player.active && player.Name.ToLower().Equals(lowercaseName))
                    return player;
            }
            return null;
        }

        /// <summary>
        /// Gets the total of all active NPCs
        /// </summary>
        /// <returns></returns>
        public static int ActiveNPCCount()
        {
            /*int npcCount = 0;
            for (int i = 0; i < Main.npcs.Length - 1; i++)
            {
                if (Main.npcs[i].Active)
					npcCount++;
            }
            return npcCount;*/
            return (from x in Main.npc
                             where x != null && x.active
                             select x).Count();
        }

        /// <summary>
        /// Finds a valid location for such things as NPC Spawning
        /// </summary>
        /// <param name="point"></param>
        /// <param name="defaultResist"></param>
        /// <returns></returns>
        public static bool IsValidLocation(Vector2 point, bool defaultResist = true)
        {
            if (point != null && (defaultResist) ? (point != Vector2.Zero) : true)
            if (point.X <= Main.maxTilesX && point.X >= 0)
            {
                if (point.Y <= Main.maxTilesY && point.Y >= 0)
                    return true;
            }

            return false;
        }

        ///// <summary>
        ///// Checks whether an item is rejected in this server
        ///// </summary>
        ///// <param name="item"></param>
        ///// <returns></returns>
        //public static bool RejectedItemsContains(string item)
        //{
        //    if (!String.IsNullOrEmpty(item))
        //    {
        //        foreach (string rItem in RejectedItems)
        //        {
        //            if (rItem.Trim().Replace(" ", String.Empty) == item.Trim().Replace(" ", String.Empty))
        //                return true;
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// Checks online players for a matching name part
        /// </summary>
        /// <param name="partName"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static List<Player> FindPlayerByPart(string partName, bool ignoreCase = true)
        {
            List<Player> matches = new List<Player>();

            foreach (var player in Main.player)
            {
                if (player == null || player.Name == null)
                    continue;

                string playerName = player.Name;

                if (ignoreCase)
                    playerName = playerName.ToLower();

                if (playerName.StartsWith((ignoreCase) ? partName.ToLower() : partName))
                    matches.Add(player);
            }

            return matches;
        }

        ///// <summary>
        ///// Tries to find an item by Type or Name via Definitions
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="ItemIdOrName"></param>
        ///// <param name="ItemList"></param>
        ///// <returns></returns>
        //public static bool TryFindItem<T>(T ItemIdOrName, out List<ItemInfo> ItemList)
        //{
        //    ItemList = new List<ItemInfo>();

        //    foreach (var pair in Registries.Item.TypesById)
        //    {
        //        var items = pair.Value as List<Item>;

        //        if (ItemIdOrName is Int32)
        //        {
        //            var itemT = Int32.Parse(ItemIdOrName.ToString());

        //            foreach (var item in items)
        //            {
        //                var type = item.Type;
        //                if (type == itemT && !ItemList.ContainsType(type))
        //                    ItemList.Add(new ItemInfo()
        //                    {
        //                        Type = type,
        //                        NetID = item.NetID
        //                    });
        //            }
        //        }
        //        else if (ItemIdOrName is String)
        //        {
        //            var findItem = CleanName(ItemIdOrName as String);

        //            foreach (var item in items)
        //            {
        //                var type = item.Type;
        //                var curItem = CleanName(item.Name);

        //                if (curItem == findItem && !ItemList.ContainsType(type))
        //                    ItemList.Add(new ItemInfo()
        //                    {
        //                        Type = type,
        //                        NetID = item.NetID
        //                    });
        //            }
        //        }
        //    }

        //    foreach (var pair in Registries.Item.TypesByName)
        //    {
        //        var item = pair.Value as Item;

        //        if (ItemIdOrName is Int32)
        //        {
        //            var itemT = Int32.Parse(ItemIdOrName.ToString());
        //            var type = item.Type;

        //            if (type == itemT && !ItemList.ContainsType(type))
        //                ItemList.Add(new ItemInfo()
        //                {
        //                    Type = type,
        //                    NetID = item.NetID
        //                });
        //        }
        //        else if (ItemIdOrName is String)
        //        {
        //            var type = item.Type;
        //            var findItem = CleanName(ItemIdOrName as String);
        //            var curItem = CleanName(item.Name);

        //            if (curItem == findItem && !ItemList.ContainsType(type))
        //                ItemList.Add(new ItemInfo()
        //                {
        //                    Type = type,
        //                    NetID = item.NetID
        //                });
        //        }
        //    }

        //    return ItemList.Count > 0;
        //}

        ///// <summary>
        ///// Uses the undefined item method to find an item by Type.
        ///// </summary>
        ///// <param name="ItemID"></param>
        ///// <param name="ItemList"></param>
        ///// <returns></returns>
        //public static bool TryFindItemByType(int itemId, out List<ItemInfo> ItemList)
        //{
        //    return itemId(ItemID, out ItemList);
        //}

        ///// <summary>
        ///// Uses the undefined item method to find an item by Name.
        ///// </summary>
        ///// <param name="ItemName"></param>
        ///// <param name="ItemList"></param>
        ///// <returns></returns>
        //public static bool TryFindItemByName(string name, out List<ItemInfo> ItemList)
        //{
        //    return TryFindItem(name, out ItemList);
        //}

        /// <summary>
        /// Used to clean the names of Items or NPC's for parsing
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string CleanName(string input)
        {
            return input.Replace(" ", String.Empty).ToLower();
        }

        /// <summary>
        /// Attempts to find the first online player
        ///		Usually the Slot Manager assigns them to the lowest possible index
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool TryGetFirstOnlinePlayer(out Player player)
        {
            player = null;
            try
            {
                for (var i = 0; i < Main.player.Length; i++)
                {
                    var ply = Main.player[i];
                    if (ply != null && ply.active && ply.Name.Trim() != String.Empty)
                    {
                        player = ply;
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool ContainsType(this List<ItemInfo> list, int type)
        {
            return list.Where(x => x.Type == type).Count() > 0;
        }

        /// <summary>
        /// Checks if there are any active NPCs of specified type
        /// </summary>
        /// <param name="type">TypeId of NPC to check for</param>
        /// <returns>True if active, false if not</returns>
        public static bool IsNPCSummoned(int type)
        {
            //for (int i = 0; i < Main.npc.Length; i++)
            //{
            //    NPC npc = Main.npc[i];
            //    if (npc != null && npc.active && npc.type == type)
            //        return true;
            //}
            //return false;
            return NPC.AnyNPCs(type);
        }

        /// <summary>
        /// Checks if there are any active NPCs of specified name
        /// </summary>
        /// <param name="Name">Name of NPC to check for</param>
        /// <returns>True if active, false if not</returns>
        public static bool IsNPCSummoned(string name)
        {
            int Id;
            return TryFindNPCByName(name, out Id);
        }

        public static bool TryFindNPCByName(string name, out int id)
        {
            id = default(Int32);

            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC npc = Main.npc[i];
                if (npc != null && npc.active && npc.name == name)
                {
                    id = npc.whoAmI;
                    return true;
                }
            }
            return false;
        }

        public static int FindExistingProjectileForUser(int playerId, int identity)
        {
            for (int x = 0; x < 1000; x++)
            {
                var prj = Main.projectile[x];
                if (prj != null && prj.owner == playerId && prj.identity == identity && prj.active)
                    return x;
            }
            return -1;
        }

        public static int AvailableNPCSlots
        {
            get
            {
                return Main.npc
                  .Where(x => x == null || !x.active)
                  .Count();
            }
        }

        public static int UsedNPCSlots
        {
            get
            {
                return Main.npc
                  .Where(x => x != null && x.active)
                  .Count();
            }
        }

        public static int AvailableItemSlots
        {
            get
            {
                return Main.item
                  .Where(x => x == null || !x.active)
                  .Count();
            }
        }

        public static int UsedItemSlots
        {
            get
            {
                return Main.item
                  .Where(x => x != null && x.active)
                  .Count();
            }
        }

        public static int ActivePlayerCount
        {
            get
            {
                return (from p in Terraria.Main.player
                                    where p != null && p.active
                                    select p.Name).Count();
            }
        }

        public static int MaxPlayers
        {
            get
            {
                return Main.maxNetPlayers;
            }
        }
        #endif
        public static RuntimePlatform RuntimePlatform
        {
            get
            { return Type.GetType("Mono.Runtime") != null ? RuntimePlatform.Mono : RuntimePlatform.Microsoft; }
        }

        #region "Encoding"

        public static class Encoding
        {
            public const Int32 BitsPerByte = 8;

            public static uint EncodeColor(Color color)
            {
                return color.PackedValue;
            }

            public static uint EncodeColor(int r, int g, int b, int a = 255)
            {
                return Color.PackHelper(r, g, b, a);
            }

            public static Color DecodeColor(uint color)
            {
                return new Color(color);
            }

            public static bool[] DecodeBits(byte data)
            {
                var bits = new bool[BitsPerByte];
                for (int i = 0; i < bits.Length; i++)
                    bits[i] = (data & 1 << i) != 0;

                return bits;
            }

            public static bool[] DecodeBits(short data)
            {
                var bits = new bool[2 * BitsPerByte];
                for (int i = 0; i < bits.Length; i++)
                    bits[i] = (data & 1 << i) != 0;

                return bits;
            }

            public static bool[] DecodeBits(int data)
            {
                var bits = new bool[4 * BitsPerByte];
                for (int i = 0; i < bits.Length; i++)
                    bits[i] = (data & 1 << i) != 0;

                return bits;
            }

            public static byte EncodeByte(bool[] bits)
            {
                if (bits == null) return 0;
                if (bits.Length >= 0 && bits.Length <= 8)
                {
                    byte value = 0;
                    for (int i = 0; i < bits.Length; i++)
                    {
                        if (bits[i]) value |= (byte)(1 << i);
                    }

                    return value;
                }
                else throw new ArgumentOutOfRangeException();
            }

            public static short EncodeShort(bool[] bits)
            {
                if (bits == null) return 0;
                if (bits.Length >= 0 && bits.Length <= 16)
                {
                    short value = 0;
                    for (int i = 0; i < bits.Length; i++)
                    {
                        if (bits[i]) value |= (short)(1 << i);
                    }

                    return value;
                }
                else throw new ArgumentOutOfRangeException();
            }

            public static int EncodeInteger(bool[] bits)
            {
                if (bits == null) return 0;
                if (bits.Length >= 0 && bits.Length <= 32)
                {
                    int value = 0;
                    for (int i = 0; i < bits.Length; i++)
                    {
                        if (bits[i]) value |= (1 << i);
                    }

                    return value;
                }
                else throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }

    public struct ItemInfo
    {
        public int NetID { get; set; }

        public int Type { get; set; }
    }

    public enum RuntimePlatform
    {
        Microsoft = 1,
        //.net
        Mono
    }
}