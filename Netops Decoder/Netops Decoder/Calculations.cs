using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Netops_Decoder
{
    //this class make the sensor data convert 
    public class Calculations
    {
        // gets the complement of incoming data
        public string BitComplement(string bit)
        {
            var d = "";
            for (var index = 0; index < bit.Length; index++)
            {

                if (bit.Substring(index, 1) == "0")
                {
                    d += "1";
                }
                else
                {
                    d += "0";
                }
            }
            return d;
		}

        //puts a zero to the right of the byte
        public string ZeroAdd(string bytes)
        {
            var d = "";
            var count = 0;
            for (var index = 1; index < bytes.Length; index++)
            {
                d += bytes.Substring(index, 1);
    
                count += 1;
            }

            var data = "0";
            if (bytes.Length != 4 || bytes.Length != 8 || bytes.Length != 12 
                || bytes.Length != 16 || bytes.Length != 20 || bytes.Length != 24)
            {
                data += d;
            }
            return data;
        }

        public string GenericZeroAdd(string data)
        {
            var d = "";
            var count = 0;
            for (var index = 0; index < data.Length; index++)
            {
                d += data.Substring(index, 1);
                count += 1;
            }

            var divisor = count;
            var remainder = 0;
            for (var i = 0; i < count; i++)
            {
                remainder = divisor - 4;
                divisor = remainder;
                if (remainder == 1 || remainder == 2 || remainder == 3)
                {
                    var addZero = "0";
                    for (var index1 = 1; index1 < remainder; index1++)
                    {
                        addZero += "0";
                    }
                    var sum = addZero + data;
                    return sum;
                }
                if (divisor < 0)
                {
                    return data;
                }
            }

            return "GenericZeroAdd";  //temp
        }

        //aggregates the incoming binary number with 1
        public string OneAddBinary(string a)
        {
            var b = "1";
            var i = a.Length - 1;
            var j = b.Length- 1;
            var carry = 0;
            var result = "";
            while (i >= 0 || j >= 0)
            {
                var m = i < 0 ? 0 : a[i] | 0;
                var n = j < 0 ? 0 : b[j] | 0;
                carry += m + n;
                result = carry % 2 + result;
                carry = carry / 2 | 0;
                i--;
                j--;
            }

            //if (carry != 0)
            //{
            //    result = carry + result;
            //}
            return result;
        }

        //reverses the entered string
        public string ReverseString(string s)
        {
            string[] arr = s.Split("").Reverse().ToArray();
            return string.Join("",arr);

            //var arr = s.Split("");
            //arr.Reverse();
            //return string.Join("", arr); // changed implementation
        }

        //Returns The sent hexadecimal data in binary.
        public string Hex2Binary(string hex)
        {
            //return (parseInt(hex, 16).toString(2)).padStart(8, '0'); Javascript implementation
            return Convert.ToString(Convert.ToInt32(hex, 16), 2).PadLeft(8,'0');
        }

        // Returns The sent hexadecimal data as Decimal.
        public string Hex2Decimal(string hex)
        {
            return Convert.ToString(Convert.ToInt32(hex, 16), 10);
           // return hex.ToLower().Split("").Aggregate((result, ch) =>
               // Convert.ToInt32(result) * 16 + "0123456789abcdefgh".IndexOf(ch)));
        }

        /// Returns binary data sent in hexadecimal.
        public string Binary2Hex(string n)
        {
            return Convert.ToString(Convert.ToInt32(n, 2),16);
        }

        //Returns the binary data sent as decimal.
        public string Binary2Dec(string n)
        {
            return Convert.ToString(Convert.ToInt32(n, 2), 10);
        }

        // Returns the sent decimal data as binary.
        public string Dec2Binary(int n)
        {
            return Convert.ToString(n, 2);
        }

        //Returns the sent decimal data hexadecimal.
        public string Dec2Hex(string i)
        {
            return Convert.ToString(Convert.ToInt32(i,10), 16);
        }

        //Returns TimeStamp type information as DateTime.
        public string UnixTimeStampToDateTime(int unix_timestamp)
        {
            return new DateTime(unix_timestamp * 1000).ToString("o").Substring(0, 19).Replace('T', ' ');
        }

    }
}
