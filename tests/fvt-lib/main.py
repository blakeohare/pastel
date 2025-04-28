import gen

def main():
    gen.runner()

if __name__ == '__main__':
    try:
        main()
    except e:
        print("FAIL!")
        print(e)
