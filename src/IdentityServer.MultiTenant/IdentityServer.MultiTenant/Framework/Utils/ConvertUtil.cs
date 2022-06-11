using System;
using System.Text;

namespace IdentityServer.MultiTenant.Framework
{
    public static class ConvertUtil
    {
        #region ToBool

        public static bool ToBool(object o) {
            return Convert.ToBoolean(o);
        }

        public static bool ToBool(object o, bool defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return Convert.ToBoolean(o);
            } catch {
                return defaultValue;
            }
        }

        #endregion

        #region ToInt

        public static int ToInt(object o) {
            return Convert.ToInt32(o);
        }

        public static int ToInt(object o, int defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return Convert.ToInt32(o);
            } catch {
                return defaultValue;
            }
        }

        #endregion

        #region ToLong

        public static long ToLong(object o) {
            return Convert.ToInt64(o);
        }

        public static long ToLong(object o, long defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return Convert.ToInt64(o);
            } catch {
                return defaultValue;
            }
        }

        #endregion

        #region ToDecimal

        public static decimal ToDecimal(object o) {
            return Convert.ToDecimal(o);
        }

        public static decimal ToDecimal(object o, decimal defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return Convert.ToDecimal(o);
            } catch {
                return defaultValue;
            }
        }

        #endregion

        #region ToFloat

        public static float ToFloat(object o) {
            return Convert.ToSingle(o);
        }

        public static float ToFloat(object o, float defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return Convert.ToSingle(o);
            } catch {
                return defaultValue;
            }
        }

        #endregion

        #region ToDouble

        public static double ToDouble(object o) {
            return Convert.ToDouble(o);
        }

        public static double ToDouble(object o, double defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return Convert.ToDouble(o);
            } catch {
                return defaultValue;
            }
        }

        #endregion

        #region ToDateTime

        public static DateTime ToDateTime(object o) {
            return Convert.ToDateTime(o);
        }

        public static DateTime ToDateTime(object o, DateTime defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return Convert.ToDateTime(o);
            } catch {
                return defaultValue;
            }
        }

        #endregion

        #region ToDate、Time、Second、String

        public static string ToDateString(DateTime o) {
            return o.Year.ToString("0000") + "/" + o.Month.ToString("00") + "/" + o.Day.ToString("00");
        }

        public static string ToDateTimeString(DateTime o) {
            return o.Year.ToString("0000") + "/" + o.Month.ToString("00") + "/" + o.Day.ToString("00") + " " + o.Hour.ToString("00") + ":" + o.Minute.ToString("00");
        }

        public static string ToDateTimeSecondString(DateTime o) {
            return o.Year.ToString("0000") + "/" + o.Month.ToString("00") + "/" + o.Day.ToString("00") + " " + o.Hour.ToString("00") + ":" + o.Minute.ToString("00") + ":" + o.Second.ToString("00");
        }

        public static string ToTimeString(DateTime o) {
            return o.Hour.ToString("00") + ":" + o.Minute.ToString("00");
        }


        public static string ToDateString(object o) {
            return ToDateString(ToDateTime(o));
        }

        public static string ToDateTimeString(object o) {
            return ToDateTimeString(ToDateTime(o));
        }

        public static string ToDateTimeSecondString(object o) {
            return ToDateTimeSecondString(ToDateTime(o));
        }

        public static string ToTimeString(object o) {
            return ToTimeString(ToDateTime(o));
        }


        public static string ToDateString(object o, string defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return ToDateString(ToDateTime(o));
            } catch {
                return defaultValue;
            }
        }

        public static string ToDateTimeString(object o, string defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return ToDateTimeString(ToDateTime(o));
            } catch {
                return defaultValue;
            }
        }

        public static string ToDateTimeSecondString(object o, string defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return ToDateTimeSecondString(ToDateTime(o));
            } catch {
                return defaultValue;
            }
        }

        public static string ToTimeString(object o, string defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return ToTimeString(ToDateTime(o));
            } catch {
                return defaultValue;
            }
        }

        #endregion

        #region ToString

        public static string ToString(object o) {
            return o.ToString();
        }

        public static string ToString(object o, string defaultValue) {
            if (o == null || o == DBNull.Value) {
                return defaultValue;
            }

            try {
                return o.ToString();
            } catch {
                return defaultValue;
            }
        }

        #endregion

        #region Long and IP

        public static long IpToLong(string ip) {
            string[] arr = ip.Split('.');
            return long.Parse(arr[0]) << 24 | long.Parse(arr[1]) << 16 | long.Parse(arr[2]) << 8 | long.Parse(arr[3]);
        }

        public static string LongToIp(long ip) {
            StringBuilder sb = new StringBuilder();
            sb.Append((ip >> 24) & 0xFF).Append(".");
            sb.Append((ip >> 16) & 0xFF).Append(".");
            sb.Append((ip >> 8) & 0xFF).Append(".");
            sb.Append(ip & 0xFF);
            return sb.ToString();
        }

        #endregion

        #region Byte Hex

        public static string BytesToHexString(byte[] b) {
            StringBuilder hexString = new StringBuilder(64);

            for (int i = 0; i < b.Length; i++) {
                hexString.Append(String.Format("{0:X2}", b[i]));
            }

            return hexString.ToString();
        }

        public static byte[] HexStringToBytes(string hex) {
            if (hex.Length == 0) {
                return new byte[] { 0 };
            }

            if (hex.Length % 2 == 1) {
                hex = "0" + hex;
            }

            byte[] result = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length / 2; i++) {
                result[i] = byte.Parse(hex.Substring(2 * i, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            }

            return result;
        }

        #endregion

        #region Escape

        public static string Escape(string value) {
            value = value.Replace("\r", "\\r");
            value = value.Replace("\n", "\\n");
            value = value.Replace("\t", "    ");
            value = value.Replace("\\", "\\\\");
            value = value.Replace("\"", "\\\"");

            return value;
        }

        #endregion
    }
}
