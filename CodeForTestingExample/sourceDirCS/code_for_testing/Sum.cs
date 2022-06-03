using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsoleApp
{
    public class Sum
    {
        int a, b;
        public Sum()
        {
            a = 41;
            b = 12;
        }

        public int GiveResult()
        {
            int res = 0;
            res = this.a + this.b;
            return res;
        }
    }
}
