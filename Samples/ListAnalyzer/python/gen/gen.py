def v_calculate_standard_deviation(v_nums, v_length, v_mean):
  v_total_dev = 0.0
  v_i = 0
  while (v_i < v_length):
    v_diff = (v_nums[v_i] - v_mean)
    v_total_dev += (v_diff ** 2)
    v_i += 1
  return (v_total_dev) ** .5

def v_perform_analysis(v_nums, v_length):
  v_output = [0, 0, 0, 0, 0.0, 0.0, 0.0]
  v_output[0] = v_length
  if (v_length > 0):
    v_output[1] = v_nums[0]
    v_output[2] = v_nums[0]
    v_output[3] = 0
    v_i = 0
    while (v_i < v_length):
      v_value = v_nums[v_i]
      v_output[3] += v_value
      if (v_value < v_output[1]):
        v_output[1] = v_value
      if (v_value > v_output[2]):
        v_output[2] = v_value
      v_i += 1
    v_output[4] = (1.0 * (v_output[3]) / (v_length))
    v_output[6] = v_calculate_standard_deviation(v_nums, v_length, v_output[4])
    v_nums_copy = (v_nums)[:]
    (v_nums_copy.sort()
    if ((v_length % 2) == 0):
      v_output[5] = (1.0 * ((v_nums[((v_length) // (2) - 1)] + v_nums[(v_length) // (2)])) / (2.0))
    else:
      v_output[5] = (0.0 + v_nums[(v_length) // (2)])
  return v_output

