using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SalvoClient
{
    public enum Operation
    {
        OPPONENTCONNECTED,
        OPPONENTHIT,
    }

    [Serializable]
    public class ClientPacket : ISerializable
    {
        private Operation _operation;
        private int _tileXNum;
        private int _tileYNum;

        public ClientPacket(Operation op, int tile_x, int tile_y)
        {
            _operation = op;
            _tileXNum = tile_x;
            _tileYNum = tile_y;
        }

        public ClientPacket(SerializationInfo si, StreamingContext sc)
        {
            _operation = (Operation)si.GetInt32("operation");
            _tileXNum = si.GetInt32("tile_x_num");
            _tileYNum = si.GetInt32("tile_y_num");
        }

        public virtual void GetObjectData(SerializationInfo si,
                              StreamingContext sc)
        {
            si.AddValue("operation", _operation);
            si.AddValue("tile_x_num", _tileXNum);
            si.AddValue("tile_y_num", _tileYNum);
        }

        public byte[] Serialize()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, this);
            return ms.ToArray();
        }

        public Object Deserialize(byte[] bt)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            ms.Write(bt, 0, bt.Length);
            ms.Position = 0;

            object obj = bf.Deserialize(ms);

            ms.Close();

            return obj;
        }
    }
}
