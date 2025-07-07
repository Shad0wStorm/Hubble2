/*----------------------------------------------------------------------------
 *  (adapted from) FILE: RC4Encrypt.cpp
 *
 *		Copyright(c) 2013 Frontier Developments Ltd.
 *
 *----------------------------------------------------------------------------
 */


#include "Rc4Encrypt.h"


void fSwap(unsigned char& _a, unsigned char& _b)
{
    unsigned char const temp = _a;
    _a = _b;
    _b = temp;
}

void RC4Encrypt::InitialisePermutationArray(std::string const & _key, unsigned char* _s)
{
	for (int i = 0; i < 256; i++)
	{
		_s[i] = (unsigned char)i;
	}

	size_t len = _key.size();
	for (size_t i = 0, j = 0; i < 256; i++)
	{
		j = (j + _s[i] + _key[i % len]) % 256;
		fSwap(_s[i],_s[j]);
	}
}


void RC4Encrypt::Encrypt
(
	std::string const & _key,
	unsigned char *io_buff,
	size_t _plainSz
)
{
	unsigned char s[256];
	InitialisePermutationArray(_key, s);

	size_t i = 0;
	size_t j = 0;
	for (size_t charIdx = 0; charIdx < _plainSz; charIdx++)
	{
		i = (i + 1u) % 256u;
		j = (j + s[i]) % 256u;
		fSwap(s[i],s[j]);
		unsigned char keystream = s[ (s[i] + s[j]) % 256 ];
		unsigned char encrypted = (unsigned char)(io_buff[charIdx] ^ keystream );
		io_buff[charIdx] = encrypted;
	}

}






