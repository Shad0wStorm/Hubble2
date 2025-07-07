using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RunElite
{
    class Program
    {
        /* RunElite
         * 
         * This is a dummy application used to display the passed arguments
         * to the user. It is hoped that this can help identify the cause for
         * elite failing to start on some user machines. So far it has not been
         * possible to reproduce the problem locally.
         * 
         * The problem exhibits when the user presses the "Play" button on the
         * launcher which disappears briefly and then reappears. This suggests
         * that the launcher is starting and application and it is then exiting
         * as these are the triggers for the hiding and showing of the play
         * button.
         * 
         * In the first instance this program can be used to replace
         * EliteDangerous.exe to test that it is being started by the launcher
         * via some route.
         * 
         * One option is to actually replace the EliteDangerous.exe and
         * renaming this application to take the place. The second approach is
         * to place this file in the same folder as EliteDangerous.exe and
         * update the corresponding versioninfo.txt to point at this executable
         * rather than EliteDangerous.exe. Either approach can be reversed by
         * performing a "Validate Game Files" operation from the user menu in 
         * the launcher.
         * 
         * If the replacement is successful *and* the launcher/watchdog are
         * behaving as expected then when the user clicks play a dialog will
         * pop up showing the arguments the game was started with. Clicking ok
         * will close the window and the launcher will again show the Play
         * button. If the behaviour is unchanged, Play button vanishes and then
         * reappears this indicates that the game is not being started
         * correctly and the error is not being detected/reported correctly.
         * 
         * In this case it is possible to replace the WatchDog.exe executable
         * with a suitably renamed copy of this executable. WatchDog can be
         * found in the launcher directory. If clicking play now pops up the
         * message box this indicates that watchdog is being started correctly
         * and the problem exists within the watchdog application. If the
         * dialog still does not appear then the problem is with starting
         * WatchDog and most likely within the launcher project runner code.
         */
        static void Main(string[] args)
        {
            String message = "Supplied arguments :\n\n";
            message += Environment.CommandLine+"\n\n";
            MessageBox.Show(message,"Application started");
        }
    }
}
