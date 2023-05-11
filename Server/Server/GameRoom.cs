using System;
using System.Collections.Generic;
using System.Text;

// 멀티쓰레드 환경에 노출된 곳
// 항상 염두해두자 

namespace Server
{
    class GameRoom
    {
        // List는 동시다발적인 멀티쓰레드 환경에서의 경우를 보장하지 못한다 
        List<ClientSession> _sessions = new List<ClientSession>();
        object _lock = new object();

        public void Broadcast(ClientSession session, string chat)
        {
            S_Chat packet = new S_Chat();
            packet.playerId = session.SessionId;
            packet.chat = $"{chat} /  I am {packet.playerId}";
            ArraySegment<byte> segment = packet.Write();

            // _sessions는 멀티쓰레드 고위험 지역
            lock (_lock)
            {
                //다른 유저에게 뿌린다
                foreach (ClientSession s in _sessions)
                    s.Send(segment);
            }
        }

        public void Enter(ClientSession session)
        {
            lock (_lock)
            {
                _sessions.Add(session);
                session.Room = this;
            }
        }

        public void Leave(ClientSession session)
        {
            lock (_lock)
            {
                _sessions.Remove(session);
            }
        }
    }
}
