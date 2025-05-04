import pygen

def on_fail(msg):
  raise Exception(msg)

pygen.PST_RegisterExtensibleCallback('fail', lambda args: on_fail(args[0]))

def main():
  pygen.V_runner()

if __name__ == '__main__':
  try:
    main()
  except Exception as e:
    print("FAIL!")
    print(e)
