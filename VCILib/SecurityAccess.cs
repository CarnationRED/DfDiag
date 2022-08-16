using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace VCILib
{
    public class SecurityAccess
    {
        [DllImport("DesEncrypt.dll", EntryPoint = "?VinToPinForDF@@YA_NPAD0J@Z", CallingConvention = CallingConvention.StdCall)]
        static extern bool VinToPinForDF(IntPtr Out, IntPtr In, long datalen);
        private static Dictionary<string, SAMethod> _methods;
        static SecurityAccess()
        {
            _methods = new Dictionary<string, SAMethod>()
            {
                {nameof(DFSA),DFSA},
                {nameof(CoeffSA),CoeffSA},
                {nameof(EMSSA),EMSSA},
            };
        }
        private delegate byte[] SAMethod(byte[] data, uint param);
        public static byte[] GetKey(byte[] seed, string saMethod, string saParam)
        {
            if (_methods.TryGetValue(saMethod, out SAMethod method))
                if (uint.TryParse(saParam, System.Globalization.NumberStyles.HexNumber, null, out var uparam))
                    if (seed.Length == 4)
                        return method(seed, uparam);
            return Array.Empty<byte>();
        }
        private static byte[] DFSA(byte[] seed, uint pin = 0x13243546)
        {
            int i, j;
            var s = seed.ToArray();
            int seednumber = s.Length;
            for (i = 0; i < seednumber; i++)
                pin ^= ((pin << 5) + s[i] + (pin >> 4));
            for (j = 0; j < seednumber; j++)
                s[j] = (byte)((pin >> (j * 8)) & 0xff);
            return s;
        }
        public static byte[] CoeffSA(byte[] seed, uint coeff)
        {
            var s = seed.ToArray();
            for (int i = 0; i < 4; i++)
            {
                var k = s[i];
                k = s[i] = (byte)((k << 5) | (k >> 3));
                s[i] = (byte)(((k & 0xAA) >> 1) | ((k & 0x55) << 1));
            }
            var s0 = s[0];
            var s1 = s[1];
            var s2 = s[2];
            var s3 = s[3];

            s[0] = (byte)((s0 << 3) | (s1 >> 5));
            s[1] = (byte)((s0 >> 5) | (s1 << 3));
            s[2] = (byte)((s2 << 3) | (s3 >> 5));
            s[3] = (byte)((s2 >> 5) | (s3 << 3));

            s[0] = (byte)(s[0] ^ (coeff >> 24 & 0xff));
            s[1] = (byte)(s[1] ^ (coeff >> 16 & 0xff));
            s[2] = (byte)(s[2] ^ (coeff >> 8 & 0xff));
            s[3] = (byte)(s[3] ^ (coeff >> 0 & 0xff));

            s[0] = (byte)(~s[0]);
            s[1] = (byte)(~s[1]);
            s[2] = (byte)(~s[2]);
            s[3] = (byte)(~s[3]);
            return s;
        }
        public static byte[] EMSSA(byte[] seed, uint coeff)
        {
            var s = seed.ToArray();
            var cal0 = (byte)(s[0] ^ 0xaf);
            var cal1 = (byte)(s[1] ^ 0x9e);
            var cal2 = (byte)(s[2] ^ 0x01);
            var cal3 = (byte)(s[3] ^ 0x66);

            s[0] = (byte)((cal2 & 0x0f) | (cal2 & 0xf0));
            s[1] = (byte)(((cal0 & 0x0f) << 4) | ((cal1 & 0xf0) >> 4));
            s[2] = (byte)((cal1 & 0xf0) | ((cal3 & 0xf0) >> 4));
            s[3] = (byte)(((cal0 & 0x0f) << 4) | (cal3 & 0x0f));
            return s;
        }
        public static uint BKDRHash(char[] str)
        {
            //BKDR hash算法
            uint seed = 131;
            uint hash = 0;
            foreach (var b in str)
                hash = hash * seed + b;
            return (hash & 0x7FFFFFFF);
        }
        static byte[] oou = new byte[20];
        public static byte[] GetPIN(byte[] str)
        {
            var result = VinToPinForDF(Marshal.UnsafeAddrOfPinnedArrayElement(oou, 0), Marshal.UnsafeAddrOfPinnedArrayElement(str, 0), str.LongLength);
            return oou.Take(4).ToArray();
        }
        public static byte[] GetESK(byte[] str)
        {
            var result = VinToPinForDF(Marshal.UnsafeAddrOfPinnedArrayElement(oou, 0), Marshal.UnsafeAddrOfPinnedArrayElement(str, 0), str.LongLength);
            return oou.Skip(4).Take(16).ToArray();
        }

    }
}
