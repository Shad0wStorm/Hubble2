// Escape.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>

int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
{
	LPTSTR prefix = _T("CMD.EXE /C START ");
	int prefixLength = wcslen(prefix);
	int arglen = prefixLength + 2;
	for (int a = 1; a < argc; ++a)
	{
		arglen += wcslen(argv[a]);
	}
	wchar_t* commandLine = new wchar_t[arglen];
	wcscpy_s(commandLine, arglen,  prefix);
	for (int a = 1; a < argc; ++a)
	{
		int offset = wcslen(commandLine);
		wcscpy_s(commandLine+offset, arglen-offset, argv[a]);
	}


	STARTUPINFO si;
	PROCESS_INFORMATION pi;

	si.cb = sizeof(si);
	si.lpReserved = NULL;
	si.lpDesktop = NULL;
	si.lpTitle = NULL;
	si.dwX = 0;
	si.dwY = 0;
	si.dwXSize = 0;
	si.dwYSize = 0;
	si.dwXCountChars = 0;
	si.dwYCountChars = 0;
	si.dwFillAttribute = 0;
	si.dwFlags = 0;
	si.wShowWindow = 0;
	si.cbReserved2 = 0;
	si.lpReserved2 = 0;
	si.hStdInput = 0;
	si.hStdOutput = 0;
	si.hStdError = 0;

	int success = CreateProcess(NULL, commandLine, NULL, NULL, false,
		CREATE_BREAKAWAY_FROM_JOB | CREATE_NO_WINDOW,
		NULL, NULL, &si, &pi);

	if (success!=0)
	{
		CloseHandle(pi.hProcess);
		CloseHandle(pi.hThread);
		return 0;
	}
	return 1;
}

