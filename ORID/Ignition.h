#pragma once

#include "Options.h"
#include "OVRHelper.h"


//-----------------------------------------------------------------------
//! @brief Class responsible for starting the launcher.
//-----------------------------------------------------------------------
class Ignition
{
public:
	Ignition(void);
	~Ignition(void);

	int LaunchApplication(Options const& options, OVRHelper const& helper);
	int LaunchRegistration(Options const& options, OVRHelper const &helper);

private:
	void DisplayPrompt(Options const& option, std::wstring id);
	int StartApplication(std::wstring commandLine);
	int Shell(std::wstring target);
	int RequiredForString(const char* argv[], int count, const char* space);
	int CombineString(char* dest, int size, const char* argv[], int count, const char* space);
};

