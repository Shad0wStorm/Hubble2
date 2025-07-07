#pragma once

#include <sstream>


class SimpleJSON
{
private:
	std::ostringstream m_content;
	std::string m_final;
	int m_keys;
public:
	SimpleJSON(void);
	~SimpleJSON(void);

	void Add(const char* key, const char* value);
	const char* c_str();
};

