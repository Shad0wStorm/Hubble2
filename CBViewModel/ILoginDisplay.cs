using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CBViewModel
{
    public interface ILoginDisplay
    {
        void SetDisplayMode(LoginView.DisplayMode mode);

        void SetStatus(String status);

        bool CheckCancel();
    }
}
