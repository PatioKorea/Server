using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

// 멀티쓰레드 환경에 노출된 곳
// 항상 염두해두자 

namespace Server
{
    // JobQueue를 통해서 임무를 수행한다면
    // 클래스 전체속 활동하는 쓰레드는 
    // 단하나 라는 뜻 (IJobQueue를 상속받은 클래스)
    class GameRoom : IJobQueue
    {
        // List는 동시다발적인 멀티쓰레드 환경에서의 경우를 보장하지 못한다 
        // 게임룸에 접속한 사람들
        // 정확히는 사람들의 핸드폰과 연결된 식당 대리자의 휴대폰들
        List<ClientSession> _sessions = new List<ClientSession>(); 

        JobQueue _jobQueue = new JobQueue();

        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        public void Push(Action job)
        {
            _jobQueue.Push(job);
        }
        
        // 모은 패킷을 쏘는 함수
        public void Flush() 
        {
            //다른 유저에게 뿌린다
            foreach (ClientSession s in _sessions)
                s.Send(_pendingList);

            Console.WriteLine($"Flushed {_pendingList.Count} items");
            _pendingList.Clear();
        }

        public void Broadcast(ClientSession session, string chat)
        {
            S_Chat packet = new S_Chat();
            packet.playerId = session.SessionId;
            packet.chat = $"{chat} I am {packet.playerId}";
            ArraySegment<byte> segment = packet.Write();

            // 패킷 모으기 시작, 쌓기
            _pendingList.Add(segment);  
        }

        public void Enter(ClientSession session)
        {
            _sessions.Add(session);
            session.Room = this;
        }

        public void Leave(ClientSession session)
        {
            _sessions.Remove(session);
        }
    }
}
