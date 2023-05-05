using System;
namespace ServerCore
{
    // [_readPos] [] [_writePos] [] [] [] [] [] [] [] []
    public class RecvBuffer
    {
        ArraySegment<byte> _buffer;
        //커서의 개념
        // 써져 있는 것을 하나 씩 읽는 커서 / 컨텐츠단에서 데이터를 판단할때 작동함
        // 만약 모든 패킷이 도착하지 않았을때 기다린다 => 패킷이 완성되면 그때부터 움직인다 
        int _readPos;
        // 쓰면서 뒤로 밀려나는 커서 / 데이터를 받을때 작동함 
        int _writePos; 

        public RecvBuffer(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        // 처리되지 않은 데이터들 ( 읽히지 않은 데이터들 , 아직 패킷이 완성된 상태가 아닐 수 있음 
        public int DataSize { get { return _writePos - _readPos; } }
        // 남은 버퍼의 크기 , 데이터를 얼마나 더 쓸 수 있는지에 대한 수 
        public int FreeSize { get { return _buffer.Count - _writePos; } }

        // _buffer.Offset : Array의 시작위치 무조건 맨 처음 
        // 실제 처리해야되는 데이터들 ( DataSize부분 ) 을 넘겨준다 
        public ArraySegment<byte> ReadSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
        }

        // 실제 받을 수 있는 데이터의 남은 크기만큼 ArraySegment로 넘겨준다 
        public ArraySegment<byte> WriteSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); } 
        }

        // 커서들의 위치가 버퍼끝까지 밀려나가서 처리에 문제가 생기는것을 방지한다
        // 커서들의 위치를 처음으로 재 조정 시킨다 
        public void Clean()
        {
            int dataSize = this.DataSize;
            if(dataSize == 0)
            {
                // 남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋 
                _readPos = _writePos = 0;
            }
            else
            {
                // 남은 찌끄레기가 있으면 시작 위치로 커서들을 복사
                //Copy( 복사할 버퍼 , 복사를 시작할 위치 , 받을 버퍼 , 데이터를 받을 버퍼의 위치 , 복사를 시작할 위치에서 얼만큼 복사할 값 )
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
                _readPos = 0;
                _writePos = dataSize;
            }
        }

        // read커서를 따라서 잘 처리가되었는지에 대한 여부 
        public bool OnRead(int numOfBytes)
        {
            // 쓰지도 않은 데이터를 읽었다고 보고한 말도 안되는 상황 ㅋㅋ 
            if (numOfBytes > DataSize)
                return false;

            _readPos += numOfBytes;
            return true;
        }

        // 클라이언트 ( 유저 )가 데이터를 보낸걸 받았을때 Write커서를 이동시킨다 
        public bool OnWrite(int numOfBytes)
        {
            // 선언한 데이터양보다 더 많은 양을 써버림ㅋㅋ 4차원의 세계로 진출~ 
            if (numOfBytes > FreeSize)
                return false;

            _writePos += numOfBytes;
            return true;
        }
    }
}
