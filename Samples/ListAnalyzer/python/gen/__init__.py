def PST_sortedCopyOfList(t):
  t = t[:]
  t.sort()
  return t

PST_ExtCallbacks = {}

def PST_RegisterExtensibleCallback(name, fn):
  PST_ExtCallbacks[name] = fn

def V_calculate_standard_deviation(V_nums, V_length, V_mean):
  V_total_dev = 0.0
  V_i = 0
  while V_i < V_length:
    V_diff = V_nums[V_i] - V_mean
    V_total_dev += V_diff ** 2
    V_i += 1
  return (V_total_dev / V_length) ** 0.5

def V_NumAnalysis_getCount(V_na):
  return V_na[0]

def V_NumAnalysis_getMax(V_na):
  return V_na[2]

def V_NumAnalysis_getMean(V_na):
  return V_na[4]

def V_NumAnalysis_getMedian(V_na):
  return V_na[5]

def V_NumAnalysis_getMin(V_na):
  return V_na[1]

def V_NumAnalysis_getStdDev(V_na):
  return V_na[6]

def V_NumAnalysis_getTotal(V_na):
  return V_na[3]

def V_perform_analysis(V_nums, V_length):
  V_testValue = "Strings ought to be tested to some degree.\n"
  V_output = [0, 0, 0, 0, 0.0, 0.0, 0.0]
  V_output[0] = V_length
  if V_length > 0:
    V_output[1] = V_nums[0]
    V_output[2] = V_nums[0]
    V_output[3] = 0
    V_i = 0
    while V_i < V_length:
      V_value = V_nums[V_i]
      V_output[3] += V_value
      if V_value < V_output[1]:
        V_output[1] = V_value
      if V_value > V_output[2]:
        V_output[2] = V_value
      V_i += 1
    V_output[4] = 1.0 * V_output[3] / V_length
    V_output[6] = V_calculate_standard_deviation(V_nums, V_length, V_output[4])
    V_nums_copy = PST_sortedCopyOfList(V_nums)
    if V_length % 2 == 0:
      V_output[5] = (V_nums_copy[V_length // 2 - 1] + V_nums_copy[V_length // 2]) / 2.0
    else:
      V_output[5] = 0.0 + V_nums_copy[V_length // 2]
  return V_output