import filecmp
import hashlib
import os
import shutil
import sys

import ManifestUtils

class ManifestEntry:
    def __init__(self):
        self.hash = None
        self.size = -1
        self.path = None
        
    def Load(self,path):
        self.path = path
        fp = open(path,"rb")
        fp.seek(0,os.SEEK_END)
        self.size = fp.tell()
        fp.seek(0,os.SEEK_SET)
        hash = hashlib.sha1()
        data = fp.read(4096)
        while (len(data)>0):
            hash.update(data)
            data = fp.read(4096)
        self.hash = hash.hexdigest()
        fp.close()
        
    def Matches(self, other):
        if self.path!=other.path:
            return False
        if self.size!=other.size:
            return False
        if self.hash!=other.hash:
            return False
        return True
        
    def __repr__(self):
        result = "%s,%d,%s" % (self.hash,self.size,self.path)
        return result
        
class Manifest:
    def __init__(self):
        self.items = {}
        
    def Load(self, dir):
        totalsize = 0
        for (root, dirs, files) in os.walk(dir):
            for file in files:
                path = os.path.join(root, file)
                entry = ManifestEntry()
                entry.Load(path)
                if (path[:len(dir)]==dir):
                    name = path[len(dir):]
                    entry.path = name
                self.items[name] = entry
                totalsize = totalsize + entry.size
                print "Loaded %d files %d bytes." % (len(self.items), totalsize) + "\r",
        print
    
    def Diff(self, other):
        added = []
        removed = []
        changed = []
        unchanged = []

        left = self.items.keys()
        right = other.items.keys()
        
        for item in left:
            if item in right:
                if self.items[item].Matches(other.items[item]):
                    unchanged.append(item)
                else:
                    changed.append(item)
            else:
                added.append(item)
        
        for item in right:
            if not item in left:
                removed.append(item)
        
        return (self, other, added, removed, changed, unchanged)
        
    def Flatten(self, source, target, reset=True, fileStore=ManifestUtils.FileStore()):
        ManifestUtils.EnsureDirectory(target,reset)
        
        processed = 0
        count = len(self.items)
        for item in self.items:
            entry = self.items[item]
            hash = entry.hash
            hashdir = os.path.join(target,hash[:2])
            hashfile = os.path.join(hashdir,hash[2:])
            sourcefile = source + entry.path
            if not os.path.isdir(hashdir):
                os.makedirs(hashdir)
            if os.path.isfile(hashfile):
                # File already exists, confirm it is a duplicate
                if not fileStore.Compare(sourcefile, hashfile):
                    print "Hash collision "+entry.hash
            else:
                fileStore.Insert(sourcefile, hashfile)
            processed = processed + 1
            print ManifestUtils.Progress(count, processed),
        print
        
    def __repr__(self):
        paths = self.items.keys()
        paths.sort()
        result = ""
        for path in paths:
            if result:
                result = result + "\n"
            result = result + `self.items[path]`
        return result
                
def Convert( (left, right, added, removed, changed, unchanged) ):
    result = ""
    if len(added)>0:
        result = result + "Added %d items\n" % len(added)
        for item in added:
            result = result + "\t"+`left.items[item]`+"\n"
    if len(removed)>0:
        result = result + "Removed %d items\n" % len(removed)
        for item in removed:
            result = result + "\t"+`right.items[item]`+"\n"
            
    if (len(changed)>0):
        result = result + "Changed %d items\n" % len(changed)
        for item in changed:
            li = left.items[item]
            ri = right.items[item]
            result = result + "\t%s->%s,%d,%s\n" % (ri.hash, li.hash, li.size, li.path)
            
    if (len(unchanged)>0):
        result = result + "Listing %d unchanged items\n" % (len(unchanged))
        for item in unchanged:
            li = left.items[item]
            ri = right.items[item]
            result = result + "\t"+`left.items[item]`+"\n"

    return result
    
if __name__=="__main__":
    sources = sys.argv[2:]
    previous = None
    previousSource = None
    for source in sources:
        sourceitem = os.path.basename(source)
        manifest = Manifest()
        manifest.Load(source)
        fp = open(sourceitem+"_manifest.txt", "w")
        fp.write(`manifest`)
        fp.close()
        if sys.argv[1]=="diff":
            if previous!=None:
                diff = manifest.Diff(previous)
                text = Convert(diff)
                
                fp = open(previousSource+"-"+sourceitem+".txt","w")
                fp.write(text)
                fp.close()
        elif sys.argv[1]=='flatten':
            target = sourceitem + "_Flattened"
            manifest.Flatten(sourceitem, target)
        elif sys.argv[1]=='shared':
            target = "Cache"
            manifest.Flatten(sourceitem, target, reset=False)
        elif sys.argv[1]=='compress':
            target = sourceitem + "_Compressed"
            manifest.Flatten(sourceitem, target, fileStore=ManifestUtils.GzipFileStore())
        elif sys.argv[1]=='compressshared':
            target = "Cache_Compressed"
            manifest.Flatten(sourceitem, target, reset=False, fileStore=ManifestUtils.GzipFileStore())
            
        previous = manifest
        previousSource = sourceitem