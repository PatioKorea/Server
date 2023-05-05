﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// Listener 정독 후 이곳 독해 

namespace ServerCore
{
    public abstract class Session
    {
        Socket _socket;
        int _disconnected = 0;

        RecvBuffer _recvBuffer = new RecvBuffer(1024);

        Object _lock = new object();
        Queue<byte[]> _sendQueue = new Queue<byte[]>();
        // Send의 과정중에 처리가 끝나지 않았다면 Queue에 쌓아둘지 아닐지 판단하는 변수 
        // 보류중인 데이터 리스트들 ( Queue에 쌓아두었던 버퍼를 List로 뭉치는 변수 )
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        // 게임코어단에서는 직접사용하지 않고 다른 곳에서 빼서 써야 하기 때문에 public함수를 선언해준다
        // 추상함수로써 Session을 상속받은 얘를 컨텐츠단에서 사용하게 된다
        // ** 이름그대로의 기능을 실행하는 것이 아닌 실행되었을때 하고 싶은 행동들을 하는 함수들 ( 콜백느낌 ) **
        public abstract void OnConnected(EndPoint endPoint);
        public abstract int  OnRecv(ArraySegment<byte> buffer); // 데이터를 받은 양을 뱉음 
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);


        // 원래는 Init 이었지만 접속을 시작하는 느낌이라 Start로 바꿈 
        public void Start(Socket socket)
        {
            _socket = socket;

            // 다른곳에서 할 필요 없다 이곳에서 함 
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            ResisterRecv();
        }

        // Recv처럼 올때까지 계속기다리는 것이 아닌 보내고 싶을때만 호출하는 방식 
        public void Send(byte[] sendBuffer)
        {
            lock (_lock)
            {
                // 보낼 버퍼를 Queue에 삽입 
                _sendQueue.Enqueue(sendBuffer);
                if (_pendingList.Count == 0)
                {
                    //내가 처음으로 Send를 호출했을때 
                    ResisterSend();
                }
            }

            //_socket.Send(sendBuffer);

            // _sendArgs를 재사용하는것은 좋지만 다른 쓰레드가 이 Args를 다시 덮어쓰게 되면 문제가 발생한다 
            //_sendArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            //ResisterSend();
        }

        // 쫒아내기 
        public void Disconnect()
        {
            // 이 함수를 두번 동시에 실행시킨다면 크래시가 나기 때문에 아래 구문을 넣는다
            // 멀티쓰레드 환경에서 발생할 문제이기에 Interlocked로 원자성을 고려한 함수를 쓴다 
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            // 추상 함수 
            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        # region 네트워크 통신 

        void ResisterSend()
        {
            // 다른 쓰레드가 Send()에서 호출될수 있기 때문

            // 한번 더 Send에 대한 최적화 
            // Queue 에 저장하기 보다 Args안에 버퍼리스트를 활용해서 되도록 한번에 처리하도록한다 ( 뭉친다 )
            while(_sendQueue.Count > 0)
            {
                byte[] buff = _sendQueue.Dequeue(); // 넣어놓았던 큐의 처리
                // 아래의 리스트들은 SendAsync가 모두 처리되도록 한다 
                _pendingList.Add(new ArraySegment<byte>(buff, 0, buff.Length));
            }

            _sendArgs.BufferList = _pendingList;

            // 이벤트핸들러를 재사용하기 위해 매개변수로 받지않고 지역변수 _sendArgs를 사용한다 
            bool pending = _socket.SendAsync(_sendArgs);
            if(pending == false)
            {
                OnSendCompleted(null, _sendArgs);
            }
        }

        void OnSendCompleted(Object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // 모든 처리가 완료되고 남은 데이터를 보냈기 때문에 초기화 해준다 
                    _sendArgs.BufferList = null;
                    _pendingList.Clear();

                    // 아래줄 코드의 기능을 대신하는 추상 함수 
                    OnSend(_sendArgs.BytesTransferred);
                    //Console.WriteLine($"Transferred bytes : {_sendArgs.BytesTransferred}");

                    if (_sendQueue.Count > 0)
                    {
                        //예약하는 동안에 처리가 안된 큐들을 처리 (쌓였던 큐의 실질적인 처리) 
                        ResisterSend();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnSendCompleted Failed {e}");
                }
            }
            else
            {
                Disconnect();
            }
        }

        void ResisterRecv()
        {
            _recvBuffer.Clean(); // 버그발생 차단 
            // 받을 수 있는 범위만큼 _recvBuffer에서 Array를 새로 만들어주었기 때문에
            // SetBuffer로 넘겨주기만 한다 
            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (pending == false)
            {
                OnRecvCompleted(null, _recvArgs);
            }
        }

        void OnRecvCompleted(Object sender, SocketAsyncEventArgs args)
        {
            lock (_lock) // 이벤트 핸들러에서 멀티쓰레드가 동시에 이 함수호출할수 있기 때문
            {
                // 가끔 받은 내용물이 0바이트가 올수도 있기 때문에 && 소켓에러가 나지 않았을때 
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        // Write 커서 이동 / if문 조건에 있어도 OnWrite함수는 실행됨 
                        if(_recvBuffer.OnWrite(args.BytesTransferred) == false)
                        {
                            // 버그발생 근데 거의도 아니고 99.9%발생하지 않음 
                            Disconnect();
                            return;
                        }


                        // OnRecv : 추상 함수 ( recv한것을 알려준다 ) 이 문단 아래의 기능을 수행한다
                        // 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다 
                        int processLen = OnRecv(_recvBuffer.ReadSegment);
                        if(processLen < 0 || _recvBuffer.DataSize < processLen)
                        {
                            // 버그발생 
                            Disconnect();
                            return;
                        }
                        //string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
                        //Console.WriteLine($"클라이언트(유저)로 부터 온 메세지 : {recvData}");

                        // Read 커서 이동 / if문 조건에 있어도 OnRead함수는 실행됨 
                        if (_recvBuffer.OnRead(processLen) == false)
                        {
                            // 버그발생 
                            Disconnect();
                            return;
                        }

                        ResisterRecv();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnRecvCompleted Failed {e}");
                    }
                }
                else // 호출을 끊는다 
                {
                    Disconnect();
                }
            }
        }

        #endregion
    }
}