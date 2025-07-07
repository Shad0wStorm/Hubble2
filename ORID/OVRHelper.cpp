#include "OVRHelper.h"

#include "OVR_Platform.h"

#include "Options.h"

#include <sstream>

enum HelperActions
{
	BuyPurchases,
	GetProducts,
	GetPurchases,
	GetUserProof,
	GetUserIdentity,
	GetUserAccessToken,
	GetEntitlement,
	ACTION_MAX
};

//-----------------------------------------------------------------------
//! @brief Initialise access to the platform library.
//!
//! Currently we only know how to do this given a pre-existing user token
//! which must be obtained from the web site. This is not suitable for
//! end user use.
//-----------------------------------------------------------------------
OVRHelper::OVRHelper(Options* options)
{
	m_valid = false;

	m_options = options;

	std::string applicationID = Options::MBSConvert(options->GetOculusApplicationID());

	ovrPlatformInitializeResult result =  ovr_PlatformInitializeWindows(applicationID.c_str());

	m_targetState = 0;

	switch (result)
	{
	case ovrPlatformInitialize_Success:
		{
			if (!IsEntitled())
			{
				std::wstring eterror = m_error;
				m_error = _T("User does not have application entitlement: ");
				m_error += eterror;
			}
			else
			{
				m_state = 0;
				m_valid = true;
			}
			break;
		}
	case ovrPlatformInitialize_Uninitialized:
		{
			m_error = _T("Uninitialised");
			break;
		}
	case ovrPlatformInitialize_PreLoaded:
		{
			m_error = _T("Preloaded");
			break;
		}
	case ovrPlatformInitialize_FileInvalid:
		{
			m_error = _T("File Invalid");
			break;
		}
	case ovrPlatformInitialize_SignatureInvalid:
		{
			m_error = _T("Signature Invalid");
			break;
		}
	case ovrPlatformInitialize_UnableToVerify:
		{
			m_error = _T("Unable to verify");
			break;
		}
	default:
		{
			std::wstringstream ss;
			ss << "Unrecognised initialisation result: ";
			ss << (unsigned int)result;
			m_error = ss.str();
			break;
		}
	}
}

//-----------------------------------------------------------------------
//! @brief Release resources.
//!
//! Clear allocated memory for any calls made.
//! There is no corresponding platform shutdown to call.
//-----------------------------------------------------------------------
OVRHelper::~OVRHelper(void)
{
}

//-----------------------------------------------------------------------
//! @brief Check whether the user has application entitlement.
//!
//! Required locally as of 1.0 since ovr_IsEntitle has been removed.
//-----------------------------------------------------------------------
bool OVRHelper::IsEntitled()
{
	unsigned int previousState = m_state;
	unsigned int previousTarget = m_targetState;

	ovr_Entitlement_GetIsViewerEntitled();
	m_state = 0;
	m_targetState = (1 << GetEntitlement);
	m_entitled = false;
	ProcessExpectedOVRMessages();
	m_state = previousState;
	m_targetState = previousTarget;
	return m_entitled;
}

//-----------------------------------------------------------------------
//! @brief Determine whether a new product has been registered requiring
//! the product to be registered on the ED Store.
//-----------------------------------------------------------------------
bool OVRHelper::RequiresRegistration()
{
	unsigned int initialTarget = 0;

	if (m_valid)
	{
		RequestProductList();

		initialTarget = m_targetState;

		ProcessExpectedOVRMessages();
	}
	return (m_state & (1<<GetUserAccessToken)) != 0;
}

//-----------------------------------------------------------------------
//! @brief Make any requested purchases through the store.
//-----------------------------------------------------------------------
void OVRHelper::RequestPurchases()
{
	std::string purchase = Options::MBSConvert(m_options->GetNextPurchase());
	if (purchase.length()>0)
	{
		ovrRequest request = ovr_IAP_LaunchCheckoutFlow(purchase.c_str());
	}
	else
	{
		RequestPurchaseList();
		m_state |= 1 << BuyPurchases;
	}
	m_targetState |= 1 << BuyPurchases;
}

//-----------------------------------------------------------------------
//! @brief Request all key products from the server.
//-----------------------------------------------------------------------
void OVRHelper::RequestProductList()
{
	ovrRequest request;
	const char* keys = "";
	request = ovr_IAP_GetProductsBySKU(&keys,1);
	m_targetState = (1 << GetProducts);
}


