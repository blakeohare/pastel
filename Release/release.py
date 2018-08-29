VERSION = '0.2.0'
MSBUILD = r'C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'
XBUILD = 'xbuild'
RELEASE_CONFIG = '/p:Configuration=Release'

import shutil
import os
import io
import sys

def runCommand(cmd):
	c = os.popen(cmd)
	output = c.read()
	c.close()
	return output

def prepareCleanDirectory(d):
	if os.path.exists(d):
		shutil.rmtree(d)
	os.makedirs(d)

def main(args):

	if len(args) != 1:
		print("usage: python release.py windows|mono")
		return

	platform = args[0]

	if not platform in ('windows', 'mono'):
		print ("Invalid platform: " + platform)
		return
	
	isMono = platform == 'mono'
	BUILD_CMD = XBUILD if isMono else MSBUILD
	SLN_PATH = os.path.join('..', 'Source', 'Pastel.sln')
	print(runCommand(' '.join([BUILD_CMD, RELEASE_CONFIG, SLN_PATH])))
	releaseDir = os.path.join('..', 'Source', 'bin', 'Release')
	copyToDir = 'pastel-' + VERSION + '-' + platform
	prepareCleanDirectory(copyToDir)
	shutil.copyfile(os.path.join(releaseDir , 'Pastel.exe'), os.path.join(copyToDir, 'pastel.exe'))
	print("Release directory created: " + copyToDir)

main(sys.argv[1:])
