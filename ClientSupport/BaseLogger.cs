using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    public abstract class BaseLogger
    {
        public abstract void Log(UserDetails user, LogEntry entry);
    }
}