//-----------------------------------------------------------------------
//! @brief Request the purchase list from Oculus.
//!
//! Ensure we update the target state to reflect the messages that need
//! to arrive before continuing.
//-----------------------------------------------------------------------
void OVRHelper::RequestPurchaseList()
{
	ovrRequest request;
	request = ovr_IAP_GetViewerPurchases();
	m_targetState = (1 << GetPurchases);
}

//-----------------------------------------------------------------------
//! @brief Request the additional information required to register a
//! set of products with the Elite Store.
//!
//! Ensure we update the target state to reflect the messages that need
//! to arrive before continuing.
//-----------------------------------------------------------------------
void OVRHelper::RequestRegistrationInfo()
{
	ovrRequest request;
	request = ovr_User_GetUserProof();
	request = ovr_User_GetLoggedInUser();
	request = ovr_User_GetAccessToken();

	m_targetState |= ((1<<GetUserAccessToken) | (1<<GetUserIdentity) | (1<<GetUserProof));
}

//-----------------------------------------------------------------------
//! @brief Process messages until the current state matches the expected
//! state.
//-----------------------------------------------------------------------
void OVRHelper::ProcessExpectedOVRMessages()
{
	while ((m_state & m_targetState)!=m_targetState)
	{
		ProcessOVRMessages();
	}
}

//-----------------------------------------------------------------------
//! @brief Process any messages on the OVR message queue.
//-----------------------------------------------------------------------
void OVRHelper::ProcessOVRMessages()
{
	ovrMessage* message = nullptr;

	while ((message=ovr_PopMessage())!=nullptr)
	{
		ovrMessageType mt = ovr_Message_GetType(message);
		switch(mt)
		{
		case ovrMessage_User_GetUserProof:
			{
				ProcessUserNonceMessage(message);
				break;
			}
		case ovrMessage_User_GetLoggedInUser:
			{
				ProcessLoggedInUserMessage(message);
				break;
			}
		case ovrMessage_User_GetAccessToken:
			{
				ProcessUserAccessTokenMessage(message);
				break;
			}
		case ovrMessage_IAP_GetProductsBySKU:
			{
				ProcessProductListMessage(message);
				break;
			}
		case ovrMessage_IAP_GetViewerPurchases:
			{
				ProcessPurchaseListMessage(message);
				break;
			}
		case ovrMessage_IAP_LaunchCheckoutFlow:
			{
				ProcessStartCheckoutMessage(message);
				break;
			}
		case ovrMessage_Entitlement_GetIsViewerEntitled:
			{
				ProcessEntitlementCheckMessage(message);
				break;
			}
		default:
			{
				// Should we care?
				break;
			}
		}
		ovr_FreeMessage(message);
	}
}

//-----------------------------------------------------------------------
//! @brief Determine the text of the UserProof needed to confirm the
//! users identity via the RestAPI.
//-----------------------------------------------------------------------
void OVRHelper::ProcessUserNonceMessage(ovrMessage* message)
{
	if (!ovr_Message_IsError(message))
	{
		ovrUserProof* nonce = ovr_Message_GetUserProof(message);
		const char* nonceText = ovr_UserProof_GetNonce(nonce);
		m_nonce = Options::WCSConvert(nonceText);
	}
	else
	{
		const ovrErrorHandle error = ovr_Message_GetError(message);
		m_error = Options::WCSConvert(ovr_Error_GetMessage(error));
		m_targetState = 0;
	}
	m_state |= 1 << GetUserProof;
}

//-----------------------------------------------------------------------
//! @brief Extract useful information about the logged in user.
//-----------------------------------------------------------------------
void OVRHelper::ProcessLoggedInUserMessage(ovrMessage* message)
{
	if (!ovr_Message_IsError(message))
	{
		unsigned short data = ((unsigned short*)message)[1];
		ovrUser* user = ovr_Message_GetUser(message);
		unsigned int messagePtr = (unsigned int)message;
		unsigned int userPtr = (unsigned int)user;
		unsigned short difference = userPtr-messagePtr;
		const char* userIDName = ovr_User_GetOculusID(user);
		m_user = Options::WCSConvert(userIDName);
	}
	else
	{
		const ovrErrorHandle error = ovr_Message_GetError(message);
		m_error = Options::WCSConvert(ovr_Error_GetMessage(error));
		m_targetState = 0;
	}
	m_state |= 1 << GetUserIdentity;
}

//-----------------------------------------------------------------------
//! @brief Get a user access token for the logged in user and the
//! current application.
//-----------------------------------------------------------------------
void OVRHelper::ProcessUserAccessTokenMessage(ovrMessage* message)
{
	if (!ovr_Message_IsError(message))
	{
		const char *uat = ovr_Message_GetString(message);
		m_userAccessToken = Options::WCSConvert(uat);
	}
	else
	{
		const ovrErrorHandle error = ovr_Message_GetError(message);
		m_error = Options::WCSConvert(ovr_Error_GetMessage(error));
		m_targetState = 0;
	}
	m_state |= 1 << GetUserAccessToken;
}

