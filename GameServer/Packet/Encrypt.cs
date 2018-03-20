using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Packet
{
    class Encrypt
    {
        //static Random random = new Random(DateTime.Now.Millisecond);
        static readonly int _ADD_BYTES = 2;
        static readonly int _MIN_BYTES = 2;// do not change it!
        public static int MAX_ADD_BYTES { get { return (_ADD_BYTES + _MIN_BYTES); } }

        public static byte[] NewPacket(byte[] data, byte header, byte[] key, int keyOffset, Client.ENCODE_TYPE encodeType)
        {
            if (key == null || data == null)
            {
                Output.WriteLine("Encrypt::NewPacket - key or data is empty return untouched");
                return data;
            }
            UInt16 addLength = (UInt16)(Program.random.Next(_ADD_BYTES) + _MIN_BYTES);
            if ((UInt64)(data.Length + Program.sendHeaderLength + Program.sendPrefixLength + addLength) >= UInt32.MaxValue)
            {
                Output.WriteLine("Encrypt::NewPacket - Packet size too large - can't send");
                return null;
            }
            byte[] Out = new byte[Program.sendPrefixLength + Program.sendHeaderLength + data.Length + addLength];
            UInt16 realLength = (UInt16)(data.Length + Program.sendPrefixLength + Program.sendHeaderLength);
            UInt16 finalLength = (UInt16)(data.Length + Program.sendPrefixLength + Program.sendHeaderLength + addLength);
            byte[] lReal = new byte[2];
            byte[] lFinal = new byte[2];
            lReal = BitConverter.GetBytes(realLength);
            lFinal = BitConverter.GetBytes(finalLength);
            byte head = header;
            Out[0] = lFinal[1];
            Out[1] = lReal[0];
            Out[2] = head;
            Out[3] = lFinal[0];
            Out[4] = lReal[1];
            byte[] tmp = new byte[1];
            for (int i = 0; i < addLength; i++)
            {
                Program.random.NextBytes(tmp);
                Out[realLength + i] = tmp[0];
            }
            data.CopyTo(Out, 5);
            switch (encodeType)
            {
                case Client.ENCODE_TYPE.AES:
                    break;
                case Client.ENCODE_TYPE.BXO:
                    break;
                case Client.ENCODE_TYPE.COD:
                    Out = Crypt.Coder.EncodeBuffer(Out, key[0]);
                    break;
                case Client.ENCODE_TYPE.XOR:
                    Out = Crypt.Xor.Encrypt(Out, key, keyOffset);
                    break;
                default:
                    Out = Crypt.Xor.Encrypt(Out, key, keyOffset);
                    break;
            }
            if (Program.DEBUG_Encrypt) Output.WriteLine("Encrypt::NewPacket  KeyOffset: " + keyOffset.ToString() + " Recv real length: " + BitConverter.ToUInt16(lReal, 0).ToString() + " final length: " + BitConverter.ToUInt16(lFinal, 0).ToString());
            return Out;
        } 
    }
}
