using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StockDataModelEntities;
using System.IO;
using System.Configuration;

namespace apm
{
    class Program
    {
        static void Main(string[] args)
        {
            Model inst = new CAPM();
            Portfolio p = inst.Generate();
        }
    }
}
