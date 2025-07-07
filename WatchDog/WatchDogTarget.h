/*----------------------------------------------------------------------------
 *  FILE: WatchDogTarget.h
 *
 *		Copyright(c) 2013 Frontier Developments Ltd.
 *
 *		WatchDogTarget declaration. 
 *      NOTE : we use standard library functions so that we don't
 *             require any static initialization of our own libraries
 *
 *----------------------------------------------------------------------------
 */

#pragma  once

//#include "fCore/Platform/fBeginPlatformIncludes.h"
#include <windows.h>
//#include "fCore/Platform/fEndPlatformIncludes.h"


struct MemoryDumpArgs
{
	int threadID;
	_EXCEPTION_POINTERS* pExceptionPtrs;
};

class WatchDogTarget
{
public:
	static void StaticInit(char const* _pPID);
	static void StaticShitDown();
	static LONG WINAPI OnUnhandledException(struct _EXCEPTION_POINTERS * _pExceptionPtrs);
	static bool SignalException(struct _EXCEPTION_POINTERS * _pExceptionPtrs);

private:
	static HANDLE s_event, s_fileMapping;
};
