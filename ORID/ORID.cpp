// ORID.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include <Windows.h>
#include "ORIDLaunch.h"

#define CONSOLE (0)

#if CONSOLE
int _tmain(int argc, _TCHAR* argv[])
{
	ORIDLaunch launch;

	return launch.Launch(argc, argv);
}
#else
int CALLBACK WinMain(HINSTANCE hInstance,
			HINSTANCE hPrevInstance,
			LPSTR lpCmdLine,
			int nCmdShow)
{
	ORIDLaunch launch;

	return launch.Launch(__argc, __argv);
}
#endif

