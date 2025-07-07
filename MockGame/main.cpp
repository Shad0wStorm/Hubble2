#include <iostream>
#include <sstream>
#include <conio.h>


#include "..\WatchDog\WatchDogTarget.h"


int main()
{
	const char* lpCmdLine = GetCommandLine(); // simulate windows app

	// Initialize the watchDog if we find an appropriate CmdLine Arg
	// NOTE : we use standard library functions so that we don't
	// require any static initialization of our own libraries
	const int MAX_CMD_LINE_LENGTH = 10 * MAX_PATH;
	char cmdLineCopy[MAX_CMD_LINE_LENGTH]; 
	strcpy_s(cmdLineCopy, MAX_CMD_LINE_LENGTH, lpCmdLine);

	char const* pWatchDogID = NULL;
	char const* pDelimiter = " ";
	char const* pKey = strtok(cmdLineCopy, pDelimiter);
	while(pKey != 0)
	{
		char const* pValue = strtok(0, pDelimiter);
		if(pValue != 0)
		{
			std::cout << pKey << " : " << pValue << "\n";
			if(!strcmp(pKey, "-WDID"))
			{
				pWatchDogID = pValue;
			}
		}

		pKey = pValue;
	}

	if(pWatchDogID != NULL)
	{
		WatchDogTarget::StaticInit(pWatchDogID);
	}
	else
	{
		std::cout << "### Watchdog disabled ###\n";
	}
	
	while(true)
	{
		std::cout << "Press '1' to exit, '2' to force a crash\n";

		int key = _getch();
		if(key == '1')
		{
			std::cout << "Exiting... Bonjour!\n";
			break;
		}
		else if(key == '2')
		{
			std::cout << "Forcing crash... BOOM!\n";
			int *pBadPtr = NULL;
			*pBadPtr = 0;
		}

		std::cout << "Computer says no...\n";
	}

	return 0;
}