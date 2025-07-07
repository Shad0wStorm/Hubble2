using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClientSupport;

namespace ClientSupport.Tests
{
    [TestClass]
    public class ProjectTests
    {
        [TestMethod]
        public void ProjectIsUninitialised()
        {
            Project p = new Project("test", "C:\\Temp\\Missing");
            Assert.IsFalse(p.Installed);
        }
    }
}
