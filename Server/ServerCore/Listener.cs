using System;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    // Script Info : 손님과 연결할 수 있는 문지기를 생성한다 

    public class Listener
    {
        Socket _listenSocket;
        //Action<Socket> _onAcceptHandler; // 모든 작업이 끝나고 콜백되는 이벤트
        // 바로위에 코드와 다른 점은 Action은 받는 인자가 있고 Func는 뱉어주는 인자가 있다 
        Func<Session> _sessionFactory; // Session을 뱉어준다 

        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory += sessionFactory; // 모든 작업이 끝남을 알려주는 이벤트 등록

            _listenSocket.Bind(endPoint); // 문지기 교육
            _listenSocket.Listen(10);// backlog -> 10 : 최대 대기수

            // pending이 참일때 이벤트를 걸어놓는다
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            // 미뤄둔 Accept가 실행되었다면 델리게이트에 함수를 실행시킨다 ( 이벤트 발생 )
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptComplete);
            // 첫 번째로 들어올 정보를 Accept하기위해서 미리 이벤트를 걸어둔다 , 미리 등록해놓는다
            ResisterAccept(args);
        }

        // Accept의 동작을 미뤄놓는다 ( 등록 해놓는다 )
        void ResisterAccept(SocketAsyncEventArgs args)
        {
            // 초기화와 동시에 소켓 재사용을 막는다
            args.AcceptSocket = null;

            // pending : 동작을 미루어 지는 것을 예상했지만 운이 좋게도 바로 Accept가 되었을때 참을 뱉어낸다
            bool pending = _listenSocket.AcceptAsync(args);
            if (pending == false)
            {
                OnAcceptComplete(null, args);
            }
            // else일 경우에는 args.Completed에 있는 이벤트가 나중에 라도 넘어가게 해준다
        }

        // 미뤄놓은 Accept의 동작을 처리해준다
        void OnAcceptComplete(Object sender, SocketAsyncEventArgs args) // 이벤트를 걸때 필요한 매개변수 형식을 맞춘다
        {
            // 소켓의 오류가 났는가
            if (args.SocketError == SocketError.Success)
            {// 오류가 없이 정상적일때

                // 컨텐츠단에서 처리하는 것이 아닌 엔진쪽에서 처리한다 
                Session session = _sessionFactory.Invoke(); // Func의 Invoke( 이벤트실행 )은 <T>값을 뱉어준다 
                session.Start(args.AcceptSocket);
                session.OnConnected(args.AcceptSocket.RemoteEndPoint); // 아래 이벤트핸들러의 기능을 대신한다 

                // 이벤트 핸들러를 이용해서 원래의 Accept함수처럼 Socket을 뱉어준다
                // 하지만 이벤트 핸들러 속 Socket을 초기화하지않으면 아래의 소켓이 재사용 될 수 있음을 알아야 한다
                //  _onAcceptHandler.Invoke(args.AcceptSocket);
            }
            else
                Console.WriteLine(args.SocketError.ToString());

            // Accept의 모든 처리가 끝났으므로 다음 Accept의 동작을 미리 걸어둔다 (낚시대를 다시 걸어둔다 )
            ResisterAccept(args);
        }
    }
}
