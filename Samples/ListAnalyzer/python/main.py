import sys

from gen import perform_analysis

def main():
	if len(sys.argv) == 1:
		print("Usage: python main.py num1 num2 num3 numr4 ...")
		print("e.g.:")
		print("  python main.py 5 2 9 4 11 13")
		return

	nums = list(map(int, sys.argv[1:]))
	analysis = perform_analysis(nums, len(nums))

	print("Count: " + str(analysis.count))
	print("Min: " + str(analysis.min))
	print("Max: " + str(analysis.max))
	print("Mean: " + str(analysis.mean))
	print("Standard Deviation: " + str(analysis.std_dev))
	print("Median: " + str(analysis.median))

if __name__ == "__main__":
	main()
