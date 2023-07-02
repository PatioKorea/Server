using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    // 유저들과 연결된 식당대리자의 휴대폰들을 관리하는 스크립트
    // 생성되는 모든 Client세션들은 여기에 저장되고 관리한다.
    class SessionManager
    {
        static SessionManager _session = new SessionManager();
        public static SessionManager Instance { get { return _session; } }

        int _sessionId = 0;
        Dictionary<int, ClientSession> _sessions = new Dictionary<int, ClientSession>();
        object _lock = new object();

        // 휴대폰에게 아이디를 발급하고 _sessions에 넣는다
        public ClientSession Generate()
        {
            lock (_lock)
            {
                int sessionId = ++_sessionId; // Session발급번호

                ClientSession session = new ClientSession();
                session.SessionId = sessionId;
                _sessions.Add(sessionId, session);

                Console.WriteLine($"Connect : {sessionId}");

                return session;
            }
        }

        public ClientSession Find(int id)
        {
            lock (_lock)
            {
                ClientSession session = null;
                _sessions.TryGetValue(id, out session);
                return session;
            }
        }

        // 넘겨온 클라세션을 관리명단에서 제외시킨다.
        public void Remove(ClientSession session)
        {
            lock (_lock)
            {
                _sessions.Remove(session.SessionId);
            }
        }

    }
}
