#include "ORIDLaunch.h"
#include "Options.h"
#include "OVRHelper.h"
#include "Ignition.h"
#include "SimpleJSON.h"

#include <windows.h>
#include <sstream>
#include <string>

ORIDLaunch::ORIDLaunch(void)
{
}


ORIDLaunch::~ORIDLaunch(void)
{
}

//-----------------------------------------------------------------------
//! @brief Manage the game startup process
//-----------------------------------------------------------------------
int ORIDLaunch::Launch(int argc, char* argv[])
{
	Options options;
	options.Process(argc, argv);

	if (options.GetApplicationID()==Options::AID_Unknown)
	{
		MessageBox(NULL, _T("A valid application code must be provided."), _T("Error"), 0);
		return 1;
	}

	OVRHelper helper(&options);

	if (HasNoErrors(&helper))
	{
		Ignition ignition;
		if (helper.RequiresRegistration())
		{
			ignition.LaunchRegistration(options, helper);
		}
		else
		{
			if (HasNoErrors(&helper))
			{
				if (ignition.LaunchApplication(options, helper)!=0)
				{
					std::wstringstream message;
					message << "Failed to start ";
					message << options.GetExecutable();

					MessageBox(NULL, message.str().c_str(), _T("Error"), 0);
				}
			}
		}
	}
	return 0;
}

//-----------------------------------------------------------------------
//! @brief Test whether any errors have been reported by the given helper.
//!
//! If any errors were reported display them to the user in a message box.
//-----------------------------------------------------------------------
bool ORIDLaunch::HasNoErrors(OVRHelper* helper)
{
	if (helper->Error().length()>0)
	{
		std::wstringstream message;
		message << "Failed to initialise with Oculus : " << helper->Error();
		MessageBox(NULL, message.str().c_str(),_T("Error"), 0 );
		return false;
	}
	return true;
}