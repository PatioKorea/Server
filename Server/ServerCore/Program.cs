using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
    class Program
    {
        static Listener _listener = new Listener();

        static void OnAcceptHandler(Socket clientSocket)
        {
            try
            {
                byte[] recvBuff = new byte[1024];
                int recvBytes = clientSocket.Receive(recvBuff);
                string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                Console.WriteLine($"클라이언트(유저)로 부터 온 메세지 : {recvData}");

                byte[] sendBuff = Encoding.UTF8.GetBytes("서버에 접속하셨습니다 !!");
                clientSocket.Send(sendBuff);

                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main(string[] args)
        {
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            // 누가 서버에 들어왔을때 OnAcceptHandler 라는 얘로 알림( 함수호출 )을 받는다
            _listener.Init(endPoint, OnAcceptHandler);

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