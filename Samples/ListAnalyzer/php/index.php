<?php

	// In lieu of command line args, send a request to this PHP script with GET
	// parameters for a variable called nums, using spaces (which are +'s in an escaped URL).
	// e.g. mysite.com/listanalyzer/index.php?nums=4+5+6

	require 'gen/gen_classes.php';
	require 'gen/functions.php';
	
	$nums_raw = explode(' ', $_GET['nums']);
	$nums = array();
	for ($i = 0; $i < count($nums_raw); ++$i) {
		array_push($nums, intval($nums_raw[$i]));
	}
	
	$output = PastelGeneratedCode::perform_analysis(pastelWrapList($nums), count($nums));
	
	echo '<html><body><table>';
	echo '<tr><td>Count:</td><td>' . $output->count . '</td></tr>';
	echo '<tr><td>Min:</td><td>' . $output->min . '</td></tr>';
	echo '<tr><td>Max:</td><td>' . $output->max . '</td></tr>';
	echo '<tr><td>Mean:</td><td>' . $output->mean . '</td></tr>';
	echo '<tr><td>Standard Deviation:</td><td>' . $output->std_dev . '</td></tr>';
	echo '<tr><td>Median:</td><td>' . $output->median . '</td></tr>';
	echo '</table></body></html>';
?>