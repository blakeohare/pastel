class NumAnalysis:
  def __init__(self, count, min, max, total, mean, median, std_dev):
    self.count = count
    self.min = min
    self.max = max
    self.total = total
    self.mean = mean
    self.median = median
    self.std_dev = std_dev

def calculate_standard_deviation(nums, length, mean):
  total_dev = 0.0
  i = 0
  while (i < length):
    diff = (nums[i] - mean)
    total_dev += (diff ** 2)
    i += 1
  return (total_dev) ** .5

def perform_analysis(nums, length):
  output = NumAnalysis(0, 0, 0, 0, 0.0, 0.0, 0.0)
  output.count = length
  if (length > 0):
    output.min = nums[0]
    output.max = nums[0]
    output.total = 0
    i = 0
    while (i < length):
      value = nums[i]
      output.total += value
      if (value < output.min):
        output.min = value
      if (value > output.max):
        output.max = value
      i += 1
    output.mean = (1.0 * (output.total) / (length))
    output.std_dev = calculate_standard_deviation(nums, length, output.mean)
    nums_copy = (nums)[:]
    (nums_copy).sort()
    if ((length % 2) == 0):
      output.median = (1.0 * ((nums[((length) // (2) - 1)] + nums[(length) // (2)])) / (2.0))
    else:
      output.median = (0.0 + nums[(length) // (2)])
  return output

