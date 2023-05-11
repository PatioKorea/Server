using Server;
using ServerCore;
using System;
using System.Collections.Generic;

// ClientSession에서 실행 => PacketManager에서 받아서 데이터조회 => 도착
class PacketHandler
{
    public static void C_ChatHandler(PacketSession session, IPacket packet)
    {
        C_Chat chatPacket = packet as C_Chat;
        ClientSession clientSession = session as ClientSession;
        if (clientSession.Room == null)
            return;

        // 채팅을 보낸 한 채팅을 나머지 유저에게 뿌린다 
        clientSession.Room.Broadcast(clientSession, chatPacket.chat);
    }
}
