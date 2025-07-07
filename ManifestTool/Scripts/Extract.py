import os
import shutil
import sys

import ManifestUtils

def Extract( manifest, source, fileStore = ManifestUtils.FileStore()):
    (base, ext) = os.path.splitext(manifest)
    if not os.path.isfile(manifest):
        print "Failed to find required manifest file "+manifest
        return
    targetdir = base + "_Extracted"
    ManifestUtils.EnsureDirectory(targetdir)
    
    fp = open(manifest, "r")
    lines = fp.readlines()
    fp.close()
    total = len(lines)
    processed = 0
    for line in lines:
        sline = line.strip()
        (hash, size, path) = sline.split(",",2)
        hashdir = hash[:2]
        hashfile = os.path.join(source, hashdir, hash[2:])
        targetfile = targetdir + path
        targetfiledir = os.path.dirname(targetfile)
        if not os.path.exists(targetfiledir):
            os.makedirs(targetfiledir)
        if not fileStore.Exists(hashfile):
            print "Source hash file not available for hash: "+hash+" "+path+" will be missing"
        else:
            fileStore.Extract(targetfile, hashfile)
        processed = processed + 1
        if (processed<total):
            print ManifestUtils.Progress(total, processed),
    print
    
if __name__=="__main__":
    sources = sys.argv[1:]
    
    for source in sources:
        fileStore = None
        try:
            (manifest, path) = source.split(":",1)
            try:
                (base,store) = path.split("_",1)
            except ValueError:
                # Assume it might be compressed
                store = "Compressed"
        except ValueError:
            (base,store) = source.split("_",1)
            manifest = base + "_manifest.txt"
            path = source
        if store=="Compressed":
            print "Enabling compressed file store"
            fileStore = ManifestUtils.GzipFileStore()
        else:
            fileStore = ManifestUtils.FileStore()
        if os.path.isdir(path) and os.path.isfile(manifest):
            Extract(manifest, path, fileStore = fileStore)