VERSION = '2.9.0'
DOT_NET_VER = 'net8.0'

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
		print("usage: python release.py platform")
		print("platform options:")
		print("  windows")
		print("  mac")
		print("  linux")
		print("  win31 <-- NOT Windows 3.1. Windows using .NET Core 3.1")
		return

	isWindows = False
	isMac = False
	isLinux = False

	dotnet_platform = ''
	platform_name = args[0]
	use_dotnet_core_31 = False
	if args[0] == 'win31':
		args[0] = 'windows'
		use_dotnet_core_31 = True

	if args[0] == 'windows':
		isWindows = True
		dotnet_platform = 'win-x64'
		final_exec_name = 'pastel.exe'
	elif args[0] == 'mac':
		isMac = True
		dotnet_platform = 'osx-x64'
		final_exec_name = 'pastel'
	elif args[0] == 'linux':
		isLinux = True
		print("Linux support not quite tested yet.")
		return
	else:
		print("Invalid platform: " + args[0])
		return

	copy_to_dir = 'pastel-' + VERSION + '-' + platform_name
	executable_path = ''
	net_dir = 'netcoreapp3.1' if use_dotnet_core_31 else DOT_NET_VER
	sln_name = 'PastelLegacy.sln' if use_dotnet_core_31 else 'Pastel.sln'
	if isWindows:
		executable_dir = os.path.join('..', 'Source', 'bin', 'Release', net_dir, dotnet_platform, 'publish')
		executable_path = os.path.join(executable_dir, 'Pastel.exe')
	elif isMac:
		executable_dir = os.path.join('..', 'Source', 'bin', 'Release', net_dir, dotnet_platform, 'publish')
		executable_path = os.path.join(executable_dir, 'Pastel')
	else:
		print("This hasn't been tested yet.")
		return

	prepareCleanDirectory(copy_to_dir)

	if os.path.exists(executable_dir):
		shutil.rmtree(executable_dir)

	cmd = ' '.join([
		'dotnet publish',
		os.path.join('..', 'Source', sln_name),
		'-c Release',
		'-r', dotnet_platform,
		'--self-contained true',
		'-p:PublishTrimmed=true',
		'-p:PublishSingleFile=true'
	])
	result = runCommand(cmd)

	dest_exec_path = os.path.join(copy_to_dir, final_exec_name)
	shutil.copyfile(executable_path, dest_exec_path)
	if isMac:
		runCommand('chmod 0777 ' + dest_exec_path)
	print("Release directory created: " + copy_to_dir)

main(sys.argv[1:])
