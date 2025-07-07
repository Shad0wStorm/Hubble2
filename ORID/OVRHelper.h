#pragma once

#include "stdafx.h"

struct ovrMessage;

#include <string>
#include <vector>

class Options;

//-----------------------------------------------------------------------
//! @brief Wrap the low level ovr interface to expose the minimal
//! features required.
//-----------------------------------------------------------------------
class OVRHelper
{
public:
	OVRHelper(Options* options);
	~OVRHelper(void);

	bool IsEntitled();
	bool RequiresRegistration();
	const std::wstring& UserAccessToken() const { return m_userAccessToken; }
	const std::wstring& Error() const { return m_error; }
	const std::wstring& User() const { return m_user; }
	const std::wstring& Nonce() const { return m_nonce; }
private:
	void RequestProductList();
	void RequestPurchases();
	void RequestPurchaseList();
	void RequestRegistrationInfo();
	void ProcessExpectedOVRMessages();
	void ProcessOVRMessages();
	void ProcessUserNonceMessage(ovrMessage* message);
	void ProcessLoggedInUserMessage(ovrMessage* message);
	void ProcessUserAccessTokenMessage(ovrMessage* message);
	void ProcessProductListMessage(ovrMessage* message);
	void ProcessPurchaseListMessage(ovrMessage* message);
	void ProcessStartCheckoutMessage(ovrMessage* message);
	void ProcessEntitlementCheckMessage(ovrMessage* message);
	bool IsRegistrationRequired(std::wstring const& sku);

	Options* m_options;
	std::vector<std::wstring> m_products;
	std::wstring m_error;
	std::wstring m_user;
	std::wstring m_userAccessToken;
	std::wstring m_nonce;
	unsigned int m_state;
	unsigned int m_targetState;
	bool m_valid;
	bool m_entitled;
};

