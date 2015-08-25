﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using tdsm.api;
using tdsm.api.Plugin;
using tdsm.core.Logging;
using tdsm.core.Messages.Out;
using tdsm.core.Misc;
using Terraria;

namespace tdsm.core.ServerCore
{
    public class ClientConnection : Connection, IPlayerConnection
    {
        int assignedSlot = -1;
        int messageLength = 0;
        int indexInAll = -1;
        new volatile int timeout = 0;

        //		byte[] sideBuffer;
        //		int    sideBytes;
        //		int    sideLength;

        volatile SlotState state = SlotState.CONNECTED;

        static Stopwatch time = new Stopwatch();

        public SlotState State
        {
            get { return state; }
            set
            {
                if ((state & SlotState.DISCONNECTING) != 0 && (value & SlotState.DISCONNECTING) == 0)
                {
                    ProgramLog.Log(Environment.StackTrace);
                    return;
                }

                state = value;
                if (state == SlotState.PLAYING)
                {
                    joinedAt = time.Elapsed;
                }
            }
        }

        public bool DisconnectInProgress()
        {
            return State.DisconnectInProgress();
        }

        public bool HasConnected()
        {
            return State >= SlotState.CONNECTED;
        }

        TimeSpan connectedAt;
        TimeSpan joinedAt;

        public TimeSpan Age
        {
            get
            {
                return time.Elapsed - connectedAt;
            }
        }

        public TimeSpan PlayTime
        {
            get
            {
                if (joinedAt != default(TimeSpan))
                    return time.Elapsed - joinedAt;
                else
                    return TimeSpan.FromSeconds(0);
            }
        }

        public int SlotIndex
        {
            get { return assignedSlot; }
        }

        /// <summary>
        /// Gets or sets the desired queue.
        /// </summary>
        /// <value>
        /// 0, 1, 2 - queue number (higher number is higher priority)
        /// 3 - bypass queues and use privileged slot
        /// </value>
        public int DesiredQueue { get; set; }
        public int Queue { get; internal set; }
        public int IndexInQueue { get; internal set; }

        public Player Player { get; set; }

        public int CompressionVersion { get; set; }

        public static List<ClientConnection> All { get; private set; }

        static ClientConnection()
        {
            time.Start();

            All = new List<ClientConnection>();

            var thread = new ProgramThread("TmoL", TimeoutLoop);
            thread.IsBackground = true;
            thread.Start();
        }

        public ClientConnection(Socket socket, int slot)
            : base(socket)
        {
            //var buf = NewNetMessage.buffer[id];
            //socket.SendBufferSize = 128000;
            connectedAt = time.Elapsed;

            if (slot >= 0) AssignSlot(slot);

            socket.LingerState = new LingerOption(true, 10);

            var ctx = new HookContext
            {
                Connection = this
            };

            var args = new HookArgs.NewConnection();

            HookPoints.NewConnection.Invoke(ref ctx, ref args);

            if (ctx.CheckForKick())
                return;

            lock (All)
            {
                indexInAll = All.Count;
                All.Add(this);
            }

            StartReceiving(new byte[4192]);
        }

        public override void Send(byte[] bytes)
        {
            base.Send(bytes);
        }

        public bool AssignSlot(int id)
        {
            if (Interlocked.CompareExchange(ref assignedSlot, id, -1) == -1)
            {
                txBuffer = new byte[16384];
                return true;
            }
            return false;
        }

        //		public void ProcessSideBuffer ()
        //		{
        //			DecodeMessages (sideBuffer, ref sideBytes, ref sideLength);
        //			sideBuffer = null;
        //			sideBytes = 0;
        //			sideLength = 0;
        //		}

        protected override void ProcessRead()
        {
            //ProgramLog.Log ("Read (total={0}).", recvBytes);
            try
            {
                DecodeMessages(recvBuffer, ref recvBytes, ref messageLength);
            }
            catch (Exception e)
            {
                ProgramLog.Log("Error processing read from client {0} @ {1}\n{2}", RemoteAddress, assignedSlot, e);
                Kick("Server malfunction, please reconnect.");
            }
            //ProgramLog.Log ("After read (total={0}).", recvBytes);
        }

