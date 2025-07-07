#include "Windows.h"
#include "Options.h"

#include <sstream>

Options::Options(void)
{
	m_applicationID = AID_Unknown;
	m_allowPurchase = false;
}


Options::~Options(void)
{
}

//-----------------------------------------------------------------------
//! @brief Process the command line arguments.
//!
//! Extract the known parameters into values.
//!
//! Extract unknown parameters into a pass through array so they are
//! forwarded on to the target process.
//!
//! Known parameters must appear before any unknown parameters or they
//! will be treated as unknown.
//!
//! Given the more complex processing the application requires compared
//! to the original plan we now use a different approach to options.
//!
//! We expect to be run with an application code which we use to
//! determine the set of options to activate rather than requiring they
//! be specified on the command line every time. This means a new build
//! when the options change, but we do not expect them to change
//! frequently. In practice we would need to update the application to
//! pass the new set of options, so it is not significantly more work to
//! update this executable instead, and the aim is for the launcher
//! itself to not need modification when this pre-launcher changes.
//!
//! Another approach would be to request the parameters from the FORC
//! servers allowing them to be modified remotely which would make it
//! easier to add new products requiring registration without updating
//! command line options or code. The information would obviously need
//! to be available without logging in to FORC to remove a circular
//! dependency.
//-----------------------------------------------------------------------
void Options::Process(int argc, char* argv[])
{
	int current = 1;

	// Do not trust argv[0] to be the full path, ask the OS who we are.
	_TCHAR pathTemp[MAX_PATH];
	GetModuleFileName(NULL, pathTemp, MAX_PATH);
	m_application = pathTemp;

	m_appID = false;
	m_allowPurchase = false;

	while (current<argc)
	{
		if ((GetAdditionalCount()==0) && (current<(argc+1)))
		{
			if (_stricmp(argv[current],"EDI")==0)
			{
				SetupEDIntegrationTest();
				++current;
				continue;
			}
			if (_stricmp(argv[current],"EDA")==0)
			{
				SetupEDArena();
				++current;
				continue;
			}
			if (_stricmp(argv[current],"ED")==0)
			{
				SetupEliteDangerous();
				++current;
				continue;
			}
			if (_stricmp(argv[current],"/purchase")==0)
			{
				AddPurchase(WCSConvert(argv[++current]));
				++current;
				continue;
			}
			if (_stricmp(argv[current],"/executable")==0)
			{
				SetupExecutableProperties(argv[++current]);
				++current;
				continue;
			}
			if (_stricmp(argv[current],"/appid")==0)
			{
				m_oculusApplicationID = WCSConvert(argv[++current]);
				++current;
				continue;
			}
			if (_stricmp(argv[current],"/registration")==0)
			{
				m_registrationLink = WCSConvert(argv[++current]);
				m_appID = true;
				++current;
				continue;
			}
		}
		AddOption(WCSConvert(argv[current++]));
	}
}

//-----------------------------------------------------------------------
//! @brief Return the contents of the indicated additional option.
//-----------------------------------------------------------------------
std::wstring Options::GetAdditionalOption(int option) const
{
	if (option<GetAdditionalCount())
	{
		return m_passThrough[option];
	}
	return nullptr;
}

//-----------------------------------------------------------------------
//! @brief Check whether the given SKU is on the list of those requiring
//! registration.
//-----------------------------------------------------------------------
bool Options::RegistrationRequired(const _TCHAR* sku)
{
	std::wstring match(sku);
	for (unsigned int m=0; m<m_requireRegistration.size(); ++m)
	{
		if (match==m_requireRegistration[m])
		{
			return true;
		}
	}
	return false;
}

//-----------------------------------------------------------------------
//! @brief Return a string indicating the registration keys explicitly
//! set as required.
//-----------------------------------------------------------------------
std::wstring Options::GetForcedRegistration() const
{
	std::wstring forced;
	for (unsigned int m=0; m<m_requireRegistration.size(); ++m)
	{
		if (forced.size()>0)
		{
			forced = forced + _T("|");
		}
		forced = forced + m_requireRegistration[m];
	}
	return forced;
}

