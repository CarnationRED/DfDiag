using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VCILib
{
    public static class GlobalParameters
    {
        public static string VIN { get; set; } = "LDP31B96";
        public static string PIN { get; set; }
        public static string ESK { get; set; }
        public static Dictionary<string, PropertyInfo> Parameters;
        static GlobalParameters()
        {
            Parameters = typeof(GlobalParameters)
                .GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Aggregate(new Dictionary<string, PropertyInfo>(), (coll, n) =>
                {
                    coll.Add(n.Name, n);
                    return coll;
                });
        }

        internal static bool TryGetParamBytes(string paramName, out IEnumerable<byte> param)
        {
            if (paramName.StartsWith('{') && paramName.EndsWith('}'))
            {
                    paramName = paramName.Substring(1, paramName.Length - 2);
                if (Parameters.TryGetValue(paramName, out var info))
                {
                    var format = string.Empty;
                    if (paramName.Contains(':'))
                    {
                        int idx = paramName.IndexOf(':');
                        format = paramName.Substring(idx, paramName.Length - 2 - idx);
                    }
                    var value = info.GetValue(null).ToString();
                    switch (format)
                    {
                        default:
                        case "ASCII":
                            param = value.Aggregate(new List<byte>(), (l, n) =>
                            {
                                l.Add((byte)n);
                                return l;
                            }).ToArray();
                            break;
                        case "BCD":
                            var tmp = (value.Length & 1) == 1 ? "0".Concat(value) : value;
                            var arr = tmp.ToArray();
                            var result = new List<byte>();
                            for (int i = 0; i < arr.Length >> 1; i++)
                                result.Add((Byte)(((arr[i * 2] - '0') << 4) | (arr[i * 2 + 1] - '0')));
                            param = result.ToArray();
                            break;
                    }
                    return true;
                }
            }

            param = null;
            return false;
        }
    }
}