        protected override void HandleClosure(SocketError err)
        {
            state = SlotState.SHUTDOWN;

            var slot = Interlocked.Exchange(ref assignedSlot, -2);
            if (slot >= 0)
            {
                ProgramLog.Log("{0} @ {1}: connection closed ({2}).", RemoteAddress, slot, err);
                SlotManager.FreeSlot(slot);
                //Netplay.slots[slot].Reset ();
            }
            else
            {
                SlotManager.RemoveFromQueues(this);
                ProgramLog.Log("{0}: connection closed ({1}).", RemoteAddress, err);
            }

            var player = Player;
            if (player != null)
            {
                player.Connection = null;
                player.active = false;
            }

            FreeSectionBuffer();

            lock (All)
            {
                if (indexInAll == All.Count - 1)
                {
                    All.RemoveAt(All.Count - 1);
                }
                else if (indexInAll >= 0)
                {
                    var other = All[All.Count - 1];
                    other.indexInAll = indexInAll;
                    All[indexInAll] = other;
                    All.RemoveAt(All.Count - 1);
                    indexInAll = -1;
                }
            }
        }

        NewNetMessage sectionBuffer;

        protected void FreeSectionBuffer()
        {
            if (sectionBuffer != null)
            {
                var buf = sectionBuffer;
                sectionBuffer = null;
                FreeSectionBuffer(buf);
            }
        }

        static long _compressed = 0;
        static long _uncompressed = 0;

        protected override ArraySegment<byte> SerializeMessage(Message msg)
        {
            switch (msg.kind)
            {
                case 10:
                    {
                        // TODO: optimize further
                        var buf = TakeSectionBuffer();
                        var sX = (msg.param >> 16) * 200;
                        var sY = (msg.param & 0xffff) * 150;

                        int uncompressed = 0;

                        for (int y = sY; y < sY + 150; y++)
                        {
                            if (CompressionVersion == 1)
                            {
                                uncompressed += buf.TileRowSize(200, sX, y);
                                buf.TileRowCompressed(200, sX, y);
                            }
                            else
                                buf.SendTileRowCompressed(sX, y, 200, 150, true);

                        }

                        if (uncompressed > 0)
                        {
                            var c = System.Threading.Interlocked.Add(ref _compressed, buf.Written);
                            var u = System.Threading.Interlocked.Add(ref _uncompressed, uncompressed);
                            ProgramLog.Log("Total section compression ratio: {2:0.00}% ({0:0.0}MB -> {1:0.0}MB)", u / 1024.0 / 1024.0, c / 1024.0 / 1024.0, c * 100.0 / u);
                        }

                        sectionBuffer = buf;
                        //ProgramLog.Log ("{0} @ {1}: Sending section ({2}, {3}) of {4} bytes.", RemoteAddress, assignedSlot, sX, sY, buf.Segment.Count);

                        return buf.Segment;
                    }
            }
            return new ArraySegment<byte>();
        }

        protected override void MessageSendCompleted()
        {
            FreeSectionBuffer();
        }

