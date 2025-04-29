import json
import os
import random
import sys

ALL_FVT_PLATFORMS = ['csharp', 'java', 'js', 'python']

PYTHON_COMMAND = 'python' if os.name == 'nt' else 'python3'

FAIL_STR = '*FAIL!*'

def file_read_text(path):
    c = open(path, 'rb')
    t = c.read().decode('utf-8').replace('\r\n', '\n')
    c.close()
    return t

def file_write_text(path, content):
    c = open(path, 'wb')
    c.write(content.encode('utf-8'))
    c.close()

def run_command(ex, args = None, cwd = None):
    cmd = ex
    if args != None: cmd += ' ' + ' '.join(args)
    old_dir = os.getcwd()
    try:
        if cwd != None:
            os.chdir(cwd)
        c = os.popen(cmd)
        t = c.read()
        c.close()
    finally:
        if cwd != None:
            os.chdir(old_dir)

    return t

def generate_csharp_guid():
    HEX = list('0123456789ABCDEF')
    output = list('HHHHHHHH-HHHH-HHHH-HHHH-HHHHHHHHHHHH')
    for i in range(len(output)):
        if output[i] == 'H':
            output[i] = random.choice(HEX)
    return ''.join(output)

def run_fvt_tests(pastel_exec_path, platforms):
    fvt_dir = os.path.join('tests', 'fvt')
    test_libs = {}
    fvt_lib_dir = os.path.join('tests', 'fvt-lib')
    for file in os.listdir(fvt_lib_dir):
        test_libs[file] = file_read_text(os.path.join(fvt_lib_dir, file))

    sln_code = test_libs['PastelTest.sln']
    sln_code = sln_code.replace('PROJ_GUID', generate_csharp_guid())
    sln_code = sln_code.replace('SOLUTION_GUID', generate_csharp_guid())
    test_libs['PastelTest.sln'] = sln_code

    test_ids = [file[:-len('.pst')] for file in os.listdir(fvt_dir) if file.endswith('.pst')]
    for test_id in test_ids:
        test_code = file_read_text(os.path.join(fvt_dir, test_id + '.pst'))
        dst_dir = get_temp_dir(test_id)
        files = test_libs.copy()
        files['test.json'] = json.dumps({
            'source': 'test.pst',
            'targets': [
                create_csharp_target('csharp', 'PastelTest.GeneratedCode', 'FunctionWrapper.cs', 'csgen'),
                create_java_target('java', 'FunctionWrapper.java', '.'),
                create_javascript_target('js', 'gen.js'),
                create_python_target('python', 'pygen/__init__.py'),
            ]
        }, indent = 2)
        files['test.pst'] = test_code
        for file in files.keys():
            file_write_text(os.path.join(dst_dir, file), files[file])

        build_path = os.path.join(dst_dir, 'test.json')
        all_pass = True
        for platform in platforms:
            print("Running FVT: " + test_id + " [" + platform + "]")
            result = run_command(pastel_exec_path, [build_path, platform]).strip()

            if result != '':
                print(FAIL_STR + " -- Pastel compilation")
                print(result)
                all_pass = False
                break

            if platform == 'js':
                # TODO: add option to apply default export to exported JS code.
                gen_js_path = os.path.join(dst_dir, 'gen.js')
                file_write_text(
                    gen_js_path,
                    file_read_text(gen_js_path) + '\n\n' + 'export default { runner, registerExtension: PASTEL_regCallback };\n')

                node_result = run_command('node', ['index.js'], cwd = dst_dir).strip()
                if node_result != '':
                    print(FAIL_STR)
                    print(node_result)
                    all_pass = False
                    break

            elif platform == 'python':
                py_result = run_command(PYTHON_COMMAND, ['main.py'], cwd = dst_dir)
                if py_result != '':
                    print(FAIL_STR)
                    print(py_result)
                    all_pass = False
                    break

            elif platform == 'java':
                javac_result = run_command('javac', ['*.java'], cwd = dst_dir).strip()
                if javac_result != '':
                    print(FAIL_STR + ' -- Java compilation')
                    print(javac_result)
                    all_pass = False
                    break

                java_result = run_command('java', ['PastelTest'], cwd = dst_dir).strip()
                if java_result != '':
                    print(FAIL_STR)
                    print(java_result)
                    all_pass = False
                    break

            elif platform == 'csharp':
                csc_result = run_command('dotnet', ['build', 'PastelTest.sln'], cwd = dst_dir).strip()
                if 'Build succeeded.' not in csc_result:
                    print(FAIL_STR + ' -- C# compilation')
                    print(csc_result)
                    all_pass = False
                    break

                cs_run_result = run_command(os.path.join('bin', 'Debug', 'net8.0', 'PastelTest'), [], cwd = dst_dir).strip()
                if cs_run_result != '':
                    print(FAIL_STR)
                    print(csc_result)
                    all_pass = False
                    break

            else:
                raise Exception("TODO: implement automatic runner for this platform")

        if all_pass:
            pass # TODO: delete directory

