#include "SimpleJSON.h"

#include <sstream>

SimpleJSON::SimpleJSON(void)
{
	m_content << "{";
	m_keys = 0;
}


SimpleJSON::~SimpleJSON(void)
{
}

void SimpleJSON::Add(const char* key, const char* value)
{
	if (m_keys>0)
	{
		m_content << ", ";
	}
	m_content << "\"" << key << "\": \"" << value << "\"";
	++m_keys;
}

const char*SimpleJSON:: c_str()
{
	m_content << "}";
	m_final = m_content.str();
	m_content.clear();
	m_content << "{";
	return m_final.c_str();
}

