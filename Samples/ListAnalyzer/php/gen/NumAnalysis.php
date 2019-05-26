<?php

  class NumAnalysis {
    var count;
    var min;
    var max;
    var total;
    var mean;
    var median;
    var std_dev;
    function __construct($a0, $a1, $a2, $a3, $a4, $a5, $a6) {
      $this->count = $a0;
      $this->min = $a1;
      $this->max = $a2;
      $this->total = $a3;
      $this->mean = $a4;
      $this->median = $a5;
      $this->std_dev = $a6;
    }
  }

?>