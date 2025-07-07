using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CBViewModel
{
    public interface UserInterface
    {
        String GetOSIdent();

        void MarkForUpdate();

        void PopupMessage(String description);
        void WarningMessage(String description, String title);
        void ErrorMessage(String description, String title);
        bool YesNoQuery(String description, String title);

        void UpdateProjectList();
        void UpdateSelectedProject();

        void MonitorCompleted();
        void ShowMonitorCancelButton(bool show);

        void MoveToFront(Process process);
        void CloseWindow();
    }
}
