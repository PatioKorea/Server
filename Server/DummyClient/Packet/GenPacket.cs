using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServerCore;

public enum PacketID
{
    C_Chat = 1,
	S_Chat = 2,
	
}

interface IPacket
{
	ushort Protocol { get; }
	void Read(ArraySegment<byte> segment);
	ArraySegment<byte> Write();
};


class C_Chat : IPacket
{
    public string chat;

    public ushort Protocol { get { return (ushort)PacketID.C_Chat; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        count += sizeof(ushort);
        //string헤더 
		ushort chatLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		
		// Byte에서 string으로 변환
		this.chat = Encoding.Unicode.GetString(s.Slice(count, chatLen));
		count += chatLen;
		
    }

    public ArraySegment<byte> Write()
    {
        // 포인터 같이 Array를 받고 여기서 수정해도 Open된 Array가 수정되고 적용된다 
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0; // 바이트의 수를 체크하는 변수 (커서) 
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        // 강의 Serialization 1강 , 2강 , 3강 
        // TryWriteBytes : 2번째 인자값을 왼쪽 배열에 넣어준다 두번째 인자값은 자료형에 따라서 바이트의 수가 결정된다
        // Slice() span값자체가 잘라지지 않고 잘라낸 부분만 span타입으로 반환한다 
        count += sizeof(ushort);
        success &=
            BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.C_Chat);
        count += sizeof(ushort);

        ushort chatLen = (ushort)Encoding.Unicode.GetBytes
		    (this.chat, 0, this.chat.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), chatLen);
		count += sizeof(ushort);
		count += chatLen;
		
            
        // count 데이터 밀어넣기 
        success &=
            BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

class S_Chat : IPacket
{
    public int playerId;
	public string chat;

    public ushort Protocol { get { return (ushort)PacketID.S_Chat; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        count += sizeof(ushort);
        this.playerId = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		count += sizeof(int);
		
		//string헤더 
		ushort chatLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		
		// Byte에서 string으로 변환
		this.chat = Encoding.Unicode.GetString(s.Slice(count, chatLen));
		count += chatLen;
		
    }

    public ArraySegment<byte> Write()
    {
        // 포인터 같이 Array를 받고 여기서 수정해도 Open된 Array가 수정되고 적용된다 
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0; // 바이트의 수를 체크하는 변수 (커서) 
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        // 강의 Serialization 1강 , 2강 , 3강 
        // TryWriteBytes : 2번째 인자값을 왼쪽 배열에 넣어준다 두번째 인자값은 자료형에 따라서 바이트의 수가 결정된다
        // Slice() span값자체가 잘라지지 않고 잘라낸 부분만 span타입으로 반환한다 
        count += sizeof(ushort);
        success &=
            BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.S_Chat);
        count += sizeof(ushort);

        success &=
		    BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
		count += sizeof(int);
		
		ushort chatLen = (ushort)Encoding.Unicode.GetBytes
		    (this.chat, 0, this.chat.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), chatLen);
		count += sizeof(ushort);
		count += chatLen;
		
            
        // count 데이터 밀어넣기 
        success &=
            BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}


