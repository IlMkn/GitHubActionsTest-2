using System;
using Xunit;

namespace XUnitTestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            TestConsoleApp.Sum temp = new TestConsoleApp.Sum();
            int expected = 50;
            Assert.Equal(expected, temp.GiveResult());
        }
    }
}
