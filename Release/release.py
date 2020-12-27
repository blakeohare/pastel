VERSION = '2.9.0'

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
		print("usage: python release.py windows|mac|linux")
		return
	
	isWindows = False
	isMac = False
	isLinux = False

	dotnet_platform = ''
	platform_name = args[0]
	if args[0] == 'windows':
		isWindows = True
		dotnet_platform = 'win-x64'
	elif args[0] == 'mac':
		isMac = True
		dotnet_platform = 'osx-x64'
	elif args[0] == 'linux':
		isLinux = True
		print("Linux support not quite tested yet.")
		return
	else:
		print("Invalid platform: " + args[0])
		return
	
	copy_to_dir = 'pastel-' + VERSION + '-' + platform_name
	executable_path = ''
	if isWindows:
		executable_dir = os.path.join('..', 'Source', 'bin', 'Release', 'netcoreapp3.1', dotnet_platform, 'publish')
		executable_path = os.path.join(executable_dir, 'Pastel.exe')
	else:
		print("This hasn't been tested yet.")
		return
	
	prepareCleanDirectory(copy_to_dir)
	
	if os.path.exists(executable_dir):
		shutil.rmtree(executable_dir)
	
	cmd = ' '.join([
		'dotnet publish',
		os.path.join('..', 'Source', 'Pastel.sln'),
		'-c Release',
		'-r', dotnet_platform,
		'--self-contained true',
		'-p:PublishTrimmed=true',
		'-p:PublishSingleFile=true'
	])
	result = runCommand(cmd)
	
	shutil.copyfile(executable_path, os.path.join(copy_to_dir, 'pastel.exe'))
	
	print("Release directory created: " + copy_to_dir)

main(sys.argv[1:])
