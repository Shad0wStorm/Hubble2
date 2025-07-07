/*----------------------------------------------------------------------------
 *  FILE: SimpleHttp.h
 *
 *		Copyright(c) 2014 Frontier Developments Ltd.
 *
 *		26-09-2014 HRC Written
 *
 *----------------------------------------------------------------------------
 */


#include <string>
#include <vector>

class SimpleHttpRequest
{
private:
    std::wstring m_userAgent;

public:
    SimpleHttpRequest(const std::wstring&, bool _secure);
    bool SendRequest(const std::wstring&, const std::wstring&, const std::wstring&, void*, DWORD);
    std::wstring m_responseHeader;
    std::vector<BYTE> m_responseBody;
    bool m_secure;
};