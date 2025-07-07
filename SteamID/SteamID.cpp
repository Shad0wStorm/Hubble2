// SteamID.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include "steam_api.h"
#include <iostream>
#include <iomanip>
#include <fstream>
#include <cstdio>

using namespace std;

void dumpbuffer(ostream* target, uint8* buffer, uint32 count, bool formatted)
{
	for (uint32 ch = 0; ch < count; ++ch)
	{
		*target << hex << setfill('0') << setw(2) << (uint32)(buffer[ch]);
		if (formatted)
		{
			if (ch % 16 == 15)
			{
				*target << "\n";
			}
			else
			{
				if (ch % 4 == 3)
				{
					*target << " ";
				}
			}
		}
	}
	*target << "\n";
}

int main()
{
	if (SteamAPI_Init())
	{
		ISteamUser *user = SteamUser();
		if (user!=nullptr)
		{
			CSteamID id = user->GetSteamID();
			uint64 uid = id.ConvertToUint64();
			uint8 buffer[1024];
			uint32 used;
			HAuthTicket ticket = user->GetAuthSessionTicket(buffer, 1024, &used);

			if (ticket == k_HAuthTicketInvalid)
			{
				cout << "Invalid Auth Session Ticket.\n";
			}
			else
			{
				cout << "Received " << used << " bytes of ticket data :\n";
				dumpbuffer(&cout, buffer, used, true);
				cout << "\n\n";
				dumpbuffer(&cout, buffer, used, false);
				ofstream targetfile;
				targetfile.open("SessionTicket.txt", ios::out | ios::trunc);
				dumpbuffer(&targetfile, buffer, used, false);
				targetfile.close();
				cout << "\n\nPress enter to cancel session ticket.\n\n";
				cin.ignore();
				user->CancelAuthTicket(ticket);
				cout << "Cancelled ticket.\n";
				remove("SessionTicket.txt");
			}
		}
		SteamAPI_Shutdown();
		cout << "\nShutdown completed.\n\nPress enter to exit.\n\n";
		cin.ignore();
	}
	else
	{
		cout << "Failed to initialise Steam, is it running?\n\nPress enter to exit.\n\n";
		cin.ignore();
	}
	return 0;
}

