/*----------------------------------------------------------------------------
 *  FILE: fSHA1.cpp
 *
 *		Copyright(c) 2012 Frontier Developments Ltd.
 *
 *		01-11-2012 DAS Written
 *
 *----------------------------------------------------------------------------
 */

#ifndef _FSHA1_DAS_011112_H
#define _FSHA1_DAS_011112_H

//#include "fCore/Types/fString.h"

typedef size_t fSizeType;
typedef unsigned long long uint64;

struct fSHA1
{
	unsigned long m_hash[5];
    char m_formatted[64];

	void StreamStart();
	void StreamBlock(void const* _data, fSizeType _size);
	void StreamFinal(void const* _data, fSizeType _size, uint64 _totalSize);

	static fSHA1 ComputeHash(void const* _data, fSizeType _size);

	char const * ToString();

	fSHA1()
	{
		m_hash[0] = 0;
		m_hash[1] = 0;
		m_hash[2] = 0;
		m_hash[3] = 0;
		m_hash[4] = 0;
	}

};

#endif
