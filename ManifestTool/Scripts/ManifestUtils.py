import filecmp
import gzip
import os
import shutil

class FileStore:
    def Compare(self,path, item):
        return filecmp.cmp(path, item)
        
    def Insert(self,path, item):
        shutil.copyfile(path, item)

    def Extract(self, path, item):
        shutil.copyfile(item, path)
        
    def Exists(self, path):
        return os.path.isfile(path)
        
class GzipFileStore:
    def Compare(self, path, item):
        cip = gzip.open(item, 'rb')
        fp = open(path, 'rb')
        blockSize = 8192
        reading = True
        result = 0
        while reading:
            cipb = cip.read(blockSize)
            fpb = fp.read(blockSize)
            if (len(cipb)!=len(fpb)):
                print "Block size different %d vs %d" % (len(cipd), len(fpb))
                result = len(cipb)-len(fpb)
                break
            if len(cipb)==0:
                # Both reached end of data
                result = 0
                break;
            result = cmp(cipb,fpb)
            if (result!=0):
                print "Data different " + `result`
                break
        fp.close()
        cip.close()
        return result==0
        
    def Insert(self, path, item):
        cip = gzip.open(item+".gz", "wb")
        fp = open(path, 'rb')
        blockSize = 8192 * 4
        reading = True
        while reading:
            data = fp.read(blockSize)
            if len(data)>0:
                cip.write(data)
            else:
                reading = False
        fp.close()
        cip.close()
        
    def Extract(self, path, item):
        if os.path.isfile(item):
            shutil.copyfile(item, path)

        cip = gzip.open(item+".gz", "rb")
        fp = open(path, 'wb')
        blockSize = 8192
        reading = True
        while reading:
            data = cip.read(blockSize)
            if len(data)>0:
                fp.write(data)
            else:
                reading = False
        fp.close()
        cip.close()
        
    def Exists(self, path):
        if os.path.isfile(path):
            return True
        if os.path.isfile(path+".gz"):
            return True
        return False
    
def Progress(total, progress):
    summary = "Processed %d of %d files" % (progress, total)
    pc = (progress*40) / total
    progressstr = "|%s%s|" % ("="*pc, "-"*(40-pc))
    message = "%-30s  %s\r" % (summary,progressstr)
    return message

def EnsureDirectory(target,reset=True):
    if os.path.isdir(target):
        if reset:
            shutil.rmtree(target)
        else:
            return
    retry = 5
    while retry>0:
        try:
            os.makedirs(target)
            retry = 0
        except WindowsError:
            retry = retry - 1
            if retry==0:
                raise
    
