using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    public abstract class DirectorySelector
    {
        public abstract String SelectDirectory(String initial);
    }
}
