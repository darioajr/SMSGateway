using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;

namespace SMSGateway
{
    public class Util
    {
        private static string from = "ÁÀÂÃÄáàâãäÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÕÖóòôõöÚÙÛÜúùûüÇçÑñÝ?ýÿ´`\"'ªº";
        private static string to = "AAAAAaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuCcNnYYyy      ";

        public static string Translate(string input, string from, string to)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in input)
            {
                int i = from.IndexOf(ch);
                if (from.IndexOf(ch) < 0)
                {
                    sb.Append(ch);
                }
                else
                {
                    if (i >= 0 && i < to.Length)
                    {
                        sb.Append(to[i]);
                    }
                }
            }
            return sb.ToString();
        }

        public static string Translate(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in input)
            {
                int i = from.IndexOf(ch);
                if (from.IndexOf(ch) < 0)
                {
                    sb.Append(ch);
                }
                else
                {
                    if (i >= 0 && i < to.Length)
                    {
                        sb.Append(to[i]);
                    }
                }
            }
            return sb.ToString();
        }
    }
}