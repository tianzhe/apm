using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apm
{
    public static class Util
    {
        public static List<List<string>> Combination(string[] input, int n, int m)
        {
            if(n < m || m == 0)
            {
                throw new InvalidOperationException(
                    string.Format("The length of the input array {0} must be no shorter than KFactor {1}", n, m));
            }

            List<List<string>> output = new List<List<string>>();

            string[] temp = new string[n];
            for(int i = 0; i < n; ++i)
            {
                temp[i] = input[i];
            }
            output.Add(temp.Take(m).ToList());

            int[] index = new int[m];
            for (int i = 0; i < m; ++i)
            {
                index[i] = m - 1;
            }

            while(index[0] < (n -1))
            {
                int j = m - 1;
                while(index[j] == (n - 1))
                {
                    --j;
                }
                ++index[j];
                for (int k = (j + 1); k < m; ++k)
                {
                    index[k] = index[j];
                }

                Swap(ref temp[j], ref temp[index[j]]);

                output.Add(temp.Take(m).ToList());
            }

            return output;
        }

        private static void Swap(ref string a, ref string b)
        {
            string temp = a;
            a = b;
            b = temp;
        }
    }
}
