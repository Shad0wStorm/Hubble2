/*----------------------------------------------------------------------------
 *  FILE: SimpleHttp.cpp
 *
 *		Copyright(c) 2014 Frontier Developments Ltd.
 *
 *		26-09-2014 HRC Written
 *
 *----------------------------------------------------------------------------
 */
#include "windows.h"
#include "SimpleHttp.h"
#include "winhttp.h"

SimpleHttpRequest::SimpleHttpRequest(const std::wstring &userAgent, bool _secure) :
    m_userAgent(userAgent),
    m_secure(_secure)
{
}

bool SimpleHttpRequest::SendRequest(const std::wstring &url, const std::wstring &method, const std::wstring &path, void *body, DWORD bodySize)
{
    DWORD dwSize=0;
    DWORD dwDownloaded=0;
    DWORD headerSize = 0;
    BOOL  bResults = FALSE;
    HINTERNET hSession=0;
    HINTERNET hConnect=0;
    HINTERNET hRequest=0;

    m_responseHeader.resize(0);
    m_responseBody.resize(0);

    hSession = WinHttpOpen( m_userAgent.c_str(), WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0 );
    if (!hSession)
    {
        //printf("session handle failed\n");
    }
    else
    {
        int port = (m_secure ? INTERNET_DEFAULT_HTTPS_PORT : INTERNET_DEFAULT_HTTP_PORT);
        hConnect = WinHttpConnect( hSession, url.c_str(), port, 0 );
        if (!hConnect)
        {
            //printf("connect handle failed\n");
        }
        else
        {
            hRequest = WinHttpOpenRequest( hConnect, method.c_str(), path.c_str(), NULL, WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, 
                (m_secure ? WINHTTP_FLAG_SECURE : 0) );

            if (hRequest)
            {
                bResults = WinHttpSendRequest( hRequest, WINHTTP_NO_ADDITIONAL_HEADERS, 0, body, bodySize, bodySize, 0 );
            }
            else
            {
                //printf("request handle failed\n");        
            }
        }
    }


    if (bResults)
    {
        bResults = WinHttpReceiveResponse( hRequest, NULL );
    }

    if (bResults)
    {
        bResults = WinHttpQueryHeaders(hRequest, WINHTTP_QUERY_RAW_HEADERS_CRLF, NULL, WINHTTP_NO_OUTPUT_BUFFER, &headerSize, WINHTTP_NO_HEADER_INDEX);
        if ((!bResults) && (GetLastError() == ERROR_INSUFFICIENT_BUFFER))
        {
            m_responseHeader.resize(headerSize / sizeof(wchar_t));
            if (m_responseHeader.empty())
            {
                bResults = TRUE;
            }
            else
            {
                bResults = WinHttpQueryHeaders(hRequest, WINHTTP_QUERY_RAW_HEADERS_CRLF, NULL, &m_responseHeader[0], &headerSize, WINHTTP_NO_HEADER_INDEX);
                if( !bResults ) headerSize = 0;
                m_responseHeader.resize(headerSize / sizeof(wchar_t));
            }
        }
    }
    if (bResults)
    {
        do
        {
            // Check for available data.
            dwSize = 0;
            bResults = WinHttpQueryDataAvailable( hRequest, &dwSize );
            if (!bResults)
            {
                //printf( "Error %u in WinHttpQueryDataAvailable.\n", GetLastError( ) );
                break;
            }

            if (dwSize == 0)
                break;

            do
            {
                // Allocate space for the buffer.
                DWORD dwOffset = m_responseBody.size();
                m_responseBody.resize(dwOffset+dwSize);

                // Read the data.
                bResults = WinHttpReadData( hRequest, &m_responseBody[dwOffset], dwSize, &dwDownloaded );
                if (!bResults)
                {
                    //printf( "Error %u in WinHttpReadData.\n", GetLastError( ) );
                    dwDownloaded = 0;
                }

                m_responseBody.resize(dwOffset+dwDownloaded);

                if (dwDownloaded == 0)
                    break;

                dwSize -= dwDownloaded;
            }
            while (dwSize > 0);
        }
        while (true);
    }

    // Report any errors.
    if (!bResults)
    {
        //printf( "Error %d has occurred.\n", GetLastError( ) );
    }

    // Close any open handles.
    if( hRequest ) WinHttpCloseHandle( hRequest );
    if( hConnect ) WinHttpCloseHandle( hConnect );
    if( hSession ) WinHttpCloseHandle( hSession );

    return bResults;
}