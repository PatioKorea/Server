using System;
namespace PacketGenerator
{
    // @"" 여러줄 string을 쓸 수 있음
    // 가변적인 변수같은거는 {번호}로 써놓으면 된다
    // { : 소괄호 같은건 {를 한번더 붙여 줘야 한다 


    // {0} enum값 / 패킷이름/패킷번호 
    // {1} 모든 패킷 클래스 코드 / 패킷목록 
    class PacketFormat
    {
        public static string fileFormat =
@"using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServerCore;

public enum PacketID
{{
    {0}
}}

{1}
";

        // {0} 패킷 이름
        // {1} 패킷 번호 
        public static string packetEnumFormat =
@"{0} = {1},";


        // {0} 패킷 이름
        // {1} 멤버 변수들 
        // {2} 멤버 변수 Read
        // {3} 멤버 변수 Write
        public static string packetFormat =
@"
class {0}
{{
    {1}

    public void Read(ArraySegment<byte> segment)
    {{
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        count += sizeof(ushort);
        {2}
    }}

    public ArraySegment<byte> Write()
    {{
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
            BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.{0});
        count += sizeof(ushort);

        {3}
            
        // count 데이터 밀어넣기 
        success &=
            BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }}
}}
";

        // {0} 변수 형식
        // {1} 변수 이름 
        public static string memberFormat =
@"public {0} {1};";

        // {0} 리스트 이름 [대문자] 구조체 이름 
        // {1} 리스트 이름 [소문자]
        // 구조체가 가지고 있는 데이터변수들
        //   {2} 멤버 변수들 
        //   {3} 멤버 변수 Read
        //   {4} 멤버 변수 Write
        // 원래는 struct 이중 리스트를 지원하고 싶으면 class 
        public static string memberListFormat =
@"
public class {0}
{{
    {2}

    public void Read(ReadOnlySpan<byte> s, ref ushort count)
    {{
        // ToInt32 : int , ToInt16 : short , ToSingle : float , ToDouble : double
        {3}
    }}

    public bool write(Span<byte> s, ref ushort count)
    {{
        bool success = true;
        {4}
        
        return success;
    }}
}}

// 구조체를 가지고 있는 배열 
public List<{0}> {1}s = new List<{0}>();
";

        // {0} 변수 이름
        // {1} To- 변수형식
        // {2} 변수 형식 = sizeOf({2}}
        public static string readFormat =
@"this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
count += sizeof({2});
";

        // {0} 변수 이름 
        // {1} 변수 형식 
        public static string readByteFormat =
@"this.{0} = ({1})segment.Array[segment.Offset + count];
count += sizeof({1});
";


        // {0} 변수 이름 
        public static string readStringFormat =
@"//string헤더 
ushort {0}Len = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);

// Byte에서 string으로 변환
this.{0} = Encoding.Unicode.GetString(s.Slice(count, {0}Len));
count += {0}Len;
";


        // {0} 리스트 이름 [대문자] 구조체 이름 
        // {1} 리스트 이름 [소문자]
        public static string readListFormat =
@"//List 읽어들이기 
this.{1}s.Clear(); // 혹시모를 상황이 있을까봐

ushort {1}Len = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);

// 루프를 돌면서 객체를 List에 밀어넣는다 나중에 List데이터를 참조해서 사용할수 있음 
for(int i = 0; i < {1}Len; i++)
{{
    {0} {1} = new {0}();
    {1}.Read(s, ref count);
    {1}s.Add({1});
}}";


        // {0} 변수 이름
        // {1} 변수 형식 
        public static string writeFormat =
@"success &=
    BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.{0});
count += sizeof({1});
";


        // {0} 변수 이름 
        // {1} 변수 형식 
        public static string writeByteFormat =
@"segment.Array[segment.Offset + count] = (byte)this.{0};
count += sizeof({1});
";


        // {0} 변수 이름
        public static string writeStringFormat =
@"ushort {0}Len = (ushort)Encoding.Unicode.GetBytes
    (this.{0}, 0, this.{0}.Length, segment.Array, segment.Offset + count + sizeof(ushort));
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Len);
count += sizeof(ushort);
count += {0}Len;
";

        // {0} 리스트 이름 [대문자] 구조체 이름 
        // {1} 리스트 이름 [소문자]
        public static string writeListFormat =
@"success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort){1}s.Count);
count += sizeof(ushort);

foreach ({0} {1} in {1}s)
{{
    // 리스트를 하나씩 밀어넣어준다 구조체 안에 함수를 이용함 
    success &= {1}.write(s, ref count);
}}";
    }
}
