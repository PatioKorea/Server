using System;
using System.Threading;

namespace ServerCore
{
    public class SendBufferHelper
    {
        // 버퍼를 재 사용을 하지않고 새롭게 만들어서 나의 쓰레드에 부여한다 
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });

        // 기본 버퍼생성 크기 ( 변경가능 )
        public static int ChunkSize { get; set; } = 65535 * 100;

        // reserveSize : 버퍼를 사용할 최대치의 크기를 입력받고 현재 사용가능한지 판단 
        public static ArraySegment<byte> Open(int reserveSize)
        {
            // 한번도 사용을 안했다면 버퍼를 새로만듬 
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            // 버퍼의 수명을 다했을때 ( 데이터를 넣고싶은 크기가 버퍼에서 못담는다 ) 다시 만든다 
            if (CurrentBuffer.Value.FreeSize < reserveSize)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            // 버퍼를 뱉어준다 , Value.Open()은 SendBuffer안에 있는 함수 
            return CurrentBuffer.Value.Open(reserveSize);
        }

        // 
        public static ArraySegment<byte> Close(int realUsedSize)
        {
            // 버퍼를 뱉어준다 , Value.Close()은 SendBuffer안에 있는 함수 
            return CurrentBuffer.Value.Close(realUsedSize);
        }
    }

    public class SendBuffer
    {
        //[u] [] [] [] [] [] []
        byte[] _buffer;
        int _usedSize = 0;

        public int FreeSize { get { return _buffer.Length - _usedSize; } }

        // chunk : 굉장히 큰 뭉텡이 
        public SendBuffer(int getChunkSize)
        {
            _buffer = new byte[getChunkSize];
        }

        // reserveSize : 버퍼를 사용할 최대치의 크기를 입력받고 현재 사용가능한지 판단 
        public ArraySegment<byte> Open(int reserveSize)
        {
            //if(reserveSize > FreeSize)
            //    return null;

            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
        }

        // 
        public ArraySegment<byte> Close(int realUsedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, realUsedSize);
            _usedSize += realUsedSize;
            return segment;
        }
    }
}
