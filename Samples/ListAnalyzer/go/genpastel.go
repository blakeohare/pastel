package main

import (
	"math"
	"sort"
)

type ExtFunc func(array []interface{}) interface{}

var PST_ExtCallbacks map[string]ExtFunc

func PST_RegisterExtensibleCallback(name string, fn ExtFunc) {
	PST_ExtCallbacks[name] = fn
}

func PST_SortedIntArrayCopy(nums []int) []int {
	copied := nums[:]
	sort.Ints(copied)
	return copied
}

func fn_calculate_standard_deviation(v_nums []int, v_length int, v_mean float64) float64 {
	var v_total_dev float64 = 0.0
	var v_i int = 0
	for (v_i) < (v_length) {
		var v_diff float64 = (float64(v_nums[v_i]) - (v_mean))
		v_total_dev += math.Pow(v_diff, 2)
		v_i += 1
	}
	return math.Pow(((v_total_dev) / float64(v_length)), 0.5)
}

func fn_perform_analysis(v_nums []int, v_length int) PtrBox_NumAnalysis {
	var v_output PtrBox_NumAnalysis = PtrBox_NumAnalysis{o: &S_NumAnalysis{f_count: 0, f_min: 0, f_max: 0, f_total: 0, f_mean: 0.0, f_median: 0.0, f_std_dev: 0.0}}
	v_output.o.f_count = v_length
	if (v_length) > (0) {
		v_output.o.f_min = v_nums[0]
		v_output.o.f_max = v_nums[0]
		v_output.o.f_total = 0
		var v_i int = 0
		for (v_i) < (v_length) {
			var v_value int = v_nums[v_i]
			v_output.o.f_total += v_value
			if (v_value) < (v_output.o.f_min) {
				v_output.o.f_min = v_value
			}
			if (v_value) > (v_output.o.f_max) {
				v_output.o.f_max = v_value
			}
			v_i += 1
		}
		v_output.o.f_mean = (((1.0) * float64(v_output.o.f_total)) / float64(v_length))
		v_output.o.f_std_dev = fn_calculate_standard_deviation(v_nums, v_length, v_output.o.f_mean)
		var v_nums_copy []int = PST_SortedIntArrayCopy(v_nums)
		if ((v_length) % (2)) == (0) {
			v_output.o.f_median = (float64(((v_nums_copy[(((v_length) / (2)) - (1))]) + (v_nums_copy[((v_length) / (2))]))) / (2.0))
		} else {
			v_output.o.f_median = ((0.0) + float64(v_nums_copy[((v_length)/(2))]))
		}
	}
	return v_output
}
