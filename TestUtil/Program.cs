using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using apm;

namespace TestUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] a = { "A", "B", "C", "D", "E", "F", "G"};

            List<List<string>> b = Util.Combination(a, a.Length, 4);
        }
    }
}
