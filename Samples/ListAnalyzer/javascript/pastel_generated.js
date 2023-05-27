PST$sortedCopyOfArray = function(n) {
	var a = n.concat([]);
	a.sort();
	return a;
};

PST$extCallbacks = {};

PST$registerExtensibleCallback = (name, fn) => { PST$extCallbacks[name] = fn; };

var calculate_standard_deviation = function(nums, length, mean) {
	var total_dev = 0.0;
	var i = 0;
	while ((i < length)) {
		var diff = (nums[i] - mean);
		total_dev += Math.pow(diff, 2);
		i += 1;
	}
	return Math.pow((total_dev / length), 0.5);
};

var perform_analysis = function(nums, length) {
	var output = [0, 0, 0, 0, 0.0, 0.0, 0.0];
	output[0] = length;
	if ((length > 0)) {
		output[1] = nums[0];
		output[2] = nums[0];
		output[3] = 0;
		var i = 0;
		while ((i < length)) {
			var value = nums[i];
			output[3] += value;
			if ((value < output[1])) {
				output[1] = value;
			}
			if ((value > output[2])) {
				output[2] = value;
			}
			i += 1;
		}
		output[4] = ((1.0 * output[3]) / length);
		output[6] = calculate_standard_deviation(nums, length, output[4]);
		var nums_copy = PST$sortedCopyOfArray(nums);
		if (((length % 2) == 0)) {
			output[5] = ((nums_copy[(Math.floor(length / 2) - 1)] + nums_copy[Math.floor(length / 2)]) / 2.0);
		} else {
			output[5] = (0.0 + nums_copy[Math.floor(length / 2)]);
		}
	}
	return output;
};
