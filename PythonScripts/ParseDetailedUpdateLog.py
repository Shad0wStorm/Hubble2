import sys
import argparse
import os
import json
import traceback

class LogEntry:
    def __init__(self):
        self.date = None
        self.time = None
        self.details = None
        self.action = None

    def ParseAction(self, text):
        parsed = True
        try:
            (entrydt, entryjson) = text.split(':',1)
        except ValueError:
            return False

        while (entryjson[-1]==';'):
            entryjson = entryjson[:-2].strip()

        entryjson = entryjson.replace('\\','\\\\')

        try:
            self.details = json.loads(entryjson)
            self.date = self.details["date"]
            self.time = self.details["time"]
            self.action = self.details["action"]
        except ValueError:
            print (entrydt + " : " + entryjson)
            traceback.print_exc()
            return False
        return parsed

    def ThreadName(self):
        try:
            return self.details["ThreadName"]
        except KeyError:
            return None

    def Log(self):
        result = self.date + "/" + self.time + ": "
        result = result + json.dumps(self.details) + " ;"
        return result


def ParseLine(text):
    if not text:
        return None

    le = LogEntry()
    if not le.ParseAction(text):
        return None

    return le

def LoadFile(filename):
    content = []
    fp = open(filename, "r")
    line = fp.readline()
    while line:
        line = line.strip()
        logentry = ParseLine(line)
        if logentry:
            content.append(logentry)
        line = fp.readline()
    fp.close()
    return content

def ProcessFile(filename, options):
    print ("Processing file "+filename)
    if not os.path.isfile(filename):
        print ("File "+filename+" could not be found, skipping.")
        return

    content = LoadFile(filename)
    print ("Loaded "+`len(content)` +" entries.")

    threads = []
    for entry in content:
        threadname = entry.ThreadName()
        if threadname:
            if not threadname in threads:
                threads.append(threadname)

    print ("Identified "+`len(threads)` + " named threads")

    (filebase,ext) = os.path.splitext(filename)

    for thread in threads:
        threadpath = filebase+"_"+thread+ext
        tfp = open(threadpath, "w")
        for entry in content:
            threadname = entry.ThreadName()
            if (threadname==None) or (threadname==thread):
                tfp.write(entry.Log()+"\n")
        tfp.close()

if __name__=="__main__":
    #print (sys.argv)

    ap = argparse.ArgumentParser(description="Post-process an Elite Dangerous detailed update log.")
    
    ap.add_argument('filelist', metavar="logfile", nargs='+', help='a log file to parse')

    options = ap.parse_args()

    for file in options.filelist:
        ProcessFile(file, options)
