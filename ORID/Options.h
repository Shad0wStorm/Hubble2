#pragma once

#include "stdafx.h"
#include <string>
#include <vector>

//-----------------------------------------------------------------------
//! @brief Process and store the command line options.
//-----------------------------------------------------------------------
class Options
{
public:
	Options(void);
	~Options(void);

	void Process(int argc, char* argv[]);

	enum ApplicationID {
		AID_Unknown,
		AID_EliteDangerous,
		AID_EliteDangerousArena,
		AID_EDIntegrationTest
	};

	ApplicationID GetApplicationID() const { return m_applicationID; }
	std::wstring GetOculusApplicationID() const { return m_oculusApplicationID; }
	std::wstring GetExecutable() const { return m_executable; }
	std::wstring GetRegistrationLink() const { return m_registrationLink;}
	int GetAdditionalCount() const { return m_passThrough.size(); }
	std::wstring GetAdditionalOption(int option) const;

	bool RegistrationRequired(const _TCHAR* sku);
	
	std::wstring GetForcedRegistration() const;

	std::wstring GetNextPurchase();

	bool HasAppID() const { return m_appID; }

	static std::string MBSConvert(std::wstring const& source);
	static std::wstring WCSConvert(const char* source);

private:
	void SetupEliteDangerous();
	void SetupEDArena();
	void SetupEDIntegrationTest();
	void SetupExecutableProperties(const char* path);
	void MakeTargetApplicationAbsolute(std::wstring const& target);
	void AddOption(std::wstring const& option);
	void AddPurchase(std::wstring const& sku);
	std::vector<std::wstring> m_passThrough;
	std::vector<std::wstring> m_requireRegistration;
	std::vector<std::wstring> m_purchase;
	ApplicationID m_applicationID;
	std::wstring m_executable;
	std::wstring m_registrationLink;
	std::wstring m_application;
	std::wstring m_fullTarget;
	// This is a non wide string, because the oculus platform initialisation
	// requires a char* even though it only compiles with unicode enabled.
	std::wstring m_oculusApplicationID;
	bool m_appID;
	bool m_allowPurchase;
};

