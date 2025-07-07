/*----------------------------------------------------------------------------
 *  (adapted from) FILE: RC4Encrypt.cpp
 *
 *		Copyright(c) 2013 Frontier Developments Ltd.
 *
 *----------------------------------------------------------------------------
 */
#ifndef _RC4ENCRYPT_HRC_161213_H
#define _RC4ENCRYPT_HRC_161213_H

#include <string>



class RC4Encrypt
{
public:
	static void Encrypt( std::string const & _key, unsigned char * io_buff, size_t _plainSz );

private:
	static void InitialisePermutationArray( std::string const & _key, unsigned char * _s);
};


#endif
