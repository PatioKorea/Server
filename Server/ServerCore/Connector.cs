using System;
using System.Net;
using System.Net.Sockets;

// Session과 Listener 정독 후 이곳 독해 

namespace ServerCore
{
    // 유저 (클라이언트 ) 뿐만 아니라 서버와 서버끼리 연결을 하는 경우도 있다
    // 그리고 그때 Listener의 역할을 하는 객체와 Connect할 수 있는 기능을 가지고 있는 객체가 서로 통신할 수 있다
    // Connector <=> Connector X  |  Connect <=> Listener O

    public class Connector
    {
        Func<Session> _sessionFactory; // Session을 뱉어준다 

        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _sessionFactory = sessionFactory;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectComplete; // Async가 완료되었을때 실행하는 함수 
            args.RemoteEndPoint = endPoint;
            args.UserToken = socket; // 전역변수나 매개변수로 보내는 방식보다 args안에 소켓을 넣어놓을 수 있다

            RegisterConnect(args);
        }

        void RegisterConnect(SocketAsyncEventArgs args)
        {
            Socket socket = args.UserToken as Socket; // Object타입의 UserToken을 Socket으로 형변환(?)
            if (socket == null)
                return;

            bool pending = socket.ConnectAsync(args);
            if (pending == false)
                OnConnectComplete(null, args);
        }

        void OnConnectComplete(Object sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory.Invoke();
                //args.ConnectSocket : ConnectAsync를 성공적으로 마친 소켓 (연결된 소켓 ) 
                session.Start(args.ConnectSocket);
                session.OnConnected(args.RemoteEndPoint);
            }
            else
            {
                Console.WriteLine($"OnConnectCompleted Fail : {args.SocketError}");
            }
        }
    }
}
