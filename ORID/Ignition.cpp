#include <Windows.h>

#include "Ignition.h"
#include "OVRHelper.h"

#include <sstream>
#include <sys/stat.h>

Ignition::Ignition(void)
{
}


Ignition::~Ignition(void)
{
}

//-----------------------------------------------------------------------
//! @brief Start the application given in the options with a token.
//!
//! Wait for the spawned application to exit in case the application
//! starting this application is using the process handle to determine
//! when the game exits.
//-----------------------------------------------------------------------
int Ignition::LaunchApplication(
	Options const& options,
	OVRHelper const& helper
)
{
	std::wstringstream commandLine;

	commandLine << options.GetExecutable();
	commandLine << " /oculus " << options.GetOculusApplicationID().c_str();

	for (int a=0; a<options.GetAdditionalCount(); ++a)
	{
		commandLine << " " << options.GetAdditionalOption(a);
	}

	std::wstring final = commandLine.str();
	int required = final.length()+1;
	_TCHAR* clstr = new _TCHAR[required];
	_tcsncpy_s(clstr, required, final.c_str(), required);
	int result = 0;
	result = StartApplication(clstr);

	delete[] clstr;

	return result;
}

//-----------------------------------------------------------------------
//! @brief Open the registration page.
//!
//! Passes on the Oculus identity information so that the registration
//! page can determine which products require registration and which
//! Oculus user is registering them.
//!
//! Do not wait for the page to be closed since it is does not ensure
//! that the products are actually registered. We require that the user
//! go through the online registration process in their own time and
//! restart the game through Oculus on completion.
//!
//! If the user restarts the game without completing the registration
//! process it will simply restart the registration process again.
//-----------------------------------------------------------------------
int Ignition::LaunchRegistration(
	Options const& options,
	OVRHelper const& helper
)
{
	std::wstringstream commandLine;
	bool shell = false;

	commandLine << options.GetRegistrationLink();
	commandLine << "?oculus=true";
	std::wstring forced = options.GetForcedRegistration();
	if (forced.size()>0)
	{
		commandLine << "&forced=";
		commandLine << forced;
	}
	commandLine << "&userex=";
	commandLine << helper.User();
	commandLine << "&useraccesstoken=";
	commandLine << helper.UserAccessToken();
	commandLine << "&nonce=" << helper.Nonce();
	shell = true;

	std::wstring final = commandLine.str();
	int required = final.length()+1;
	_TCHAR* clstr = new _TCHAR[required];
	_tcsncpy_s(clstr, required, final.c_str(), required);
	int result = 0;
	result = Shell(clstr);
	delete[] clstr;

	DisplayPrompt(options, _T("REG"));

	return result;
}

//-----------------------------------------------------------------------
//! @brief Display a prompt using an external application.
//-----------------------------------------------------------------------
void Ignition::DisplayPrompt
(
	Options const& options,
	std::wstring id
)
{
	std::wstring prompt = options.GetExecutable();
	int applicationDirEnd = prompt.length();
	while (prompt[applicationDirEnd]!='\\')
	{
		--applicationDirEnd;
	}

	prompt = prompt.substr(0, applicationDirEnd+1);
	prompt = prompt + _T("ORPrompt.exe");

	DWORD attrib = GetFileAttributes(prompt.c_str());

	if ((attrib!=INVALID_FILE_ATTRIBUTES) &&
		(!(attrib & FILE_ATTRIBUTE_DIRECTORY)))
	{
		std::wstringstream commandLine;
		commandLine << prompt << " ";
		commandLine << id;
		StartApplication(commandLine.str());
	}
}

//-----------------------------------------------------------------------
//! @brief Start an application with the given command line.
//-----------------------------------------------------------------------
int Ignition::StartApplication
(
	std::wstring commandLine
)
{
	STARTUPINFO si;
	PROCESS_INFORMATION pi;

	ZeroMemory( &si, sizeof(si));
	si.cb = sizeof(si);
	ZeroMemory( &pi, sizeof(pi));

	int required = commandLine.length()+1;
	_TCHAR* clstr = new _TCHAR[required];
	_tcsncpy_s(clstr, required, commandLine.c_str(), required);

	int result = 0;

	if (!CreateProcess(NULL,
		clstr,
		NULL,
		NULL,
		FALSE,
		0,
		NULL,
		NULL,
		&si,
		&pi))
	{
		result = 1;
	}
	else
	{
		// Wait for application to finish
		WaitForSingleObject(pi.hProcess, INFINITE);

		// Close Process and Thread Handles.
		CloseHandle( pi.hProcess );
		CloseHandle( pi.hThread );
	}

	delete[] clstr;

	return result;
}

//-----------------------------------------------------------------------
//! @brief Pass a command to the shell for execution
//!
//! Allows the OS to determine what to do with the command. In this
//! case we want the opened URI to be sent to the system default handler
//! which should be the default web browser.
//-----------------------------------------------------------------------
int Ignition::Shell(std::wstring target)
{
	int result = 0;

	int required = target.length()+1;
	_TCHAR* clstr = new _TCHAR[required];
	_tcsncpy_s(clstr, required, target.c_str(), required);

	HINSTANCE hresult = ShellExecute(NULL, _T("open"), clstr, NULL, NULL, SW_SHOWNORMAL);
	if (((int)hresult)>32)
	{
	}
	else
	{
		result = 1;
	}
	return result;
}