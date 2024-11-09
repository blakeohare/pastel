def PST_sortedCopyOfList(t):
  t = t[:]
  t.sort()
  return t

PST_ExtCallbacks = {}

def PST_RegisterExtensibleCallback(name, fn):
  PST_ExtCallbacks[name] = fn;

def calculate_standard_deviation(nums, length, mean):
  total_dev = 0.0
  i = 0
  while i < length:
    diff = nums[i] - mean
    total_dev += diff ** 2
    i += 1
  return (total_dev / length) ** 0.5

def NumAnalysis_getCount(na):
  return na[0]

def NumAnalysis_getMax(na):
  return na[2]

def NumAnalysis_getMean(na):
  return na[4]

def NumAnalysis_getMedian(na):
  return na[5]

def NumAnalysis_getMin(na):
  return na[1]

def NumAnalysis_getStdDev(na):
  return na[6]

def NumAnalysis_getTotal(na):
  return na[3]

def perform_analysis(nums, length):
  testValue = "Strings ought to be tested to some degree.\n"
  output = [0, 0, 0, 0, 0.0, 0.0, 0.0]
  output[0] = length
  if length > 0:
    output[1] = nums[0]
    output[2] = nums[0]
    output[3] = 0
    i = 0
    while i < length:
      value = nums[i]
      output[3] += value
      if value < output[1]:
        output[1] = value
      if value > output[2]:
        output[2] = value
      i += 1
    output[4] = 1.0 * output[3] / length
    output[6] = calculate_standard_deviation(nums, length, output[4])
    nums_copy = PST_sortedCopyOfList(nums)
    if length % 2 == 0:
      output[5] = (nums_copy[length // 2 - 1] + nums_copy[length // 2]) / 2.0
    else:
      output[5] = 0.0 + nums_copy[length // 2]
  return output
