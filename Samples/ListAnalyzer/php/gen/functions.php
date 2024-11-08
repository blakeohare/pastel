<?php

	class FunctionWrapper {
		private static function PST_sortedCopyOfIntArray($nums) {
			$o = new PastelPtrArray();
			$o->arr = array_merge($nums->arr, array());
			sort($o->arr);
			return $o;
		}

		private static $PST_ExtCallbacks = array();

		private static function PST_RegisterExtensibleCallback($name, $fnObj) {
			static::$PST_ExtCallbacks[$name] = $fnObj;
		}

		public static function calculate_standard_deviation($nums, $length, $mean) {
			$total_dev = 0.0;
			$i = 0;
			while (($i < $length)) {
				$diff = ($nums->arr[$i] - $mean);
				$total_dev += pow($diff, 2);
				$i += 1;
			}
			return pow((($total_dev) / ($length)), 0.5);
		}

		public static function NumAnalysis_getCount($na) {
			return $na->count;
		}

		public static function NumAnalysis_getMax($na) {
			return $na->max;
		}

		public static function NumAnalysis_getMean($na) {
			return $na->mean;
		}

		public static function NumAnalysis_getMedian($na) {
			return $na->median;
		}

		public static function NumAnalysis_getMin($na) {
			return $na->min;
		}

		public static function NumAnalysis_getStdDev($na) {
			return $na->std_dev;
		}

		public static function NumAnalysis_getTotal($na) {
			return $na->total;
		}

		public static function perform_analysis($nums, $length) {
			$testValue = "Strings ought to be tested to some degree.\n";
			$output = new NumAnalysis(0, 0, 0, 0, 0.0, 0.0, 0.0);
			$output->count = $length;
			if (($length > 0)) {
				$output->min = $nums->arr[0];
				$output->max = $nums->arr[0];
				$output->total = 0;
				$i = 0;
				while (($i < $length)) {
					$value = $nums->arr[$i];
					$output->total += $value;
					if (($value < $output->min)) {
						$output->min = $value;
					}
					if (($value > $output->max)) {
						$output->max = $value;
					}
					$i += 1;
				}
				$output->mean = (((1.0 * $output->total)) / ($length));
				$output->std_dev = self::calculate_standard_deviation($nums, $length, $output->mean);
				$nums_copy = self::PST_sortedCopyOfIntArray($nums);
				if ((($length % 2) == 0)) {
					$output->median = ((($nums_copy->arr[(intval(($length) / (2)) - 1)] + $nums_copy->arr[intval(($length) / (2))])) / (2.0));
				} else {
					$output->median = (0.0 + $nums_copy->arr[intval(($length) / (2))]);
				}
			}
			return $output;
		}
	}

?>