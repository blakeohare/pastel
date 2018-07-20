from gen import *

nums = raw_input("Type some integers: ")
nums = list(map(int, nums))
analysis = perform_analysis(nums)

print("Count:", analysis.count)
print("Min:", analysis.min)
print("Max:", analysis.max)
print("Mean:", analysis.mean)
print("Standard Deviation:", analysis.std_dev)
print("Median:", analysis.median)