def split_negative_test_file(content, throw_path):
    lines = content.replace('\r\n', '\n').split('\n')
    for i in range(len(lines)):
        line = lines[i].strip()
        if len(line) >= 3 and len(line) * '#' == line:
            return ('\n'.join(lines[:i]).strip(), '\n'.join(lines[i + 1:]).strip())
    raise Exception("Invalid test file: " + throw_path)

def create_java_target(name, func_path, struct_path):
    if not func_path.endswith('.java'): raise Exception()
    return {
        'name': name,
        'language': 'java',
        'output': {
            'structs-path': struct_path,
            'functions-path': func_path,
            'functions-wrapper-class': func_path[:-len('.java')],
        }
    }

def create_csharp_target(name, ns, func_path, struct_path):
    if not func_path.endswith('.cs'): raise Exception()
    return {
        'name': name,
        'language': 'csharp',
        'imports': [
            'System.Collections.Generic',
        ],
        'output': {
            'namespace': ns,
            'structs-path': struct_path,
            'functions-path': func_path,
            'functions-wrapper-class': func_path[:-len('.cs')],
        }
    }

def create_javascript_target(name, func_path):
    return {
        'name': name,
        'language': 'javascript',
        'output': { 'functions-path': func_path, }
    }

def create_python_target(name, func_path):
    return {
        'name': name,
        'language': 'python',
        'output': {  'functions-path': func_path }
    }

def run_error_tests(pastel_exec_path):
    error_dir = os.path.join('tests', 'errors')
    test_ids = [f[:-len('.txt')] for f in os.listdir(error_dir) if f.lower().endswith('.txt')]
    for test_id in test_ids:
        dst_path = get_temp_dir(test_id)
        test_path = os.path.join(error_dir, test_id + '.txt')
        test_content = file_read_text(test_path)
        code, expected = split_negative_test_file(test_content, test_path)
        code_path = os.path.abspath(os.path.join(dst_path, 'test.pst'))
        lang_id = 'js'
        if test_id.endswith(']'):
            lang_id = test_id.split('[').pop()[:-1]
        build_file = {
            'source': 'test.pst',
            'targets': [
                {
                    'csharp': create_csharp_target('test', 'PastelGenerated', 'FunctionWrapper.cs', '.'),
                    'java': create_java_target('test', 'FunctionWrapper.java', '.'),
                    'js': create_javascript_target('test', 'gen.js'),
                    'python': create_python_target('test', 'gen.py'),
                }[lang_id]
            ]
        }
        build_path = os.path.join(dst_path, 'test.json')
        file_write_text(
            build_path,
            json.dumps(build_file, indent = 2))
        file_write_text(
            code_path,
            code)

        actual = run_command(pastel_exec_path, [build_path, 'test']).strip().replace('\r\n', '\n')
        actual = actual.replace(code_path, 'test.pst')
        print("Running Error Test: " + test_id)
        if expected != actual:
            print("FAIL!")
            print("BUILD FILE:")
            print("  " + os.path.abspath(build_path))
            print('-' * 40)
            print("Expected:\n" + expected)
            print('-' * 40)
            print("Actual:\n" + actual)
            print('-' * 40)
        else:
            pass # TODO: delete the tmp directory

LETTERS = 'abcdefghijklmnopqrstuvwyxz'
CHARS = list(LETTERS + LETTERS.upper() + '0123456789')
def gen_gibberish(sz):
    sb = []
    while len(sb) < sz:
        sb.append(random.choice(CHARS))
    return ''.join(sb)

def ensure_dir_exists(path):
    os.makedirs(path, exist_ok = True)

def get_temp_dir(hint = None):
    name = gen_gibberish(10)
    if hint != None: name = hint + '_' + name
    path = os.path.join('tests', 'tmp', name)
    ensure_dir_exists(path)
    return path

def main(args):
    if len(args) == 0:
        print("Usage: python testrunner.py path/to/pastel/binary/pastel[.exe] --errtests --fvt:[ all | " + ' | '.join(ALL_FVT_PLATFORMS) + "]")
        print("")
        print("e.g. python testrunner.py ./bin/pastel --errtests --fvt:js --fvt:python")
        return

    pastel_path = args[0]
    if not os.path.exists(pastel_path):
        print("Path does not exist: " + pastel_path[:999])
        return

    fvt_flags = []
    enable_err = False
    for arg in args[1:]:
        if arg.startswith('--fvt:'):
            fvt_arg = arg[len('--fvt:'):]
            if fvt_arg == 'all':
                fvt_flags += ALL_FVT_PLATFORMS
            elif fvt_arg not in ALL_FVT_PLATFORMS:
                print("Unrecognized FVT arg: " + arg)
                return
            else:
                fvt_flags.append(fvt_arg)
        elif arg == '--errtests':
            enable_err = True
        else:
            print("Unknown arg: " + arg)
            return

    fvt_platforms = list(set(fvt_flags))

    if len(fvt_platforms):
        run_fvt_tests(pastel_path, fvt_platforms)
    if enable_err:
        run_error_tests(pastel_path)

if __name__ == '__main__':
    try:
        main(sys.argv[1:])
    except Exception as ex:
        print(FAIL_STR)
        print(ex)
