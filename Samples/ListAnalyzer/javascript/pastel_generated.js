var calculate_standard_deviation = function(nums, length, mean) {
	var total_dev = 0.0;
	var i = 0;
	while ((i < length)) {
		var diff = (nums[i] - mean);
		total_dev += Math.pow(diff, 2);
		i += 1;
	}
	return Math.sqrt((total_dev / length));
};

var perform_analysis = function(nums, length) {
	var output = {count: 0, min: 0, max: 0, total: 0, mean: 0.0, median: 0.0, std_dev: 0.0};
	output.count = length;
	if ((length > 0)) {
		output.min = nums[0];
		output.max = nums[0];
		output.total = 0;
		var i = 0;
		while ((i < length)) {
			var value = nums[i];
			output.total += value;
			if ((value < output.min)) {
				output.min = value;
			}
			if ((value > output.max)) {
				output.max = value;
			}
			i += 1;
		}
		output.mean = ((1.0 * output.total) / length);
		output.std_dev = calculate_standard_deviation(nums, length, output.mean);
		var nums_copy = (nums).concat([]);
		(nums_copy).sort();
		if (((length % 2) == 0)) {
			output.median = ((nums_copy[(Math.floor(length / 2) - 1)] + nums_copy[Math.floor(length / 2)]) / 2.0);
		} else {
			output.median = (0.0 + nums_copy[Math.floor(length / 2)]);
		}
	}
	return output;
};

