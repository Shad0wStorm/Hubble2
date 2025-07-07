using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ClientSupport;

namespace MockClientSupport
{
    public class MockMachineIdentifier : MachineIdentifierInterface
    {
        public override String GetMachineIdentifier()
        {
            return "Individual";
        }

        public override String GetSteamKey(String ident)
        {
            return ident;
        }
    }
}
