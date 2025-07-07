//
//  main.c
//  edbootstrapper
//
//  Created by Elliot Prior on 21/01/2015.
//  Copyright (c) 2015 Frontier Developments PLC. All rights reserved.
//

#include <limits.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <mach-o/dyld.h>

int main(int argc, const char * argv[]) {
    //Two paths, four "s and a space.
    const int BUFSIZE = PATH_MAX * 2 + 5;
    char exePath[BUFSIZE];
    char resolvedPath[BUFSIZE];
    char* inResolvedPath = resolvedPath;
    //Clear our buffers properly.
    memset(exePath, 0, BUFSIZE);
    memset(resolvedPath, 0, BUFSIZE);
    //Set quotes so that we don't have to worry about spaces in paths.
    *inResolvedPath = '"';
    ++inResolvedPath;
    uint32_t size = sizeof(exePath);
    if (_NSGetExecutablePath(exePath, &size) == 0)
    {
        realpath(exePath, inResolvedPath);
        //Go to the end of the path so we can search backwards.
        char* strEnd = (resolvedPath + strlen(resolvedPath));
        --strEnd;
        //Find the last path separator,zeroing as we go along.
        while (*strEnd != '/')
        {
            *strEnd = '\0';
            --strEnd;
        }
        //Re-use the original buffer for storing the path.
        memset(exePath, 0, BUFSIZE);
        strcpy(exePath, resolvedPath);
        //Go one past the last path separator.
        ++strEnd;
        //Set to the shell script we want to execute.
        char * const hardwareReporterName = "launchscript";
        strcpy(strEnd, hardwareReporterName);
        strEnd += strlen(hardwareReporterName);
        //Set up the quotes to be safe.
        *strEnd = '"';
        ++strEnd;
        *strEnd = ' ';
        //Provide the working directory as the second argument.
        ++strEnd;
        strcpy(strEnd, exePath);
        strEnd += strlen(exePath);
        *strEnd = '"';
        system(resolvedPath);
    }
    return 0;
}
