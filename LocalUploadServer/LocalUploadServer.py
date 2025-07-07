#!/usr/bin/env python
 
"""Simple HTTP Server With Upload.
 
This module builds on BaseHTTPServer by implementing the standard GET
and HEAD requests in a fairly straightforward manner.

This is based on SimpleHTTPServerWithUpload.py from:

https://gist.github.com/UniIsland/3346170

extended to support the PUT command used by the main server upload API.

This server is intended to be used as a replacement for the real server in
order to test crash reporter/Hardware reporter. The real server cannot be
easily used since it relies on valid tokens and timing information which is
difficult to provide when running the crash reporter under visual studio.

CrashReporter has been extended to support additional options to work with the
local server, similar changes will be required for the HardwareReporter.

The server can be run in two ways:

A) In visual studio set "LocalUploadServer" as the start up project and run
   as normal. This will allow the code to be debugged (assuming the python
   tools are installed).

B) Open a command prompt in the LocalUploadServer:
      Right click on "LocalUploadServer.py" and select "Open Command Prompt"
      Type "python LocalUploadServer.py" and press enter.

Once the server is running it may be accessed via a web browser as normal.

The server uses the current working directory as the root of the pages it
serves. The most useful page to test the server is working will be:

http://localhost/api/1.0/dump/

Which is where files uploaded using the "http://localhost/api/1.0/dump/upload"
uri will be placed. The server built in upload mechanism has not been changed
but is not useful for testing our report uploader tools.

The uri

http://localhost/exit

may be used to shutdown the server (any uri ending in exit will have this
effect).

The CrashReporter can then be run from the command line or through Visual
Studio by setting it as the start up project. Usually you will run the server
via a command prompt so you can run CrashReporter via Visual Studio for
debugging.

The crash reporter needs the following arguments though the values are never
used by the local upload server. The real server does require them for
validation/logging purposes and so they must always be provided.

/MachineToken <Machine>
/Version <versionstring>
/MachineId <machineidstring>
/Time <timestring> 

Standard required options that are used and must therefore be provided and
contain correct values:

/AuthToken <tokenstring>
    The auth token is used as the file name to save the uploaded file under on
    the server. For ease of testing it should end in .zip so the file can be
    opened in place to confirm that the file was created/uploaded correctly.
    Since the local server provides no authentication the actual name is not
    significant to the server an can be chosen to aid testing.
/DumpReport <reportname>
    The dump report represents the crash dump to be packaged/uploaded as
    normal. Since this is for testing only there is no requirement that it is
    actually a crash dump, any file can be chosen to provide as much data as
    required for the test.

The following options have been added to the crash reporter that are not used
with the real server but are useful for testing purposes.

/ServerRoot http://localhost
    Sets the path to the server to communicate with instead of the compiled in
    path which always talks to the live server. If you actually want to
    communicate between machines the server can be run on a different machine
    and the path updated accordingly. Note that the path segment of the uri
    "/api/1.0/dump/upload" is still added internally.
/AutoSend
    Do not wait for the user to press the send button but immediately begin
    the upload process.
/SkipCompress
    If there is an existing CrashReport.zip file upload that directly instead
    of creating it from the file given in the /DumpReport option. For large
    files the compression can take significant time and should not be changing
    unless the source file has changed. Either delete CrashReport.zip or
    remove this option if you change the source file used to ensure the correct
    CrashReport.zip is generated initially.
"""
 
 
__version__ = "0.1"
__all__ = ["SimpleHTTPRequestHandler"]
__author__ = "bones7456"
__home_page__ = "http://li2z.cn/"
 
import os
import posixpath
import BaseHTTPServer
import urllib
import urlparse
import cgi
import shutil
import mimetypes
import re
import time
try:
    from cStringIO import StringIO
except ImportError:
    from StringIO import StringIO
 
 
