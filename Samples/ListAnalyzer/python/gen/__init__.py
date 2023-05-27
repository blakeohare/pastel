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
  while (i < length):
    diff = (nums[i] - mean)
    total_dev += (diff ** 2)
    i += 1
  return ((1.0 * (total_dev) / (length)) ** 0.5)

def perform_analysis(nums, length):
  output = [0, 0, 0, 0, 0.0, 0.0, 0.0]
  output[0] = length
  if (length > 0):
    output[1] = nums[0]
    output[2] = nums[0]
    output[3] = 0
    i = 0
    while (i < length):
      value = nums[i]
      output[3] += value
      if (value < output[1]):
        output[1] = value
      if (value > output[2]):
        output[2] = value
      i += 1
    output[4] = (1.0 * ((1.0 * output[3])) / (length))
    output[6] = calculate_standard_deviation(nums, length, output[4])
    nums_copy = PST_sortedCopyOfList(nums)
    if ((length % 2) == 0):
      output[5] = (1.0 * ((nums_copy[((length) // (2) - 1)] + nums_copy[(length) // (2)])) / (2.0))
    else:
      output[5] = (0.0 + nums_copy[(length) // (2)])
  return output
