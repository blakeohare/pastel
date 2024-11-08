const [calculate_standard_deviation, NumAnalysis_getCount, NumAnalysis_getMax, NumAnalysis_getMean, NumAnalysis_getMedian, NumAnalysis_getMin, NumAnalysis_getStdDev, NumAnalysis_getTotal, perform_analysis] = (() => {
let PST$sortedCopyOfArray = v => {
	let o = [...v];
	if (o.length < 2) return o;
	if (typeof(o[0]) === 'number') return o.sort((a, b) => a - b);
	return o.sort();
};

let PST$extCallbacks = {};

let PST$registerExtensibleCallback = (name, fn) => { PST$extCallbacks[name] = fn; };

let calculate_standard_deviation = function(nums, length, mean) {
	let total_dev = 0.0;
	let i = 0;
	while ((i < length)) {
		let diff = (nums[i] - mean);
		total_dev += Math.pow(diff, 2);
		i += 1;
	}
	return Math.pow((total_dev / length), 0.5);
};

let NumAnalysis_getCount = function(na) {
	return na[0];
};

let NumAnalysis_getMax = function(na) {
	return na[2];
};

let NumAnalysis_getMean = function(na) {
	return na[4];
};

let NumAnalysis_getMedian = function(na) {
	return na[5];
};

let NumAnalysis_getMin = function(na) {
	return na[1];
};

let NumAnalysis_getStdDev = function(na) {
	return na[6];
};

let NumAnalysis_getTotal = function(na) {
	return na[3];
};

let perform_analysis = function(nums, length) {
	let testValue = "Strings ought to be tested to some degree.\n";
	let output = [0, 0, 0, 0, 0.0, 0.0, 0.0];
	output[0] = length;
	if ((length > 0)) {
		output[1] = nums[0];
		output[2] = nums[0];
		output[3] = 0;
		let i = 0;
		while ((i < length)) {
			let value = nums[i];
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
		let nums_copy = PST$sortedCopyOfArray(nums);
		if (((length % 2) == 0)) {
			output[5] = ((nums_copy[(Math.floor(length / 2) - 1)] + nums_copy[Math.floor(length / 2)]) / 2.0);
		} else {
			output[5] = (0.0 + nums_copy[Math.floor(length / 2)]);
		}
	}
	return output;
};

return [calculate_standard_deviation, NumAnalysis_getCount, NumAnalysis_getMax, NumAnalysis_getMean, NumAnalysis_getMedian, NumAnalysis_getMin, NumAnalysis_getStdDev, NumAnalysis_getTotal, perform_analysis];
})();