class SimpleHTTPRequestHandler(BaseHTTPServer.BaseHTTPRequestHandler):
 
    """Simple HTTP request handler with GET/HEAD/POST commands.
 
    This serves files from the current directory and any of its
    subdirectories.  The MIME type for files is determined by
    calling the .guess_type() method. And can reveive file uploaded
    by client.
 
    The GET/HEAD/POST requests are identical except that the HEAD
    request omits the actual contents of the file.
 
    """
 
    server_version = "SimpleHTTPWithUpload/" + __version__
 
    def do_GET(self):
        """Serve a GET request."""
        f = self.send_head()
        if f:
            self.copyfile(f, self.wfile)
            f.close()
        requestpath = self.translate_path(self.path).lower()
        if (os.path.basename(requestpath)=="exit"):
            self.server.keeprunning = False
 
    def do_HEAD(self):
        """Serve a HEAD request."""
        f = self.send_head()
        if f:
            f.close()
 
    def do_POST(self):
        """Serve a POST request."""
        r, info = self.deal_post_data()
        print r, info, "by: ", self.client_address
        f = StringIO()
        f.write('<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 3.2 Final//EN">')
        f.write("<html>\n<title>Upload Result Page</title>\n")
        f.write("<body>\n<h2>Upload Result Page</h2>\n")
        f.write("<hr>\n")
        if r:
            f.write("<strong>Success:</strong>")
        else:
            f.write("<strong>Failed:</strong>")
        f.write(info)
        f.write("<br><a href=\"%s\">back</a>" % self.headers['referer'])
        f.write("<hr><small>Powerd By: bones7456, check new version at ")
        f.write("<a href=\"http://li2z.cn/?s=SimpleHTTPServerWithUpload\">")
        f.write("here</a>.</small></body>\n</html>\n")
        length = f.tell()
        f.seek(0)
        self.send_response(200)
        self.send_header("Content-type", "text/html")
        self.send_header("Content-Length", str(length))
        self.end_headers()
        if f:
            self.copyfile(f, self.wfile)
            f.close()

    def do_PUT(self):
        """Server a PUT request."""
        r, info = self.deal_put_data()
        print r, info, "by: ", self.client_address
        f = StringIO()
        f.write('<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 3.2 Final//EN">')
        f.write("<html>\n<title>Upload Result Page</title>\n")
        f.write("<body>\n<h2>Upload Result Page</h2>\n")
        f.write("<hr>\n")
        if r:
            f.write("<strong>Success:</strong>")
        else:
            f.write("<strong>Failed:</strong>")
        f.write(info)
        try:
            f.write("<br><a href=\"%s\">back</a>" % self.headers['referer'])
        except KeyError:
            pass
        length = f.tell()
        f.seek(0)
        self.send_response(200)
        self.send_header("Content-type", "text/html")
        self.send_header("Content-Length", str(length))
        self.end_headers()
        if f:
            self.copyfile(f, self.wfile)
            f.close()

    def deal_put_data(self):
        remainbytes = int(self.headers['content-length'])

        o = urlparse.urlparse(self.path)
        if (o.path!="/api/1.0/dump/upload"):
            if (o.path!="/1.1/dump/upload"):
                return False, "Unrecognised path "+path
        try:
            qp = urlparse.parse_qs(o.query, True, True)
            targetName = o.path[1:]+qp["authToken"][0]
            targetName = os.path.abspath(targetName)
            print "Saving uploaded data to : " + targetName
        except ValueError:
            return False, "Invalid query string"

        try:
            out = open(targetName, 'wb')
        except IOError:
            return (False, "Can't create file to write, do you have permission to write?")

        try:
            while remainbytes > 0:
                #time.sleep(0.002)
                toread = 8192
                if (remainbytes<toread):
                    toread = remainbytes
                    time.sleep(10.0) # extra delay at the end to try and force a drop at the other end
                line = self.rfile.read(toread)
                remainbytes -= len(line)
                out.write(line)
            out.close()
            return (True, "Data received.")
        except:
            return (False, "Unexpect Ends of data.")
 
        
    def deal_post_data(self):
        boundary = self.headers.plisttext.split("=")[1]
        remainbytes = int(self.headers['content-length'])
        line = self.rfile.readline()
        remainbytes -= len(line)
        if not boundary in line:
            return (False, "Content NOT begin with boundary")
        line = self.rfile.readline()
        remainbytes -= len(line)
        fn = re.findall(r'Content-Disposition.*name="file"; filename="(.*)"', line)
        if not fn:
            return (False, "Can't find out file name...")
        path = self.translate_path(self.path)
        fn = os.path.join(path, fn[0])
        line = self.rfile.readline()
        remainbytes -= len(line)
        line = self.rfile.readline()
        remainbytes -= len(line)
        try:
            out = open(fn, 'wb')
        except IOError:
            return (False, "Can't create file to write, do you have permission to write?")
                
        preline = self.rfile.readline()
        remainbytes -= len(preline)
        while remainbytes > 0:
            line = self.rfile.readline()
            remainbytes -= len(line)
            if boundary in line:
                preline = preline[0:-1]
                if preline.endswith('\r'):
                    preline = preline[0:-1]
                out.write(preline)
                out.close()
                return (True, "File '%s' upload success!" % fn)
            else:
                out.write(preline)
                preline = line
        return (False, "Unexpect Ends of data.")
 
    def send_head(self):
        """Common code for GET and HEAD commands.
 
        This sends the response code and MIME headers.
 
        Return value is either a file object (which has to be copied
        to the outputfile by the caller unless the command was HEAD,
        and must be closed by the caller under all circumstances), or
        None, in which case the caller has nothing further to do.
 
        """
        path = self.translate_path(self.path)
        f = None
        if os.path.isdir(path):
            if not self.path.endswith('/'):
                # redirect browser - doing basically what apache does
                self.send_response(301)
                self.send_header("Location", self.path + "/")
                self.end_headers()
                return None
            for index in "index.html", "index.htm":
                index = os.path.join(path, index)
                if os.path.exists(index):
                    path = index
                    break
            else:
                return self.list_directory(path)
        ctype = self.guess_type(path)
        try:
            # Always read in binary mode. Opening files in text mode may cause
            # newline translations, making the actual size of the content
            # transmitted *less* than the content-length!
            f = open(path, 'rb')
        except IOError:
            self.send_error(404, "File not found")
            return None
        self.send_response(200)
        self.send_header("Content-type", ctype)
        fs = os.fstat(f.fileno())
        self.send_header("Content-Length", str(fs[6]))
        self.send_header("Last-Modified", self.date_time_string(fs.st_mtime))
        self.end_headers()
        return f
 
    def list_directory(self, path):
        """Helper to produce a directory listing (absent index.html).
 
        Return value is either a file object, or None (indicating an
        error).  In either case, the headers are sent, making the
        interface the same as for send_head().
 
        """
        try:
            list = os.listdir(path)
        except os.error:
            self.send_error(404, "No permission to list directory")
            return None
        list.sort(key=lambda a: a.lower())
        f = StringIO()
        displaypath = cgi.escape(urllib.unquote(self.path))
        f.write('<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 3.2 Final//EN">')
        f.write("<html>\n<title>Directory listing for %s</title>\n" % displaypath)
        f.write("<body>\n<h2>Directory listing for %s</h2>\n" % displaypath)
        f.write("<hr>\n")
        f.write("<form ENCTYPE=\"multipart/form-data\" method=\"post\">")
        f.write("<input name=\"file\" type=\"file\"/>")
        f.write("<input type=\"submit\" value=\"upload\"/></form>\n")
        f.write("<hr>\n<ul>\n")
        for name in list:
            fullname = os.path.join(path, name)
            displayname = linkname = name
            # Append / for directories or @ for symbolic links
            if os.path.isdir(fullname):
                displayname = name + "/"
                linkname = name + "/"
            if os.path.islink(fullname):
                displayname = name + "@"
                # Note: a link to a directory displays with @ and links with /
            f.write('<li><a href="%s">%s</a>\n'
                    % (urllib.quote(linkname), cgi.escape(displayname)))
        f.write("</ul>\n<hr>\n</body>\n</html>\n")
        length = f.tell()
        f.seek(0)
        self.send_response(200)
        self.send_header("Content-type", "text/html")
        self.send_header("Content-Length", str(length))
        self.end_headers()
        return f
 
    def translate_path(self, path):
        """Translate a /-separated PATH to the local filename syntax.
 
        Components that mean special things to the local file system
        (e.g. drive or directory names) are ignored.  (XXX They should
        probably be diagnosed.)
 
        """
        # abandon query parameters
        path = path.split('?',1)[0]
        path = path.split('#',1)[0]
        path = posixpath.normpath(urllib.unquote(path))
        words = path.split('/')
        words = filter(None, words)
        path = os.getcwd()
        for word in words:
            drive, word = os.path.splitdrive(word)
            head, word = os.path.split(word)
            if word in (os.curdir, os.pardir): continue
            path = os.path.join(path, word)
        return path
 
    def copyfile(self, source, outputfile):
        """Copy all data between two file objects.
 
        The SOURCE argument is a file object open for reading
        (or anything with a read() method) and the DESTINATION
        argument is a file object open for writing (or
        anything with a write() method).
 
        The only reason for overriding this would be to change
        the block size or perhaps to replace newlines by CRLF
        -- note however that this the default server uses this
        to copy binary data as well.
 
        """
        shutil.copyfileobj(source, outputfile)
 
    def guess_type(self, path):
        """Guess the type of a file.
 
        Argument is a PATH (a filename).
 
        Return value is a string of the form type/subtype,
        usable for a MIME Content-type header.
 
        The default implementation looks the file's extension
        up in the table self.extensions_map, using application/octet-stream
        as a default; however it would be permissible (if
        slow) to look inside the data to make a better guess.
 
        """
 
        base, ext = posixpath.splitext(path)
        if ext in self.extensions_map:
            return self.extensions_map[ext]
        ext = ext.lower()
        if ext in self.extensions_map:
            return self.extensions_map[ext]
        else:
            return self.extensions_map['']
 
    if not mimetypes.inited:
        mimetypes.init() # try to read system mime.types
    extensions_map = mimetypes.types_map.copy()
    extensions_map.update({
        '': 'application/octet-stream', # Default
        '.py': 'text/plain',
        '.c': 'text/plain',
        '.h': 'text/plain',
        })
 
 
def test(HandlerClass = SimpleHTTPRequestHandler,
         ServerClass = BaseHTTPServer.HTTPServer):
    BaseHTTPServer.test(HandlerClass, ServerClass)

def runserver(HandlerClass = SimpleHTTPRequestHandler,
         ServerClass = BaseHTTPServer.HTTPServer):
    server_address = ('', 80)
    httpd = ServerClass(server_address, HandlerClass)
    httpd.keeprunning = True
    while httpd.keeprunning:
        httpd.handle_request()

if __name__ == '__main__':
    runserver()