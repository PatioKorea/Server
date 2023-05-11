using System;
using System.IO;
using System.Xml;

namespace PacketGenerator
{
    class Program
    {
        static string genPackets;
        static ushort packetId; // 패킷개수 카운팅 변수 
        static string packetEnums;

        static string clientRegister; // 클라로 보내는 패킷
        static string serverRegister; // 서버로 보내는 패킷

        // args 를 외부의 bat파일에서 인자를 넘겨줄 수 있음 
        static void Main(string[] args)
        {
            // ../ : 찾는 경로에서 뒤로가기한 위치로 이동 
            string pdlPath = "../PDL.xml";

            XmlReaderSettings settings = new XmlReaderSettings()
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
            };

            //프로그램이 시작될때 인자로 넘겨주었다면 (1개 이상)
            if(args.Length >= 1)
            {
                pdlPath = args[0];
            }

            // using : 이 구문이 끝나면 자동으로 dispose(xml파일을 닫는다)가 실행된다 
            using (XmlReader r = XmlReader.Create(pdlPath, settings))
            {
                r.MoveToContent(); // <PDL>구문안으로 들어감

                while (r.Read()) // Read : 한줄씩 읽기 
                {
                    // r.NodeType == XmlNodeType.Element : 요소가 시작되는 시점 <packet>
                    if (r.Depth == 1 && r.NodeType == XmlNodeType.Element)
                        ParsePacket(r);
                    //Console.WriteLine(r.Name + " " + r["name"]);
                }

                string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);
                // 만든 템플릿을 하나의 파일로 만든다 (만들파일 , 자동화코드)
                File.WriteAllText("GenPacket.cs", fileText);

                string clientManagerText = string.Format(PacketFormat.managerFormat, clientRegister);
                File.WriteAllText("ClientPacketManager.cs", clientManagerText);
                string serverManagerText = string.Format(PacketFormat.managerFormat, serverRegister);
                File.WriteAllText("ServerPacketManager.cs", serverManagerText);
            }
        }

        public static void ParsePacket(XmlReader r)
        {
            // XmlNodeType.EndElement PDL바로 전 요소 <packet>
            if (r.NodeType == XmlNodeType.EndElement)
                return;

            // 소문자로 만든 요소의 이름이 packet이 아니면 버그발생 
            if (r.Name.ToLower() != "packet")
            {
                Console.WriteLine("Invalid packet node!!");
                return;
            }

            string packetName = r["name"];
            // packetName이 비어있는지 확인 
            if (string.IsNullOrEmpty(packetName))
            {
                Console.WriteLine("Packet without Name!!");
                return;
            }

            Tuple<string, string, string> t = ParseMembers(r);
            // (쓸 템플릿 코드변수, {0}, {1} ...계속 )
            genPackets += string.Format(PacketFormat.packetFormat,
                packetName, t.Item1, t.Item2, t.Item3);
            // 패킷이 추가 될때마다 패킷 열거형이 늘어난다 
            packetEnums += string.Format(PacketFormat.packetEnumFormat
                , packetName, ++packetId)
                + Environment.NewLine + "\t"; // 엔터키누른거 같이 정렬 

            // 패킷의 용도에 따른 분기점을 표시하는 규율으로 가른다 
            if (packetName.StartsWith("S_") || packetName.StartsWith("s_")) {
                clientRegister += string.Format(PacketFormat.managerRegisterFormat,
                    packetName)
                    + Environment.NewLine;
            }
            else {
                // PacketManager안 변수 추가 과정 
                serverRegister += string.Format(PacketFormat.managerRegisterFormat,
                    packetName)
                    + Environment.NewLine;
            }

        }

        // {1} 멤버 변수들 
        // {2} 멤버 변수 Read
        // {3} 멤버 변수 Write
        public static Tuple<string, string, string> ParseMembers(XmlReader r)
        {
            string packetName = r["name"];

            string memberCode = "";
            string readCode = "";
            string writeCode = "";

            int depth = r.Depth + 1; // packet범위 안으로 들어온 깊이 

            while (r.Read())
            {
                if (r.Depth != depth)
                    break;

                string memberName = r["name"];
                if (string.IsNullOrEmpty(memberName))
                {
                    Console.WriteLine("Member without name");
                    return null;
                }

                // ;다음에 들여쓰는 것을 구현한다 엔터누르는 것과 동일하다 
                if (string.IsNullOrEmpty(memberCode) == false)
                    memberCode += Environment.NewLine;
                if (string.IsNullOrEmpty(readCode) == false)
                    readCode += Environment.NewLine;
                if (string.IsNullOrEmpty(writeCode) == false)
                    writeCode += Environment.NewLine;

                //혹시 오타가 났을까봐 ToLower()사용
                // 변수는 이름 그래로가 크기이다
                // string과 list는 가변적인 크기를 가지고 있다 
                string memberType = r.Name.ToLower();
                switch (memberType)
                {
                    case "byte":
                    case "sbyte":
                        memberCode += string.Format(PacketFormat.memberFormat,
                            memberType, memberName);
                        readCode += string.Format(PacketFormat.readByteFormat,
                            memberName, memberType);
                        writeCode += string.Format(PacketFormat.writeByteFormat,
                             memberName, memberType);
                        break;
                    case "bool":
                    case "short":
                    case "ushort":
                    case "int":
                    case "long":
                    case "float":
                    case "double":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readFormat,
                            memberName, ToMemberType(memberType), memberType);
                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
                        break;
                    case "string":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readStringFormat,memberName);
                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
                        break;
                    case "list":
                        Tuple<string, string, string> t = ParseList(r);
                        memberCode += t.Item1;
                        readCode += t.Item2;
                        writeCode += t.Item3;
                        break;
                    default:
                        break;
                }
            }

            memberCode = memberCode.Replace("\n", "\n\t");
            readCode = readCode.Replace("\n", "\n\t\t");
            writeCode = writeCode.Replace("\n", "\n\t\t");

            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        public static Tuple<string, string, string> ParseList(XmlReader r)
        {
            string listName = r["name"];
            if(string.IsNullOrEmpty(listName))
            {
                Console.WriteLine("list without Name");
                return null;
            }

            Tuple<string, string, string> t = ParseMembers(r);

            string memberCode = string.Format(PacketFormat.memberListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName),
                t.Item1,
                t.Item2,
                t.Item3
                );

            string readCode = string.Format(PacketFormat.readListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName)
                );

            string writeCode = string.Format(PacketFormat.writeListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName)
                );

            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }


        // To변수형() 를 자동화하는 함수 
        public static string ToMemberType(string memberType)
        {
            // To변수형 함수를 자동화하는 분기점 
            switch (memberType)
            {
                case "bool":
                    return "ToBoolean";
                case "short":
                    return "ToInt16";
                case "ushort":
                    return "ToUInt16";
                case "int":
                    return "ToInt32";
                case "long":
                    return "ToInt64";
                case "float":
                    return "ToSingle";
                case "double":
                    return "ToDouble";
                default:
                    return "";
            }
        }


        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            // SubString(숫자 ) 입력된 숫자부터 나머지까지의 인덱스 클자들반환 
            return input[0].ToString().ToUpper() + input.Substring(1);
        }

        public static string FirstCharToLower(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            // SubString(숫자 ) 입력된 숫자부터 나머지까지의 인덱스 클자들반환 
            return input[0].ToString().ToLower() + input.Substring(1);
        }
    }
}
