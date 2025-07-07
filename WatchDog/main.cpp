#include <iostream>
#include <fstream>
#include <Windows.h>
#include <dbghelp.h>
#include <shellapi.h>
#include <shlobj.h>
#include <sys/stat.h>
#include <StrSafe.h>
#include <sstream>
#include <time.h>
#include "sha1.h"
#include "simplehttp.h"
#include "rc4encrypt.h"

#define CREATE_PROCESS_USES_SEPARATE_ARGS (1)
#define DEBUG_DEBUGGING (_DEBUG && 0)

std::ostream* flog = NULL;

struct MemoryDumpArgs
{
	int threadID;
	_EXCEPTION_POINTERS* pExceptionPtrs;
	char dumpfile[256];
};

// Function declarations
class ProcessDebugger
{
public:
	ProcessDebugger( PROCESS_INFORMATION const& _pi, std::string const& _appPath, std::string const& _cmdLine, time_t startTime );
	~ProcessDebugger();

	void WaitForDebuggerToAttach();

private:
	PROCESS_INFORMATION m_pi;
	std::string m_appPath;
	std::string m_cmdLine;
	time_t m_startTime;
	volatile bool m_stopNow;
	HANDLE m_hDebugger;
	bool m_hasReceivedCreateProcess;
	bool m_hasReceivedInitialBreakpoint;
	bool m_hasSignaledDebuggerAttached;
	HANDLE m_hDebuggerAttachedEvent;

	static DWORD WINAPI DebugThread( void *_parameter );
	DWORD DebugThread();
	DWORD ProcessExceptionEvent( DEBUG_EVENT *_de );
	void ProcessCreateThreadEvent( DEBUG_EVENT *_de );
	void ProcessCreateProcessEvent( DEBUG_EVENT *_de );
	void ProcessExitThreadEvent( DEBUG_EVENT *_de );
	void ProcessExitProcessEvent( DEBUG_EVENT *_de );
	void ProcessLoadDLLEvent( DEBUG_EVENT *_de );
	void ProcessUnloadDLLEvent( DEBUG_EVENT *_de );
	void ProcessOutputDebugStringEvent( DEBUG_EVENT *_de );
	void ProcessRIPEvent( DEBUG_EVENT *_de );
};


