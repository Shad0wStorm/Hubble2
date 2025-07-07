/*----------------------------------------------------------------------------
 *  FILE: WatchDogTarget.cpp
 *
 *		Copyright(c) 2013 Frontier Developments Ltd.
 *
 *		WatchDogTarget definition. 
 *      NOTE : we use standard library functions so that we don't
 *             require any static initialization of our own libraries
 *
 *----------------------------------------------------------------------------
 */

#include "WatchDogTarget.h"

HANDLE WatchDogTarget::s_event = NULL;
HANDLE WatchDogTarget::s_fileMapping = NULL;

void WatchDogTarget::StaticInit(char const* _pPID) 
{
	if(_pPID != NULL)
	{
		char eventId[MAX_PATH]; 
		strncpy(eventId, _pPID, MAX_PATH);
		strncat(eventId, "E", MAX_PATH);

		char fileMappingId[MAX_PATH];
		strncpy(fileMappingId, _pPID, MAX_PATH);
		strncat(fileMappingId, "F", MAX_PATH);

		// our exception signal, note : we use 'CreateEvent' as opposed to 
		// 'OpenEvent' so that we can check if we inherited the event.
		s_event = CreateEvent( 
			NULL,	// default security attributes
			true,	// manual-reset event
			false,				// initial signal state
			eventId);	// object name

		// if we didn't inherit the event, then we can't signal an exception
		// to the parent watchdog process(if one exists).
		if(s_event != NULL && GetLastError() == ERROR_ALREADY_EXISTS)
		{
			// open our inherited file mapping. note : we use 'CreateFileMapping' as opposed to 
			// 'OpenFileMapping' so that we can check if we inherited the event.
			s_fileMapping = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, sizeof(MemoryDumpArgs), fileMappingId);

			// if we didn't inherit the mapping, then we can't pass data
			// to the parent watchdog process(if one exists).
			if(s_fileMapping != NULL && GetLastError() == ERROR_ALREADY_EXISTS)
			{
				// create a handler for the point at which we want to create a crash report
				SetUnhandledExceptionFilter(OnUnhandledException);
			}
		}
	}
}

void WatchDogTarget::StaticShitDown()
{
	CloseHandle(s_event);
	CloseHandle(s_fileMapping);
}

LONG WINAPI WatchDogTarget::OnUnhandledException(struct _EXCEPTION_POINTERS * _pExceptionPtrs)
{	 
	if(SignalException(_pExceptionPtrs))
	{
		// we handled the exception by performing a dump
		return EXCEPTION_EXECUTE_HANDLER;
	}

	// we didn't end up doing anything, revert to normal practice
	return EXCEPTION_CONTINUE_SEARCH;
}

bool WatchDogTarget::SignalException(struct _EXCEPTION_POINTERS * _pExceptionPtrs)
{
	// this should never be the case, but just in-case
	if(s_event == NULL || s_fileMapping == NULL)
		return false;

	// map the view to write
	LPVOID pData = MapViewOfFile(s_fileMapping, FILE_MAP_WRITE, 0, 0, 0);
	if(pData != NULL)
	{
		// make note of data we need for the memory dump report
		MemoryDumpArgs args;
		args.threadID = (int)GetCurrentThreadId(); 
		args.pExceptionPtrs = _pExceptionPtrs;

		// write the memory dump info
		memcpy(pData, &args, sizeof(MemoryDumpArgs));

		// clean up
		UnmapViewOfFile(pData);
	}

	// signal to the watch dog that an exception has been thrown, so it can 
	// read our newly written data and generate a dump report
	SetEvent(s_event);

	// we sleep forever here expecting the watchdog to terminate us once 
	// it has created the crash dump report.
	Sleep(INFINITE);

	return true;
}