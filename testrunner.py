import json
import os
import random
import sys

def file_read_text(path):
    c = open(path, 'rt')
    t = c.read().replace('\r\n', '\n')
    c.close()
    return t

def file_write_text(path, content):
    c = open(path, 'wb')
    c.write(content.encode('utf-8'))
    c.close()

def run_command(ex, args = None):
    cmd = ex
    if args != None: cmd += ' ' + ' '.join(args)
    c = os.popen(cmd)
    t = c.read()
    c.close()
    return t

def split_negative_test_file(content, throw_path):
    lines = content.replace('\r\n', '\n').split('\n')
    for i in range(len(lines)):
        line = lines[i].strip()
        if len(line) >= 3 and len(line) * '#' == line:
            return ('\n'.join(lines[:i]).strip(), '\n'.join(lines[i + 1:]).strip())
    raise Exception("Invalid test file: " + throw_path)

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
                    'js': { 'name': 'test', 'language': 'javascript', 'output': { 'functions-path': 'gen.js' } },
                    'php': { 'name': "php", "language": "php", "output": { "structs-path": "structs", "namespace": "PTest", "functions-path": "gen_functions.php", "functions-wrapper-class": "FunctionWrapper" } }
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
        print("Running: " + test_id)
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
        print("Usage: python testrunner.py path/to/pastel/binary/pastel[.exe]")
        return

    pastel_path = args[0]
    if not os.path.exists(pastel_path):
        print("Path does not exist: " + pastel_path[:999])
        return

    run_error_tests(pastel_path)

if __name__ == '__main__':
    main(sys.argv[1:])
