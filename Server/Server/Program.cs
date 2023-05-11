using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerCore;

namespace Server
{
    class Program
    {
        static Listener _listener = new Listener();
        public static GameRoom Room = new GameRoom();

        static void Main(string[] args)
        {

            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            // 누가 서버에 들어왔을때 OnAcceptHandler 라는 얘로 알림( 함수호출 )을 받는다
            // 변경 : Func<>로 GameSession을 받는다 ( 객체생성과 함께 )
            _listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });

            Console.WriteLine("서버 기다리는 중...");

            // 위에 코드가 아래의 역할을 하기 때문에 아래는 프로그램이 꺼지지않도록 방어하는 역할
            while (true)
            {
                //Socket clientSocket = _listener.Accept();
                ;
            }

        }
    }
}
