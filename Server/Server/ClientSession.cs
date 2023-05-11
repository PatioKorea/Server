using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerCore;

namespace Server
{
    class ClientSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            //Packet packet = new Packet() { size = 100, packetId = 10 };

            //ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            //byte[] buffer = BitConverter.GetBytes(packet.size);
            //byte[] buffer2 = BitConverter.GetBytes(packet.packetId);
            //Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
            //Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
            //ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);
            //byte[] sendBuff = Encoding.UTF8.GetBytes("서버에 접속하셨습니다 !!");

            //Send(sendBuff);

            // 위 작업은 ServerSession과 ClientSession에서 처리한다 

            Thread.Sleep(5000);
            Disconnect();
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            ushort count = 0;

            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += 2;
            ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            switch ((PacketID)id)
            {
                case PacketID.PlayerInfoReq:
                    {
                        PlayerInfoReq req = new PlayerInfoReq();
                        req.Read(buffer);

                        //long playerId = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
                        //count += 8;
                        Console.WriteLine($"PlayerInfoReq : {req.playerId} , {req.name}");

                        foreach (PlayerInfoReq.Skill skill in req.skills)
                        {
                            Console.WriteLine($"Skill : {skill.id} , {skill.level} , {skill.duration}");
                        }
                    }
                    break;
            }

            Console.WriteLine($"RecvPacket [ Size : {size} , Id : {id} ]");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes : {numOfBytes}");
        }
    }
}