////////////////////////////////////////////////////////////////////////////////
/// @brief Start a process
/// @param _appPath The exe
/// @param _appArgs The CLI args
/// @param _workingDir The working directory
/// @param _pProcessInfoOut Where to put the process information
/// @return Whatever CreateProcess returned
int StartProcess
(
	std::string const& _appPath,
	std::string const& _appArgs,
	std::string const& _workingDir,
	PROCESS_INFORMATION* _pProcessInfoOut
)
{
#if DEBUG
	std::stringstream debug; 
	debug << "Path : " << _appPath << "\n\nArgs : " << _appArgs << "\r\nWorkingDir : " << _workingDir;
	MessageBox(0, debug.str().data(), "", 0);
#endif 

	*(flog) << "StartProcess\n";

	STARTUPINFO info = { sizeof(info) };

	// most apps expect the first arg to be the path/filename of the running application
	std::stringstream ostr;
	char* pExecutable = NULL;
#if CREATE_PROCESS_USES_SEPARATE_ARGS
	ostr << _appArgs;
	size_t pathLength = _appPath.length();
	pExecutable = new char[pathLength+1];
	memcpy(pExecutable, _appPath.data(), pathLength);
	pExecutable[pathLength] = 0;
#else
	ostr << _appPath << " " <<  _appArgs;
#endif

	// args have to be stored in a non const buffer
	size_t cmdLineArgsLength = ostr.str().length();
	char* pCmdLineArgs = new char[cmdLineArgsLength + 1]; 
	memcpy(pCmdLineArgs, ostr.str().data(), cmdLineArgsLength);
	pCmdLineArgs[cmdLineArgsLength] = 0;

	bool result = CreateProcess(pExecutable, pCmdLineArgs, 
		NULL, /*Processor attribs*/
		NULL, /*Thread attribs*/
		true, /*Inherit handles*/
		CREATE_NEW_CONSOLE|CREATE_SUSPENDED,    /*Creation flags*/ 
		NULL, /*environment*/ 
		_workingDir.empty() ? NULL : _workingDir.data(), 
		&info, _pProcessInfoOut) == TRUE;

	if (!result)
	{
		if(GetLastError() != ERROR_SUCCESS)
		{
			switch(GetLastError())
			{
			case ERROR_FILE_NOT_FOUND:
				{
					std::stringstream message;
					message << "Failed to find file application file : " << _appPath;
					MessageBox(NULL, (LPCSTR)(message.str().c_str()), (LPCSTR)"WatchDog", MB_OK );
					*(flog) << message.str().c_str() << "\n";
					*(flog) << "ERROR_FILE_NOT_FOUND\n"; 
					break;
				}

            case ERROR_DIRECTORY:
                {
                    std::stringstream message;
                    message << "Failed to find working directory : " << _workingDir;
                    MessageBox(NULL, (LPCSTR)(message.str().c_str()), (LPCSTR)"WatchDog", MB_OK );
                    *(flog) << message.str().c_str() << "\n";
                    *(flog) << "ERROR_DIRECTORY\n"; 
                    break;
                }
            
            case ERROR_PATH_NOT_FOUND :
				{
					std::stringstream message;
					message << "Failed to find file application path : " << _appPath;
					MessageBox(NULL, (LPCSTR)(message.str().c_str()), (LPCSTR)"WatchDog", MB_OK );
					*(flog) << message.str().c_str() << "\n";
					*(flog) << "ERROR_PATH_NOT_FOUND\n"; 
					break;
				}
			default:
				{
					std::stringstream message;
					message << "Unhandled error starting application : " << GetLastError();
					MessageBox(NULL, (LPCSTR)(message.str().c_str()), (LPCSTR)"WatchDog", MB_OK );
					*(flog) << message.str().c_str() << "\n";
					break;
				}
			}
		}
		else
		{
			*(flog) << "CreateProcess reported failure, but failure code is success.\n";
		}
	}

	delete[] pCmdLineArgs;
#if CREATE_PROCESS_USES_SEPARATE_ARGS
	delete[] pExecutable;
#endif

	return result;
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Generate a dump file for a crashed process
/// @param _path The dump file to create
/// @param _processHandle The process's handle
/// @param _processId The process's id
/// @param _threadID The thread's id
/// @param _pExceptionPointers Any exception pointers
/// @param _clientPointers Whether the pointers in _exceptionPointers are addresses this process or the client process
/// @return Success indicator (true = success; false = fail)
bool GenerateDump
(
	std::string const& _path,
	HANDLE _processHandle,
	DWORD _processId,
	int _threadID,
	EXCEPTION_POINTERS* _pExceptionPointers,
	bool _clientPointers
)
{
	*(flog) << "GenerateDump\n";

	HANDLE hDumpFile;
	MINIDUMP_EXCEPTION_INFORMATION expParam;

	hDumpFile = CreateFile(_path.data(), GENERIC_READ|GENERIC_WRITE, 
		FILE_SHARE_WRITE|FILE_SHARE_READ, 0, CREATE_ALWAYS, 0, 0);

	// if we were passed exception info, then use it
	if(_pExceptionPointers != 0)
	{
		expParam.ThreadId = _threadID;
		expParam.ExceptionPointers = _pExceptionPointers;
		expParam.ClientPointers = _clientPointers;
	}

	bool res = MiniDumpWriteDump(_processHandle, _processId, 
		hDumpFile, MiniDumpWithDataSegs, _pExceptionPointers != NULL ? &expParam : NULL, NULL, NULL) == TRUE;

	CloseHandle(hDumpFile);

	return res;
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Generate a dump and report it back
/// @param _hProcess Handle of process which exceptioned
/// @param _appPath The path which started the application
/// @param _cmdLine The application command line
/// @param _startTime The time the process started
/// @param _processId ID of process which exceptioned
/// @param _threadID ID of thread which exceptioned
/// @param _hMemoryMapFile The memory map file for the process
/// @param _exceptionPointers Pointers to exception information
/// @param _clientPointers Whether the pointers in _exceptionPointers are addresses this process or the client process
void GenerateAndReportDump
(
	HANDLE _hProcess,
	std::string const& _appPath,
	std::string const& _cmdLine,
	time_t _startTime,
	DWORD _processId,
	int _threadID,
	HANDLE _hMemoryMapFile,
	EXCEPTION_POINTERS *_exceptionPointers,
	bool _clientPointers
)
{
	// create a path in the temp directory with the DataTime as the name
	SYSTEMTIME stLocalTime;
	GetLocalTime(&stLocalTime);

	CHAR szTempDirectory[MAX_PATH]; 
	GetTempPath(MAX_PATH, szTempDirectory);

	CHAR szFileName[MAX_PATH]; 
	StringCchPrintf(szFileName, MAX_PATH, "%sFrontier.CrashReport.%02d.%02d.%04d-%02d.%02d.%02d.dmp", 
		szTempDirectory, stLocalTime.wDay, stLocalTime.wMonth, stLocalTime.wYear, 
		stLocalTime.wHour, stLocalTime.wMinute, stLocalTime.wSecond);

	*(flog) << "Exception occurred in ThreadID : " << _threadID << "\n";

	bool dumpGenerated = false;

	if ( _clientPointers )
	{
		// Client will have captured the exception and stored the process info ready to use

		if ( _hMemoryMapFile != INVALID_HANDLE_VALUE )
		{
			LPVOID pData = MapViewOfFile(_hMemoryMapFile, FILE_MAP_READ, 0, 0, 0);  
			if(pData != NULL)
			{
				MemoryDumpArgs* pArgs = (MemoryDumpArgs*)pData;

				if (pArgs->threadID == 0 && pArgs->pExceptionPtrs == 0 && strlen(pArgs->dumpfile) < MAX_PATH )
				{
					std::ifstream file(pArgs->dumpfile, std::ios::in | std::ios::binary);
					if (file.is_open())
					{
						std::cout << "Exception occurred, dump found at : " << pArgs->dumpfile << "\n";

						strcpy_s(szFileName, MAX_PATH, pArgs->dumpfile);
						dumpGenerated = true;
						file.close();
					}
				}

				if ( !dumpGenerated )
				{
					std::cout << "Exception occurred in ThreadID : " << pArgs->threadID << "\n";

					// generate the dump using the target apps Process/Thread/Exception info
					dumpGenerated = GenerateDump(szFileName, _hProcess, _processId, pArgs->threadID, pArgs->pExceptionPtrs, _clientPointers);
				}
				// clean up
				UnmapViewOfFile(pData);
			}
		}
	}
	else
	{
		dumpGenerated = GenerateDump( szFileName, _hProcess, _processId, _threadID, _exceptionPointers, _clientPointers );
	}

	// generate the dump using the target apps Process/Thread/Exception info
	if ( dumpGenerated )
	{
		time_t endTime = time(NULL);
		time_t correction = endTime - _startTime;

		std::stringstream ostr;
		ostr << "/DumpReport \"" << szFileName << "\" /ApplicationPath \"" << _appPath << "\" ";

		for (unsigned int ch=0; ch<_cmdLine.length(); ++ch)
		{
			switch (_cmdLine[ch])
			{
			case '\"':
				{
					ostr << "\\\"";
					break;
				}
			default :
				{
					ostr << _cmdLine[ch];
				}
			}
		}
		ostr << " /TimeCorrection " << correction;
#ifdef _WIN32
#ifdef _WIN64
		ostr << " /buildType " << "Win64";
#else
		ostr << " /buildType " << "Win32";
#endif
#endif


        // we want to get our module filename, to get the folder, to find the crash reporter
        char rawFilename[_MAX_PATH];
        char drive[_MAX_DRIVE];
        char directory[_MAX_DIR];
        rawFilename[0] = drive[0] = directory[0] = 0;
        ::GetModuleFileName ( NULL, rawFilename, _MAX_PATH );
        _splitpath_s ( rawFilename, drive, _MAX_DRIVE, directory, _MAX_DIR, NULL, 0, NULL, 0 );

        std::stringstream crashreporter;
        crashreporter << drive << directory << "CrashReporter.exe";

		PROCESS_INFORMATION processInfo;
		if(StartProcess(crashreporter.str().c_str(), ostr.str().c_str(), "", &processInfo)) // the crash reporter may not be in current dir
		{
			ResumeThread( processInfo.hThread );

			// cleanup
			CloseHandle(processInfo.hProcess);
			CloseHandle(processInfo.hThread);
		}
		else
		{
			MessageBox(0, "An exception occurred, but the CrashReporter was not found", "Error", 0);
		}
	}
}

std::string GetLogDirectory(std::string& application)
{
	std::stringstream dirname;
	dirname << application;
	dirname << ".";
	return dirname.str();
}

void OpenLog(std::string& application)
{
	//DebugBreak();
	std::string working = GetLogDirectory(application);
	if (working.length()>0)
	{
		// working directory exists.
		std::stringstream filename;
		filename << working;
		filename << "watchdog.log";

		try
		{
			flog = new std::ofstream(filename.str().c_str());
			*flog << "Opened log file.\n";
		}
		catch(...)
		{
			flog = &std::cout;
			MessageBox(0, "Failed to create watchdog log file.", "WatchDog", 0);
		}
	}
	else
	{
		MessageBox(0,"Not found", "Log", 0);
	}
}

void CloseLog()
{
	if (flog!=&std::cout)
	{
		*flog << "Closed log file.\n";
		flog->flush();
		delete flog;
		flog = &std::cout;
	}
}


////////////////////////////////////////////////////////////////////////////////
/// @brief read and hash a file
/// @param executable - name of the file
/// @return string - sha1 of file contents
std::string CalculateFileChecksum
(
    std::string executable
)
{
    std::string fileChecksum;
    const int buffsize = 256*1024;
    uint64 totalsize = 0;
    char* buffer = new char[buffsize];
    fSHA1 hasher;

    std::ifstream file ( executable, std::ios::in|std::ios::binary);
    if ( file.is_open() )
    {
        hasher.StreamStart();
        while(1)
        {
            file.read ( (char*)buffer, buffsize );
            int dataread = (int) file.gcount();
            totalsize += dataread;

            if ( dataread < buffsize )
            {
                hasher.StreamFinal( buffer, dataread, totalsize );
                break;
            }
            hasher.StreamBlock ( buffer, dataread );
        }

        file.close();
        fileChecksum.append ( hasher.ToString() );
    }

    delete[] buffer;
    return fileChecksum;
}


////////////////////////////////////////////////////////////////////////////////
/// @brief ReportChecksumFail
/// @param suppliedChecksum
/// @param fileChecksum
/// @param executable
void ReportChecksumFail
(
    std::string suppliedChecksum, 
    std::string fileChecksum, 
    std::string executable
)
{
    // write checksum error to log (we may remove this later!)
    *(flog) << "checksum error:" << suppliedChecksum << "::" << fileChecksum << "\n";

    // we'll need the time as part of the report
    time_t t;
    time( &t );
    uint64 epochTime = uint64(t);

    std::wstringstream urlpath;
    urlpath << '/' << 1 << '.' << 3 << "/watchdog/event" << '?' << "eventTime=" << epochTime << "&event=HashMismatch";

    // we will also need the time, machineid and authtoken
    std::stringstream telemetry;
    telemetry << "hash=" << fileChecksum << "&exe=" << executable;

    int len = telemetry.str().size();
    unsigned char* buff = new unsigned char[len];
    memcpy_s( buff, len, telemetry.str().data(), len );

    // we decided not to use the encrypted version, but I'll leave this in as obfuscation
    RC4Encrypt::Encrypt ( "HX863wRDd9C4265pQM6YZbvk355J8rJC", buff, len ); // if we do send encrypted data, we would need to base16 encode it

    bool secure = false;
    std::wstring url;

#if 0 // _DEBUG
    // report the error to the webserver 
    secure = false; // false: HTTP port 80
    url = L"eliteapi.onsrvdev1.corp.frontier.co.uk";
#else
    // report the error to the webserver 
    secure = true;
    url = L"api.orerve.net";
#endif

    // report the error to the webserver 
    SimpleHttpRequest request(L"Forc-Watchdog/1.0",secure); // true: HTTPS security
    request.SendRequest( url, L"POST", urlpath.str(), (void*) telemetry.str().data(), len );

    delete [] buff;
}


////////////////////////////////////////////////////////////////////////////////
/// @brief Hex-encode a string
/// @param _input
/// @param _len
/// @return std::string

std::string Base16Encode
( 
    char const * _input, 
    unsigned int _len 
)
{
    char const bitsToChar[16] = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
    std::string encStr;
    encStr.reserve( _len * 2 );	// For each byte, we write two characters.
    for(unsigned i = 0; i < _len; i++)
    {
        TCHAR currChar = _input[i];
        encStr.append( 1, bitsToChar[ (currChar & 0xF0) >> 4 ] );	// Most significant 4 bits
        encStr.append( 1, bitsToChar[  currChar & 0x0F       ] );	// Least significant 4 bits
    }

    return encStr;
}


////////////////////////////////////////////////////////////////////////////////
/// @brief Encrypt then base-16 encode a buffer
/// @param _args
/// @param szName
/// @return std::string encoded data
std::string EncryptAndB16
(
    void* _args, 
    unsigned _len,
    char const * _szKey
)
{
    TCHAR buff[4096];
    memcpy(buff,_args,_len);
    RC4Encrypt::Encrypt ( _szKey, (unsigned char *) buff, _len );

    return Base16Encode ( buff, _len );
}


////////////////////////////////////////////////////////////////////////////////
/// @brief Write the game's command-line params into shared memory, to hide from hackers
/// @param _pid - process id, used as part of the shared memory filename
/// @param _args - the args to save
/// @param o_hFile - filename handle
/// @param o_pMem - memory pointer
/// @return bool - success

bool PutArgsInSharedMemory
( 
    DWORD _pid, 
    void* _args, 
    unsigned _len,
    HANDLE* o_hFile, 
    LPVOID* o_pMem 
)
{
    TCHAR szName[256];
    sprintf_s ( szName, 256, "Local\\ED-%u-Wd", _pid );

    const DWORD bufSize = 4096;

    *o_hFile = CreateFileMapping(
        INVALID_HANDLE_VALUE,    // use paging file
        NULL,                    // default security
        PAGE_READWRITE,          // read/write access
        0,                       // maximum object size (high-order DWORD)
        bufSize,                 // maximum object size (low-order DWORD)
        szName);                 // name of mapping object

    if (*o_hFile == NULL)
    {
        *(flog) << "Could not create file mapping object:" << szName;
        return false;
    }
    
    *o_pMem = MapViewOfFile(*o_hFile,   // handle to map object
        FILE_MAP_ALL_ACCESS, // read/write permission
        0,
        0,
        bufSize);

    if (*o_pMem == NULL)
    {
        *(flog) << "Could not map view of file",

        CloseHandle(*o_hFile);
        *o_hFile = NULL;
        return false;
    }

    std::string encoded = EncryptAndB16( _args, _len, szName);

    strcpy_s ( (char*) *o_pMem, bufSize, encoded.data() );

    return true;
}


////////////////////////////////////////////////////////////////////////////////
/// @brief Clean up handles etc used to create the shared memory
/// @param _hFile
/// @param _pMem
/// @return void
void CleanupSharedMemory
( 
    HANDLE _hFile, 
    LPVOID _pMem 
)
{
    if ( _pMem )
    {
        UnmapViewOfFile(_pMem);
    }

    if ( _hFile )
    {
        CloseHandle(_hFile);
    }
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Start of WatchDog
/// @param argc How many args
/// @param argv The args
/// @return Return code
int main
(
	int argc,
	char* argv[]
)
{
	flog = &(std::cout);

	std::string cmdLine = GetCommandLine();
	std::string executable, executableArgs, workingDir, suppliedChecksum;

	std::vector<HANDLE> waitHandles;

	time_t startTime = time(NULL);
    bool bAttachDebugger = false;


	for (int i = 0; i < argc; ++i)
	{
		if(i + 1 < argc)
		{
			std::string key = argv[i];

			if (key == "/Executable")
			{
				executable = argv[i + 1];
			}
			else if (key == "/ExecutableArgs")
			{
				executableArgs = argv[i + 1];
			}
			else if (key == "/WorkingDirectory")
			{
				workingDir = argv[i + 1];
			}
            else if ( key == "/ExecutableHash")
            {
                suppliedChecksum = argv[i+1];
            }
#ifdef _DEBUG
            else if ( key == "/Debug" )
            {
                // sometimes it may be useful to launch the game from the watchdog, bit have the watchdog not connect to the game as a debugger
                // this is only supported in a debug build of the watchdog. Add "/Nodebug true" to the command line
                bAttachDebugger = true;
                // actually "/nodebug anything" works - it's just this parsing code expects every keyword to have a value
            }
#endif
		}
	}

	OpenLog(executable);
#ifdef _DEBUG
	std::stringstream debug;
	debug << "Executable : " << executable 
		  << "\r\nExecutableArgs : " << executableArgs 
		  << "\r\nWorkingDirectory : " << workingDir
          << "\r\nChecksum : " << suppliedChecksum
		  << "\r\nCMDLINE : " << cmdLine;
	MessageBox(0, debug.str().data(), "Info", 0);
#endif


    // calculate a checksum for the executable file, compare against the supplied checksum
    {
        std::string fileChecksum = CalculateFileChecksum(executable);

        if ( fileChecksum != suppliedChecksum )
        {
#ifndef _DEBUG
            ReportChecksumFail(suppliedChecksum, fileChecksum, executable);
            CloseLog();
            return 0;
#endif
        }
    }

	//create the heartbeat timer
	LPSECURITY_ATTRIBUTES timerSecurityAttributes = new SECURITY_ATTRIBUTES();
	memset(timerSecurityAttributes, 0, sizeof(SECURITY_ATTRIBUTES));
	timerSecurityAttributes->bInheritHandle = TRUE;
	HANDLE hHeartbeatTimer = CreateWaitableTimer(timerSecurityAttributes, TRUE, "Local\\EliteDangerousHeartbeatTimer");
	if (hHeartbeatTimer == INVALID_HANDLE_VALUE)
	{
		if (GetLastError() == ERROR_ALREADY_EXISTS)
		{
			*(flog) << "Heartbeat timer could not be started, timer already exists [" << GetLastError() << "]\n";
		}
		else
		{
			*(flog) << "Heartbeat timer could not be started [" << GetLastError() << "]\n";
		}
	}
	else
	{
		waitHandles.push_back( hHeartbeatTimer );
	}

	//create the 'I have crashed' signal
	HANDLE hIHaveCrashedSignal = NULL;
	if ( !bAttachDebugger )
	{
		std::stringstream signalNameStr;
		signalNameStr << "Local\\ED" << GetCurrentProcessId() << "HasCrashed";
		std::string signalName = signalNameStr.str();

		// args to make the events inheritable in child processes
		SECURITY_ATTRIBUTES eventAttribs;
		eventAttribs.nLength = sizeof(SECURITY_ATTRIBUTES);
		eventAttribs.lpSecurityDescriptor = NULL;
		eventAttribs.bInheritHandle = true;

		// our un-handled exception signal
		hIHaveCrashedSignal = CreateEvent(
			&eventAttribs,		// security attributes
			true,				// manual-reset event
			false,				// initial signal state
			signalName.data());    // object name

		// the event may already exist if an existing application is
		// open. We don't bother waiting in that case 
		if(hIHaveCrashedSignal == NULL || GetLastError() == ERROR_ALREADY_EXISTS)
		{
			std::stringstream message;
			message << "Call to CreateEvent failed : " << GetLastError();
			message << "\nCrash monitoring disabled.";
			MessageBox(NULL, (LPCSTR)(message.str().c_str()), (LPCSTR)"WatchDog", MB_OK );
		}
		else
		{
			waitHandles.push_back( hIHaveCrashedSignal );
		}
	}

    HANDLE hMemoryMapFile=NULL;
    LPVOID pSharedMemory=NULL;

    std::srand( (unsigned)startTime );
    unsigned randomNonce = std::rand();

    std::stringstream extendedArgs;
    extendedArgs << '"' << executable << "\" \"wseed " << randomNonce << "\" " << executableArgs ;

	// now start the target app, storing its process info so we can terminate it
	// in the event of an un-handled exception
	PROCESS_INFORMATION processInfo;
    // the command-line parameters here are encrypted purely for obfuscation purposes
	if(StartProcess(executable, extendedArgs.str().data(), workingDir, &processInfo))
	{
		waitHandles.push_back( processInfo.hProcess );

		// at this point, we wait for the target app to signal it has either closed
		// normally (process handle), .. *or* it has thrown an exception
		*(flog) << "waiting for event...\n";
		DWORD waitResult;

        unsigned hiddenargs[4];
        hiddenargs[0] = sizeof(hiddenargs);
        hiddenargs[1] = GetCurrentProcessId();
        hiddenargs[2] = processInfo.dwProcessId;
        hiddenargs[3] = randomNonce;
        PutArgsInSharedMemory( processInfo.dwProcessId, (void*)hiddenargs, sizeof(hiddenargs), &hMemoryMapFile, &pSharedMemory );

#ifdef _DEBUG
        if ( !bAttachDebugger )
#endif
        {
            ResumeThread( processInfo.hThread );

			waitResult = WaitForMultipleObjects( waitHandles.size(), waitHandles.data(), FALSE /*WaitOnAll*/, INFINITE /*Time out*/ );
        }
#ifdef _DEBUG
        else
        {
			// ensure debugging is in place before starting the process going
			ProcessDebugger pd( processInfo, executable, cmdLine, startTime );
			pd.WaitForDebuggerToAttach();
			ResumeThread( processInfo.hThread );

			waitResult = WaitForMultipleObjects( waitHandles.size(), waitHandles.data(), FALSE /*WaitOnAll*/, INFINITE /*Time out*/ );
		}
#endif
		*(flog) << "Event triggered!\n";

		switch (waitResult) 
		{ 
		case WAIT_OBJECT_0: 
		case WAIT_OBJECT_0+1: 
		case WAIT_OBJECT_0+2: 
		case WAIT_OBJECT_0+3: 
		case WAIT_OBJECT_0+4: 
		case WAIT_OBJECT_0+5: 
		case WAIT_OBJECT_0+6: 
		case WAIT_OBJECT_0+7: 
		case WAIT_OBJECT_0+8: 
		case WAIT_OBJECT_0+9: 
			{
				if ( waitResult - WAIT_OBJECT_0 <= waitHandles.size() )
				{
					HANDLE triggerHandle = waitHandles[waitResult - WAIT_OBJECT_0];

					if ( triggerHandle == processInfo.hProcess )
					{
						// process completed
						// do nothing, exit normally
						DWORD exitCode;
						if (GetExitCodeProcess(processInfo.hProcess, &exitCode))
						{
							*(flog) << "Target app exited cleanly with exit code " << exitCode << ".\n";
						}
						else
						{
							*(flog) << "Failed to obtain application exit code reported error " << GetLastError() << ".\n";
						}
					}
					else if ( triggerHandle == hHeartbeatTimer )
					{
						// heartbeat went off indicating game is stuck
						*(flog) << "Heartbeat timer reset failed.\n";
						GenerateAndReportDump(processInfo.hProcess, executable, cmdLine, startTime, GetProcessId(processInfo.hProcess), GetThreadId(processInfo.hThread), INVALID_HANDLE_VALUE, NULL, false);
					}
					else if ( triggerHandle == hIHaveCrashedSignal )
					{
						// game signaled crash
						// generate a dump report
						std::cout << "Target app threw an exception!\n";
						GenerateAndReportDump(processInfo.hProcess, executable, cmdLine, startTime, GetProcessId(processInfo.hProcess), GetThreadId(processInfo.hThread), hMemoryMapFile, NULL, true);

						// kill the target app (as it should be in an infinite sleep)
						TerminateProcess(processInfo.hProcess, 1);
					}
				}

				break; 
			}

		case WAIT_TIMEOUT:
			{
				// do nothing, exit normally?
				std::stringstream message;
				message << "Wait for application exit timed out.\n";
				MessageBox(NULL, (LPCSTR)(message.str().c_str()), (LPCSTR)"WatchDog", MB_OK );
				*(flog) << "Operation timed out!\n";
				break;
			}

		default: 
			{
				// do nothing, exit normally?
				std::stringstream message;
				message << "Unhandled error waiting for application exit : ";
				message << GetLastError();
				MessageBox(NULL, (LPCSTR)(message.str().c_str()), (LPCSTR)"WatchDog", MB_OK );
				*(flog) << "Wait error [" << GetLastError() << "]\n";
				break; 
			}
		}
        
        CleanupSharedMemory( hMemoryMapFile, pSharedMemory );

        // clean up
		CloseHandle(processInfo.hProcess);
		CloseHandle(processInfo.hThread);
		if (hHeartbeatTimer != INVALID_HANDLE_VALUE)
		{
			CloseHandle(hHeartbeatTimer);
		}
		delete timerSecurityAttributes;
	}

	CloseLog();
	return 0;
}




////////////////////////////////////////////////////////////////////////////////
/// @brief ctor
/// @param _pi The process information of the process to debug
ProcessDebugger::ProcessDebugger
(
	PROCESS_INFORMATION const& _pi,
	std::string const& _appPath,
	std::string const& _cmdLine,
	time_t _startTime

):
	m_pi(_pi),
	m_appPath(_appPath),
	m_cmdLine(_cmdLine),
	m_startTime(_startTime),
	m_stopNow(false),
	m_hDebugger(CreateThread( NULL, 0, &ProcessDebugger::DebugThread, this, 0, NULL )),
	m_hasReceivedCreateProcess(false),
	m_hasReceivedInitialBreakpoint(false),
	m_hasSignaledDebuggerAttached(false),
	m_hDebuggerAttachedEvent(CreateEvent(NULL, FALSE, FALSE, NULL))
{
}

////////////////////////////////////////////////////////////////////////////////
/// @brief dtor
ProcessDebugger::~ProcessDebugger
(
)
{
	m_stopNow = true;
	WaitForSingleObject( m_hDebugger, INFINITE );
	CloseHandle(m_hDebuggerAttachedEvent);
}

////////////////////////////////////////////////////////////////////////////////
void ProcessDebugger::WaitForDebuggerToAttach
(
)
{
	// wait for the debug thread to connect the debugger and receive the initial information about the game process
	// time-out after 15 seconds, just in case (for whatever reason) the process fails to send us the expected data
	WaitForSingleObject(m_hDebuggerAttachedEvent, 15 * 1000);
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Debug the process to prevent anybody else debugging he process
/// @param _parameter Parameter passed to CreateThread - must be ProcessDebugger *
/// @return Thread return code
DWORD WINAPI ProcessDebugger::DebugThread
(
	void *_parameter
)
{
	return reinterpret_cast<ProcessDebugger *>(_parameter)->DebugThread();
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Debug the process to prevent anybody else debugging he process
/// @param _parameter Parameter passed to CreateThread - must be PROCESS_INFORMATION const * of the process to debug
/// @return Thread return code
DWORD ProcessDebugger::DebugThread
(
)
{
	if ( !DebugActiveProcess(m_pi.dwProcessId) )
	{
		// failed to start debugging
		DWORD err = GetLastError();
		std::stringstream message;
		message << "Call to DebugActiveProcess failed : " << err;
		MessageBox(NULL, (LPCSTR)(message.str().c_str()), (LPCSTR)"WatchDog", MB_OK );
		SetEvent(m_hDebuggerAttachedEvent);
		return 1;
	}

	DEBUG_EVENT de;
	while(!m_stopNow)
	{
		if ( WaitForDebugEvent( &de, (DWORD)100 ) )
		{
			DWORD dwContinueStatus = DBG_EXCEPTION_NOT_HANDLED;
			// Debug event arrived
			switch(de.dwDebugEventCode)
			{
			case EXCEPTION_DEBUG_EVENT: dwContinueStatus = ProcessExceptionEvent( &de ); break;
			case CREATE_THREAD_DEBUG_EVENT: ProcessCreateThreadEvent( &de ); break;
			case CREATE_PROCESS_DEBUG_EVENT: ProcessCreateProcessEvent( &de ); break;
			case EXIT_THREAD_DEBUG_EVENT: ProcessExitThreadEvent( &de ); break;
			case EXIT_PROCESS_DEBUG_EVENT: ProcessExitProcessEvent( &de ); break;
			case LOAD_DLL_DEBUG_EVENT: ProcessLoadDLLEvent( &de ); break;
			case UNLOAD_DLL_DEBUG_EVENT: ProcessUnloadDLLEvent( &de ); break;
			case OUTPUT_DEBUG_STRING_EVENT: ProcessOutputDebugStringEvent( &de ); break;
			case RIP_EVENT: ProcessRIPEvent( &de ); break;
			}
			ContinueDebugEvent( de.dwProcessId, de.dwThreadId, dwContinueStatus );

			// after calling DebugActiveProcess, we will receive a bunch of CREATE_PROCESS_DEBUG_INFO, CREATE_THREAD_DEBUG_INFO and LOAD_DLL_DEBUG_EVENT events followed by a single EXCEPTION_DEBUG_EVENT to tell us that we're ready to go
			if(!m_hasSignaledDebuggerAttached && m_hasReceivedCreateProcess && m_hasReceivedInitialBreakpoint)
			{
				SetEvent(m_hDebuggerAttachedEvent);
				m_hasSignaledDebuggerAttached = true;
			}
		}
		else
		{
			// timeout - perform any background actions
		}
	}

	return 0;
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Exception event arrived from watched process
/// @param _de The event details
DWORD ProcessDebugger::ProcessExceptionEvent
(
	DEBUG_EVENT *_de
)
{
	// nothing to do (probably)
#if DEBUG_DEBUGGING
	*(flog) << "Exception " << _de->u.Exception.ExceptionRecord.ExceptionCode << "\n";
#endif

	if ( _de->u.Exception.dwFirstChance )
	{
		// Breakpoint exceptions are treated as immediate crashes, unless its the first one, in which case it's OK, as the runtime generates it
		// Other exceptions are permitted to be processed by the application's exception processing first.
		if( _de->u.Exception.ExceptionRecord.ExceptionCode != EXCEPTION_BREAKPOINT )
		{
			return DBG_EXCEPTION_NOT_HANDLED;
		}
		else
		{
			m_hasReceivedInitialBreakpoint = true;
			return DBG_CONTINUE;
		}
	}

	// Second chance exception or breakpoints
	// With second chance exceptions the application's exception handing hasn't consumed the exception so the application is now, officially, dead
	// With breakpoints, we shouldn't be there, so generate a crash dump
	HANDLE hProcess = OpenProcess( PROCESS_ALL_ACCESS, false, _de->dwProcessId );
	if ( hProcess )
	{
		HANDLE hThread = OpenThread( THREAD_ALL_ACCESS, false, _de->dwThreadId );
		if ( hThread )
		{
			CONTEXT threadContext;
			memset(&threadContext, 0, sizeof(threadContext));
			threadContext.ContextFlags = CONTEXT_FULL;
			if ( GetThreadContext( hThread, &threadContext ) != 0 )
			{
				// got the thread context

				EXCEPTION_POINTERS exceptionPointers;
				exceptionPointers.ExceptionRecord = &_de->u.Exception.ExceptionRecord;
				exceptionPointers.ContextRecord = &threadContext;

				GenerateAndReportDump( hProcess, m_appPath, m_cmdLine, m_startTime, _de->dwProcessId, 
					_de->dwThreadId, INVALID_HANDLE_VALUE, &exceptionPointers, false );
			}
			else
			{
				DWORD err = GetLastError();
				*(flog) << "Failed to get thread content due to " << err << "\n";
			}

			CloseHandle( hThread );
		}

		TerminateProcess( hProcess, 1 );

		CloseHandle( hProcess );
	}

	return DBG_EXCEPTION_NOT_HANDLED;
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Create Thread event arrived from watched process
/// @param _de The event details
void ProcessDebugger::ProcessCreateThreadEvent
(
	DEBUG_EVENT *_de
)
{
	// nothing to do
#if DEBUG_DEBUGGING
	*(flog) << "Create Thread " << _de->u.CreateThread.hThread << "\n";
#endif
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Create Process event arrived from watched process
/// @param _de The event details
void ProcessDebugger::ProcessCreateProcessEvent
(
	DEBUG_EVENT *_de
)
{
#if DEBUG_DEBUGGING
	*(flog) << "Create Process " << _de->u.CreateProcessInfo.lpImageName << "\n";
#endif
	CloseHandle( _de->u.CreateProcessInfo.hFile );
	m_hasReceivedCreateProcess = true;
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Process Exit event arrived from watched process
/// @param _de The event details
void ProcessDebugger::ProcessExitThreadEvent
(
	DEBUG_EVENT *_de
)
{
	// nothing to do
#if DEBUG_DEBUGGING
	*(flog) << "Exit Thread " << _de->dwThreadId << " rc=" << _de->u.ExitThread.dwExitCode << "\n";
#endif
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Exit Process event arrived from watched process
/// @param _de The event details
void ProcessDebugger::ProcessExitProcessEvent
(
	DEBUG_EVENT *_de
)
{
	// nothing to do
#if DEBUG_DEBUGGING
	*(flog) << "Exit Process " << _de->dwProcessId << " rc=" << _de->u.ExitProcess.dwExitCode << "\n";
#endif
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Load DLL event arrived from watched process
/// @param _de The event details
void ProcessDebugger::ProcessLoadDLLEvent
(
	DEBUG_EVENT *_de
)
{
#if DEBUG_DEBUGGING
	char modName[1024];
	GetFinalPathNameByHandle( _de->u.LoadDll.hFile, modName, sizeof(modName), FILE_NAME_OPENED );
	*(flog) << "Load DLL " << modName << " at " << std::hex << _de->u.LoadDll.lpBaseOfDll << "\n";
#endif
	CloseHandle( _de->u.LoadDll.hFile );
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Unload DLL event arrived from watched process
/// @param _de The event details
void ProcessDebugger::ProcessUnloadDLLEvent
(
	DEBUG_EVENT *_de
)
{
#if DEBUG_DEBUGGING
	*(flog) << "Unload DLL from " << std::hex << _de->u.UnloadDll.lpBaseOfDll << "\n";
#endif
	// nothing to do
}

////////////////////////////////////////////////////////////////////////////////
/// @brief Output debug string event arrived from watched process
/// @param _de The event details
void ProcessDebugger::ProcessOutputDebugStringEvent
(
	DEBUG_EVENT *_de
)
{
#if DEBUG_DEBUGGING && 0
	if ( !_de->u.DebugString.fUnicode )
	{
		char *copyOfString = new char[_de->u.DebugString.nDebugStringLength+1];
		SIZE_T numberRead;
		ReadProcessMemory( m_pi.hProcess, _de->u.DebugString.lpDebugStringData, copyOfString, _de->u.DebugString.nDebugStringLength, &numberRead );
		copyOfString[numberRead] = 0;
		*(flog) << copyOfString;
		delete[] copyOfString;
	}
#endif
	// nothing to do
}

////////////////////////////////////////////////////////////////////////////////
/// @brief RIP event arrived from watched process
/// @param _de The event details
void ProcessDebugger::ProcessRIPEvent
(
	DEBUG_EVENT *_de
)
{
#if DEBUG_DEBUGGING
	*(flog) << "RIP " << _de->u.RipInfo.dwError << "\n";
#endif
	// nothing to do
}