        public void DecodeMessages(byte[] readBuffer, ref int totalData, ref int msgLen)
        {
            int processed = 0;
            const Int32 MessageLengthSize = 2 /* Short */;

            if (totalData >= MessageLengthSize)
            {
                if (msgLen == 0)
                {
                    msgLen = BitConverter.ToInt16(readBuffer, 0);

                    if (msgLen <= MessageLengthSize || msgLen > 4096)
                    {
                        Kick("Client sent invalid network message (" + msgLen + " | " + readBuffer[processed + MessageLengthSize] + ")");
                        msgLen = 0;
                        return;
                    }
                }
                while (totalData >= msgLen + processed && msgLen > 0)
                {
                    /*
                    if (state == SlotState.PLAYER_AUTH && msgLen > 4
                        && (Packet) readBuffer[processed + 4] != Packet.PASSWORD_RESPONSE)
                    {
                        // put player packets aside until password response
						
                        if (sideBytes + msgLen > 4096)
                        {
                            Kick ("Player data too big.");
                            return;
                        }
						
                        if (sideBuffer == null) sideBuffer = new byte [4096];
						
                        Buffer.BlockCopy (readBuffer, processed, sideBuffer, sideBytes, msgLen);
						
                        sideBytes += msgLen;
                    }
                    else
                    */
                    {
                        //var slot = assignedSlot;
                        //if (slot >= 0)
                        tdsm.core.Messages.MessageDispatcher.Dispatch(this, readBuffer, processed + MessageLengthSize, msgLen - MessageLengthSize);
                        //else
                        //    return;

                        timeout = 0;

                        if (kicking) return;
                    }


                    processed += msgLen;
                    if (totalData - processed >= MessageLengthSize)
                    {
                        msgLen = BitConverter.ToInt16(readBuffer, processed);

                        if (msgLen <= MessageLengthSize || msgLen > 4096)
                        {
                            Kick("Client sent invalid network message (" + msgLen + " | " + readBuffer[processed + MessageLengthSize] + ")");
                            msgLen = 0;
                            return;
                        }
                    }
                    else
                    {
                        msgLen = 0;
                    }
                }
                if (processed == totalData)
                {
                    totalData = 0;
                }
                else if (processed > 0)
                {
                    Buffer.BlockCopy(readBuffer, processed, readBuffer, 0, totalData - processed);
                    totalData -= processed;
                }
            }
        }

        public void Kick(string reason, bool announce = true)
        {
            if (kicking) return;

            state = SlotState.KICK;

            if (announce)
            {
                if (assignedSlot >= 0)
                {
                    ProgramLog.Log("{0} @ {1}: disconnecting for: {2}", RemoteAddress, assignedSlot, reason);
                    var player = Main.player[assignedSlot];
                    if (player != null) player.DisconnectReason = reason;
                }
                else
                    ProgramLog.Log("{0}: disconnecting for: {1}", RemoteAddress, reason);
            }

            if (!kicking)
            {
                var msg = NewNetMessage.PrepareThreadInstance();
                msg.Disconnect(reason);
                KickAfter(msg.Output);
            }
        }

        public void SendSection(int x, int y)
        {
            Send(new Message { kind = 10, param = (x << 16) | (y & 0xffff) });
        }

        static Stack<NewNetMessage> sectionPool = new Stack<NewNetMessage>();
        static int sectionPoolCount = 0;

        static NewNetMessage TakeSectionBuffer()
        {
            lock (sectionPool)
            {
                if (sectionPool.Count > 0)
                    return sectionPool.Pop();
                sectionPoolCount += 1;
            }

            ProgramLog.Log("Section pool capacity: {0}", sectionPoolCount);
            return new NewNetMessage(272250);
        }

        static void FreeSectionBuffer(NewNetMessage buf)
        {
            buf.Clear();
            lock (sectionPool)
                sectionPool.Push(buf);
        }

        public void ResetTimeout()
        {
            timeout = 0;
        }

        static void TimeoutLoop()
        {
            var conns = new List<ClientConnection>();
            var msg = NewNetMessage.PrepareThreadInstance();

            while (true)
            {
                Thread.Sleep(5000);

                lock (All) conns.AddRange(All);

                foreach (var conn in conns)
                {
                    conn.timeout += 5;

                    if (conn.State == SlotState.QUEUED)
                    {
                        if (conn.timeout >= Main.timeOut / 2)
                        {
                            msg.Clear();
                            msg.SendTileLoading(1, SlotManager.WaitingMessage(conn));
                            conn.Send(msg.Output);
                            conn.timeout = 0;
                        }
                    }
                    else if (conn.timeout >= Main.timeOut)
                    {
                        try
                        {
                            conn.Kick("Timed out.");
                        }
                        catch (Exception e)
                        {
                            ProgramLog.Log("Exception in timeout thread: {0}", e);
                        }
                    }
                }

                conns.Clear();
            }
        }
    }
}