//-----------------------------------------------------------------------
//! @brief Return the next purchase to make from the purchase list.
//!
//! returns empty string if there are no purchases remaining.
//-----------------------------------------------------------------------
std::wstring Options::GetNextPurchase()
{
	std::wstring result;
	if (m_purchase.size()>0)
	{
		result = m_purchase.back();
		m_purchase.pop_back();
	}
	return result;
}

//-----------------------------------------------------------------------
//! @brief Setup the default options for EliteDangerous
//-----------------------------------------------------------------------
void Options::SetupEliteDangerous()
{
	m_applicationID = AID_EliteDangerous;
	m_oculusApplicationID = _T("988773191157765");
	SetupExecutableProperties("EDLaunch.exe");
	m_registrationLink = _T("https://user.frontierstore.net/oculus/register");
}

//-----------------------------------------------------------------------
//! @brief Setup the default options for Elite Dangerous: Arena
//!
//! NOTE This currently uses the AppID for the integration test as no
//! arena has been set up yet. When the correct ID is setup, along with
//! the correct registration link, ensure the ability to add purchases
//! is removed.
//-----------------------------------------------------------------------
void Options::SetupEDArena()
{
	m_applicationID = AID_EliteDangerousArena;

	// These should be set to the correct values when available.
	m_oculusApplicationID = _T("893573177407649");
	SetupExecutableProperties("EDLaunch.exe");
	m_registrationLink = _T("https://user.frontierstore.net/oculus/register");
}

//-----------------------------------------------------------------------
//! @brief Setup the default options for EDIntegrationTest
//-----------------------------------------------------------------------
void Options::SetupEDIntegrationTest()
{
	m_applicationID = AID_EDIntegrationTest;
	m_oculusApplicationID = _T("1100544779976900");
	SetupExecutableProperties("EDLaunch.exe");
	m_registrationLink = _T("https://user.frontierstore.net/oculus/register");
	m_allowPurchase = true;
}

//-----------------------------------------------------------------------
//! @brief Setup the Default properties for the chained executable given
//! a base path.
//-----------------------------------------------------------------------
void Options::SetupExecutableProperties
(
	const char* path
)
{
	m_executable = WCSConvert(path);
	if (path[1]!=':')
	{
		MakeTargetApplicationAbsolute(m_executable);
	}
	else
	{
		m_fullTarget = m_executable;
	}
}

//-----------------------------------------------------------------------
//! @brief Make a relative path to the executable absolute.
//-----------------------------------------------------------------------
void Options::MakeTargetApplicationAbsolute(std::wstring const& target)
{
	int applicationDirEnd = m_application.length();
	while (m_application[applicationDirEnd]!='\\')
	{
		--applicationDirEnd;
	}
	++applicationDirEnd;
	m_fullTarget = m_application.substr(0, applicationDirEnd);
	m_fullTarget.append(target);
	m_executable = m_fullTarget;
}

//-----------------------------------------------------------------------
//! @brief Add the given option to the end of the list of available
//! options.
//-----------------------------------------------------------------------
void Options::AddOption(std::wstring const& option)
{
	m_passThrough.push_back(option);
}

void Options::AddPurchase(std::wstring const& sku)
{
	// Only allow purchases if they are enabled for the configured application.
	if (m_allowPurchase)
	{
		m_purchase.push_back(sku);
	}
}

//-----------------------------------------------------------------------
//! @brief Helper method for converting to std::wstring used in unicode
//! applications to std::string required by platform.
//-----------------------------------------------------------------------
std::string Options::MBSConvert(std::wstring const& source)
{
	size_t bufferSize = (source.size()*2)+1;
	std::string result;
	char* applicationID = new char[bufferSize];
	size_t used = 0;
	wcstombs_s(&used, applicationID, bufferSize, source.c_str(), bufferSize);
	result = applicationID;
	delete[] applicationID;
	return result;
}

//-----------------------------------------------------------------------
//! @brief Convert a character array to a wide string.
//!
//! Probably not efficient but sufficient for the amount of text we
//! expect to convert.
//-----------------------------------------------------------------------
std::wstring Options::WCSConvert(const char* source)
{
	std::wstringstream output;
	output << source;
	return output.str();
}
