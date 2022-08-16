using System.Text;
namespace Utils
{
    public static class Util
    {
        public static bool PartialSequenceEqual<T>(this IEnumerable<T> a, IEnumerable<T> b)
        {
            using (var ea = a.GetEnumerator())
            using (var eb = b.GetEnumerator())
                while (ea.MoveNext() && eb.MoveNext())
                    if (!Equals(ea.Current, eb.Current))
                        return false;
            return true;
        }
        public static byte[] ToByteArray(this string s) => s.Select(c => (byte)c).ToArray();
        public static byte[] HexTo8ByteArray(this string msg)
        {
            msg = new string(msg.ToUpper().Where(c => ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))).ToArray());
            if (msg.Length == 0) return null;
            if (msg.Length % 2 == 1) msg = msg.Insert(msg.Length - 1, "0");
            //while (msg.Length < 16) msg += "00";
            if (msg.Length < 16) msg = msg.PadRight(16, '0');
            msg = msg.Substring(0, 16);
            List<byte> result = new List<byte>(msg.Length / 2);
            for (int i = 0; i < msg.Length; i += 2)
            {
                result.Add(byte.Parse(msg.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }
            if (result.Count != 8) throw new Exception();
            return result.ToArray();
        }
        public static byte[] HexToByteArray(this string msg, int length = -1)
        {
            msg = new string(msg.ToUpper().Where(c => ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))).ToArray());
            if (msg.Length == 0) return null;
            if (msg.Length % 2 == 1) msg = msg.Insert(msg.Length - 1, "0");
            if (length == -1)
                length = msg.Length / 2;
            if (msg.Length < length << 1) msg = msg.PadRight(length << 1, '0');
            msg = msg.Substring(0, length << 1);
            List<byte> result = new List<byte>(msg.Length >> 1);
            for (int i = 0; i < msg.Length; i += 2)
            {
                result.Add(byte.Parse(msg.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }
            if (result.Count != length) throw new Exception();
            return result.ToArray();
        }
        public static string ByteArrToHexString(this byte[] arr)
        {
            return arr.Aggregate(new StringBuilder(), (str, b) => str.Append($"{b:X2} ")).ToString().TrimEnd(' ');
        }
        public static string ByteArrToHexString(this IEnumerable<byte> arr)
        {
            return arr.Aggregate(new StringBuilder(), (str, b) => str.Append($"{b:X2} ")).ToString().TrimEnd(' ');
        }
    }
}