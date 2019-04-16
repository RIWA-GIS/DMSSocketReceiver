using System;
using DMSSocketReceiver;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DMSSocketReceiverTest
{
    [TestClass]
    public class StateObjectTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            StateObject st = new StateObject();
            
            Assert.IsNotNull(st);
        }
    }
}