//-----------------------------------------------------------------------
//! @brief Get the list of matching products.
//-----------------------------------------------------------------------
void OVRHelper::ProcessProductListMessage(ovrMessage* message)
{
	if (!ovr_Message_IsError(message))
	{
		std::string key("KEY_");
		ovrProductArrayHandle products = ovr_Message_GetProductArray(message);
		for (unsigned int i=0; i<ovr_ProductArray_GetSize(products); ++i)
		{
			ovrProductHandle item = ovr_ProductArray_GetElement(products, i);

			std::string sku = ovr_Product_GetSKU(item);
			if (sku.compare(0, key.length(), key)==0)
			{
				m_products.push_back(Options::WCSConvert(sku.c_str()));
			}
		}
	}
	else
	{
		const ovrErrorHandle error = ovr_Message_GetError(message);
		m_error = Options::WCSConvert(ovr_Error_GetMessage(error));
		m_targetState = 0;
	}
	// Have the list of products, so figure out if the user has purchased any.
	RequestPurchaseList();
	m_state |= 1 << GetProducts;
}

//-----------------------------------------------------------------------
//! @brief Process the list of purchased products to see if there are
//! any which require registration.
//-----------------------------------------------------------------------
void OVRHelper::ProcessPurchaseListMessage(ovrMessage* message)
{
	if (!ovr_Message_IsError(message))
	{
		ovrPurchaseArrayHandle purchases = ovr_Message_GetPurchaseArray(message);
		bool registration = false;
		for (unsigned int i=0; i<ovr_PurchaseArray_GetSize(purchases); ++i)
		{
			ovrPurchaseHandle item = ovr_PurchaseArray_GetElement(purchases, i);

			const char* sku = ovr_Purchase_GetSKU(item);
			registration = registration || IsRegistrationRequired(Options::WCSConvert(sku));
		}
		if ((m_state&(1<<BuyPurchases))==0)
		{
			// Did not attempt to buy the purchase items, so instead
			// treat them as items we should pretend we have for testing
			// purposes.
			std::wstring purchase = m_options->GetNextPurchase();
			while (purchase.length()>0)
			{
				registration = registration || IsRegistrationRequired(purchase);
				purchase = m_options->GetNextPurchase();
			}
		}
		if (registration)
		{
			RequestRegistrationInfo();
		}
	}
	else
	{
		const ovrErrorHandle error = ovr_Message_GetError(message);
		m_error = Options::WCSConvert(ovr_Error_GetMessage(error));
		m_targetState = 0;
	}
	m_state |= 1 << GetPurchases;
}

//-----------------------------------------------------------------------
//! @brief Process the results purchasing an item, maybe.
//-----------------------------------------------------------------------
void OVRHelper::ProcessStartCheckoutMessage(ovrMessage* message)
{
	if (!ovr_Message_IsError(message))
	{
		ovrPurchaseHandle item = ovr_Message_GetPurchase(message);
		const char* sku = ovr_Purchase_GetSKU(item);
	}
	else
	{
		const ovrErrorHandle error = ovr_Message_GetError(message);
		m_error = Options::WCSConvert(ovr_Error_GetMessage(error));
		m_targetState = 0;
	}
	// Queue any additional required purchases
	RequestPurchases();
}

//-----------------------------------------------------------------------
//! @brief Process the results of the entitlement check message.
//-----------------------------------------------------------------------
void OVRHelper::ProcessEntitlementCheckMessage(ovrMessage* message)
{
	if (!ovr_Message_IsError(message))
	{
		m_entitled = true;
	}
	else
	{
		m_entitled = false;
		const ovrErrorHandle error = ovr_Message_GetError(message);
		m_error = Options::WCSConvert(ovr_Error_GetMessage(error));
		m_targetState = 0;
	}
	m_state |= (1 << GetEntitlement);
}

//-----------------------------------------------------------------------
//! @brief Test whether registration is required for the given SKU.
//-----------------------------------------------------------------------
bool OVRHelper::IsRegistrationRequired(std::wstring const& sku)
{
	for (unsigned int p = 0; p<m_products.size(); ++p)
	{
		if (m_products[p]==sku)
		{
			return true;
		}
		else
		{
			if (m_options->RegistrationRequired(sku.c_str()))
			{
				return true;
			}
		}
	}
	return false;
}
