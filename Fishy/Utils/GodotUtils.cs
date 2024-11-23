using System.Numerics;
using System.Text;

namespace Fishy.Utils
{
    enum GodotTypes
    {
        Null = 0,
        Bool = 1,
        Int = 2,
        Float = 3,
        String = 4,
        Vector2 = 5,
        Rect2 = 6,
        Vector3 = 7,
        Dictionary = 18,
        Array = 19
    }


    public class GodotUtilReader(byte[] data)
    {

        readonly BinaryReader _binaryReader = new(new MemoryStream(data), Encoding.UTF8);
        public byte[] data = data;

        public Dictionary<string, object> ReadPacket()
        {
            Dictionary<string, object> packet = [];

            try
            {
                int type = _binaryReader.ReadInt32();
                if (type != (int)GodotTypes.Dictionary)
                    return [];

                packet = ReadDictionary();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return packet;
        }

        private object? ReadNext()
        {
            int v = _binaryReader.ReadInt32();

            int type = v & 0xFFFF;
            int flags = v >> 16;

            return type switch
            {
                (int)GodotTypes.Null => null,
                (int)GodotTypes.Bool => ReadBool(),
                (int)GodotTypes.Int => ReadInt((flags & 1) == 1),
                (int)GodotTypes.Float => ReadFloat((flags & 1) == 1),
                (int)GodotTypes.String => ReadString(),
                (int)GodotTypes.Vector2 => ReadVector2(),
                (int)GodotTypes.Vector3 => ReadVector3(),
                (int)GodotTypes.Dictionary => ReadDictionary(),
                (int)GodotTypes.Array => ReadArray(),
                _ => null,
            };
        }

        private bool ReadBool()
            => _binaryReader.ReadInt32() != 0;

        private double ReadFloat(bool is64)
            => is64 ? _binaryReader.ReadDouble() : _binaryReader.ReadSingle();

        private Vector2 ReadVector2()
             => new(_binaryReader.ReadSingle(), _binaryReader.ReadSingle());

        private Vector3 ReadVector3()
            => new(_binaryReader.ReadSingle(), _binaryReader.ReadSingle(), _binaryReader.ReadSingle());

        private long ReadInt(bool is64)
            => is64 ? _binaryReader.ReadInt64() : _binaryReader.ReadInt32();

        private string ReadString()
        {
            int stringLength = _binaryReader.ReadInt32();
            char[] chars = _binaryReader.ReadChars(stringLength);

            if (4 - (int)_binaryReader.BaseStream.Position % 4 != 4)
                _binaryReader.ReadBytes(4 - (int)_binaryReader.BaseStream.Position % 4);

            return new string(chars);
        }

        private Dictionary<string, object> ReadDictionary()
        {
            Dictionary<string, object> dic = [];
            int elementCount = _binaryReader.ReadInt32() & 0x7FFFFFFF;
            for (int i = 0; i < elementCount; i++)
            {
                object? keyValue = ReadNext();
                string key;

                if (keyValue == null)
                    break;
                else
                    key = keyValue.ToString() ?? "Null";

                object? value = ReadNext() ?? "";
                dic[key] = value;
            }
            return dic;
        }

        private Dictionary<int, object> ReadArray()
        {
            Dictionary<int, object> array = [];

            int elementCount = _binaryReader.ReadInt32() & 0x7FFFFFFF;

            for (int i = 0; i < elementCount; i++)
                array[i] = ReadNext() ?? "";
                
            return array;
        }
    }



    public class GodotUtilWriter
    {
        readonly MemoryStream _memoryStream = new();
        readonly BinaryWriter _binaryWriter;

        public GodotUtilWriter()
        {
            _binaryWriter = new(_memoryStream);
        }

        public byte[] ConvertToGodotBytePacket(object packet)
        {
            Write(packet);
            return _memoryStream.ToArray();
        }

        public void Write(object packet)
        {
            switch (packet)
            {
                case null:
                    _binaryWriter.Write(0); break;
                case bool b:
                    WriteAsBool(b); break;
                case int i:
                    WriteAsInt(i); break;
                case long l:
                    WriteAsLong(l); break;
                case float f:
                    WriteAsFloat(f); break;
                case double d:
                    WriteAsDouble(d); break;
                case string s:
                    WriteAsString(s); break;
                case Vector3 v:
                    WriteAsVector3(v); break;
                case Dictionary<int, object> dictionaryArray:
                    WriteAsArray(dictionaryArray); break;
                case Dictionary<string, object> dictionary:
                    WriteAsDictionary(dictionary); break;
                default:
                    Console.WriteLine("Unknow Datatype while writing package!");
                    break;
            }
        }

        private void WriteAsBool(bool b)
        {
            _binaryWriter.Write((int)GodotTypes.Bool);
            _binaryWriter.Write(b ? 1 : 0);
        }

        public void WriteAsInt(int i)
        {
            _binaryWriter.Write((int)GodotTypes.Int);
            _binaryWriter.Write(i);
        }

        private void WriteAsLong(long l)
        {
            _binaryWriter.Write(65538);
            _binaryWriter.Write(l);
        }

        private void WriteAsFloat(float f)
        {
            _binaryWriter.Write((int)GodotTypes.Float);
            _binaryWriter.Write(f);
        }

        private void WriteAsDouble(double d)
        {
            _binaryWriter.Write(65539);
            _binaryWriter.Write(d);
        }

        private void WriteAsString(string s)
        {
            _binaryWriter.Write((int)GodotTypes.String);
            _binaryWriter.Write(s.Length);

            byte[] bytes = Encoding.UTF8.GetBytes(s);

            _binaryWriter.Write(bytes);

            int padding = (4 - bytes.Length % 4) % 4; // Calculate padding

            for (int i = 0; i < padding; i++)
                _binaryWriter.Write((byte)0);

        }

        private void WriteAsVector3(Vector3 v)
        {
            _binaryWriter.Write((int)GodotTypes.Vector3);
            _binaryWriter.Write(v.X);
            _binaryWriter.Write(v.Y);
            _binaryWriter.Write(v.Z);
        }

        private void WriteAsDictionary(Dictionary<string, object> packet)
        {
            _binaryWriter.Write((int)GodotTypes.Dictionary);
            _binaryWriter.Write(packet.Count);

            foreach (KeyValuePair<string, object> pair in packet)
            {
                Write(pair.Key);
                Write(pair.Value);
            }
        }

        private void WriteAsArray(Dictionary<int, object> packet)
        {
            _binaryWriter.Write((int)GodotTypes.Array);
            _binaryWriter.Write((int)packet.Count);

            for (int i = 0; i < packet.Count; i++)
                Write(packet[i]);
        }
    }
}
