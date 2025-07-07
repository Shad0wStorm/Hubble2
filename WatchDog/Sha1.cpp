/*----------------------------------------------------------------------------
 *  FILE: fSHA1.cpp
 *
 *		Copyright(c) 2012 Frontier Developments Ltd.
 *
 *		2012-09-01 DAS
 *
 *----------------------------------------------------------------------------
 */

#include "SHA1.h"
#include <memory.h>
#include <stdio.h>

//F_DEFINE_DEBUG_SECTION(fSHA1)

typedef unsigned char uint8;
typedef unsigned long uint32;
typedef unsigned uint;

template<unsigned N> static inline uint32 LeftRotate(uint32 _value) { return (_value << N) | (_value >> (32 - N)); }

void fSHA1::StreamStart()
{
	m_hash[0] = 0x67452301;
	m_hash[1] = 0xEFCDAB89;
	m_hash[2] = 0x98BADCFE;
	m_hash[3] = 0x10325476;
	m_hash[4] = 0xC3D2E1F0;
}

void fSHA1::StreamBlock(void const* _data, fSizeType _size)
{
//	fAssert(_size % 64 == 0); // Required chunk size
	uint8* data = (uint8*)_data;
	uint32 w[80];

	while(_size > 0)
	{
		// Read data as big-endian
		for (uint i = 0; i < 16; i++)
		{
			w[i] = (uint32)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);
			data += 4;
			_size -= 4;
		}

		// Extend to 80 words
		for (uint i = 16; i < 80; i++)
		{
			w[i] = LeftRotate<1>(w[i-3] ^ w[i-8] ^ w[i-14] ^ w[i-16]);
		}

		uint32 a = m_hash[0];
		uint32 b = m_hash[1];
		uint32 c = m_hash[2];
		uint32 d = m_hash[3];
		uint32 e = m_hash[4];

#define F_SHA1_MAIN(k, f)												\
		{																\
			uint32 temp = LeftRotate<5>(a) + (f) + e + (k) + w[i];		\
			e = d;														\
			d = c;														\
			c = LeftRotate<30>(b);										\
			b = a;														\
			a = temp;													\
		}

		for (uint i = 0;  i < 20; i++)	{ F_SHA1_MAIN(0x5A827999, (b & c) | ((~b) & d)); }
		for (uint i = 20; i < 40; i++)	{ F_SHA1_MAIN(0x6ED9EBA1, b ^ c ^ d); }
		for (uint i = 40; i < 60; i++)	{ F_SHA1_MAIN(0x8F1BBCDC, (b & c) | (b & d) | (c & d)); }
		for (uint i = 60; i < 80; i++)	{ F_SHA1_MAIN(0xCA62C1D6, b ^ c ^ d); }

#undef F_SHA1_MAIN

		m_hash[0] += a;
		m_hash[1] += b;
		m_hash[2] += c;
		m_hash[3] += d;
		m_hash[4] += e;
	}
}

void fSHA1::StreamFinal(void const* _data, fSizeType _size, uint64 _totalSize)
{
	uint tailSize = _size % 64;
	StreamBlock(_data, _size - tailSize);

	// Do we fit into one chunk?
	uint paddedSize = (tailSize + 1 + 8 <= 64 ? 64u : 128u);
	uint64 totalSizeBits = _totalSize * 8;
	uint8 w[128];
	memset(w, 0, sizeof(w));
	memcpy(w, (uint8*)_data + _size - tailSize, tailSize);
	// Append '1' bit
	w[tailSize] = 0x80;
	// Append message length in bits as big-endian
	w[paddedSize - 8] = (uint8)((totalSizeBits >> 56) & 0xFF);
	w[paddedSize - 7] = (uint8)((totalSizeBits >> 48) & 0xFF);
	w[paddedSize - 6] = (uint8)((totalSizeBits >> 40) & 0xFF);
	w[paddedSize - 5] = (uint8)((totalSizeBits >> 32) & 0xFF);
	w[paddedSize - 4] = (uint8)((totalSizeBits >> 24) & 0xFF);
	w[paddedSize - 3] = (uint8)((totalSizeBits >> 16) & 0xFF);
	w[paddedSize - 2] = (uint8)((totalSizeBits >> 8)  & 0xFF);
	w[paddedSize - 1] = (uint8)((totalSizeBits >> 0)  & 0xFF);
	StreamBlock(w, paddedSize);
}

fSHA1 fSHA1::ComputeHash(void const* _data, fSizeType _size)
{
	fSHA1 sha1;
	sha1.StreamStart();
	sha1.StreamFinal(_data, _size, _size);
	return sha1;
}


char const *fSHA1::ToString()
{
	sprintf( &m_formatted[0], "%08X%08X%08X%08X%08X", m_hash[0], m_hash[1], m_hash[2], m_hash[3], m_hash[4]);
    return &m_formatted[0];
}

