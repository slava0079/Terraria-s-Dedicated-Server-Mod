﻿using System;

namespace Terraria_Server.Messages
{
    public class PlayerBallswingMessage : IMessage
    {
        public Packet GetPacket()
        {
            return Packet.PLAYER_BALLSWING;
        }

        public int? GetRequiredNetMode()
        {
            return null;
        }

        public void Process(int start, int length, int num, int whoAmI, byte[] readBuffer, byte bufferData)
        {
            int playerIndex = readBuffer[num++];

            if (Main.netMode == 2)
            {
                playerIndex = whoAmI;
            }

            float itemRotation = BitConverter.ToSingle(readBuffer, num);
            num += 4;
            int itemAnimation = (int)BitConverter.ToInt16(readBuffer, num);
            Main.player[playerIndex].itemRotation = itemRotation;
            Main.player[playerIndex].itemAnimation = itemAnimation;
            if (Main.netMode == 2)
            {
                NetMessage.SendData(41, -1, whoAmI, "", playerIndex);
            }
        }
    }
}
