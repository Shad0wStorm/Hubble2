#pragma once

#include "stdafx.h"

class OVRHelper;

//-----------------------------------------------------------------------
//! @brief Class for managing Launcher startup options on Oculus.
//-----------------------------------------------------------------------
class ORIDLaunch
{
public:
	ORIDLaunch(void);
	~ORIDLaunch(void);

	int Launch(int argc, char* argv[]);

private:
	bool HasNoErrors(OVRHelper* helper);
};

