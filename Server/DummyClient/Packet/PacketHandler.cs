using DummyClient;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

// ClientSession에서 실행 => PacketManager에서 받아서 데이터조회 => 도착
class PacketHandler
{
    public static void S_ChatHandler(PacketSession session, IPacket packet)
    {
        S_Chat chatPacket = packet as S_Chat;
        ServerSession serverSession = session as ServerSession;

        //if(chatPacket.playerId == 1)
            //Console.WriteLine(chatPacket.chat);
    }
}
