using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    /// <summary>
    /// Interface used to obtain an identifier for the current machine.
    /// </summary>
    public abstract class MachineIdentifierInterface
    {
        /// <summary>
        /// Get the current machine identifier. This should be consistent
        /// across calls, and runs.
        /// 
        /// The server will use the returned value to determine whether the
        /// machine has been seen previously and thus requires additional
        /// authentication.
        /// 
        /// The precise method is left to the implementation but it should
        /// provide:
        /// a) Differentiation between machines.
        /// b) Consistency between runs to avoid frequent reauthorisation
        /// particularly if a user in only permitted a limited number of
        /// authorised machines.
        /// c) It is reasonable, but not required, that reinstallation of
        /// the operating system or significant hardware changes may result
        /// in a machine being detected as changed.
        /// </summary>
        /// <returns>The machine identification string.</returns>
        public abstract String GetMachineIdentifier();

        /// <summary>
        /// Get the users steam key for the product
        /// </summary>
        /// <returns></returns>
        public abstract String GetSteamKey(String ident);
    }
}
