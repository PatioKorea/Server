﻿using System;

//public abstract class Packet
//{
//    // ushort : 2bite / int : 4bite MMO 서버에선 엄청난 크기 차이 
//    public ushort size;
//    public ushort packetId;

//    public abstract ArraySegment<byte> Write();
//    public abstract void Read(ArraySegment<byte> s);
//}

class PlayerInfoReq
{
    public long playerId;
    public string name;

    public struct Skill
    {
        public int id;
        public short level;
        public float duration;

        public bool write(Span<byte> s, ref ushort p_count)
        {
            bool success = true;
            success &=
                BitConverter.TryWriteBytes(s.Slice(p_count, s.Length - p_count), id);
            p_count += sizeof(int);
            success &=
                BitConverter.TryWriteBytes(s.Slice(p_count, s.Length - p_count), level);
            p_count += sizeof(short);
            success &=
                BitConverter.TryWriteBytes(s.Slice(p_count, s.Length - p_count), duration);
            p_count += sizeof(float);

            return success;
        }

        public void Read(ReadOnlySpan<byte> s, ref ushort p_count)
        {
            // ToInt32 : int , ToInt16 : short , ToSingle : float , ToDouble : double
            id = BitConverter.ToInt32(s.Slice(p_count, s.Length - p_count));
            p_count += sizeof(int);

            level = BitConverter.ToInt16(s.Slice(p_count, s.Length - p_count));
            p_count += sizeof(short);

            duration = BitConverter.ToSingle(s.Slice(p_count, s.Length - p_count));
            p_count += sizeof(float);
        }
    }

    // 구조체를 가지고 있는 배열 
    public List<Skill> skills = new List<Skill>();

    //public PlayerInfoReq()
    //{
    //    this.packetId = (ushort)PacketID.PlayerInfoReq;
    //}

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        count += sizeof(ushort);
        this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
        count += sizeof(long);

        //string헤더 
        ushort nameLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
        count += sizeof(ushort);
        // Byte에서 string으로 변환
        this.name = Encoding.Unicode.GetString(s.Slice(count, nameLen));
        count += nameLen;

        //skill List 읽어들이기 
        skills.Clear(); // 혹시모를 상황이 있을까봐 
        ushort skillLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
        count += sizeof(ushort);

        // 루프를 돌면서 skill객체를 List에 밀어넣는다 나중에 List데이터를 참조해서 사용할수 있음 
        for (int i = 0; i < skillLen; i++)
        {
            Skill skill = new Skill();
            skill.Read(s, ref count);
            skills.Add(skill);
        }
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
            BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.PlayerInfoReq);
        count += sizeof(ushort);
        success &=
            BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
        count += sizeof(long);

        #region 비효율적 코드 예시 
        //// 1. string len [2] string을 길이 2만큼으로 보낼거야
        //ushort nameLen = (ushort)Encoding.Unicode.GetByteCount(this.name);
        //success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
        //count += sizeof(ushort);

        //// 2. byte[] 실제 string데이터 
        //Array.Copy(Encoding.Unicode.GetBytes(this.name), 0, segment.Array, count, nameLen);
        //count += nameLen;
        #endregion

        //위에 코드 모다 아래가 더 효율적이다 
        // this.name을 0부터 Length까지 segment.Array에 갱신부분까지 밀어넣는다
        // ( 실제데이터 밀어넣기 + nameLen넣을 공간 마련하기 )
        ushort nameLen = (ushort)Encoding.Unicode.GetBytes
            (this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
        count += sizeof(ushort);
        count += nameLen;

        //SkillInfo list
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)skills.Count);
        count += sizeof(ushort);
        foreach (Skill skill in skills)
        {
            // 리스트를 하나씩 밀어넣어준다 구조체 안에 함수를 이용함 
            success &= skill.write(s, ref count);
        }

        // count 데이터 밀어넣기 
        success &=
            BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public enum PacketID
{
    PlayerInfoReq = 1,
    playerInfoOk = 2,
}