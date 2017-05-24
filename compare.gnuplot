set term png size 1600,1200
set output outputfile
set title sprintf("Per-driver %s throughput vs. number of concurrent requests", type)
set noxtics
set ytics nomirror
set yrange [0:]
set key left
set bmargin 2
set style fill solid 0.25 border lt -1
set style data boxplot
set style boxplot nooutliers
set bars 0.2
set boxwidth 0.1
set border 2
set label '10 requests' at 1,200 rotate center font 'Verdana,10'
set label '20 requests' at 2,200 rotate center font 'Verdana,10'
set label '30 requests' at 3,200 rotate center font 'Verdana,10'
set label '40 requests' at 4,200 rotate center font 'Verdana,10'
set label '50 requests' at 5,200 rotate center font 'Verdana,10'
set label '70 requests' at 6,200 rotate center font 'Verdana,10'
set label '90 requests' at 7,200 rotate center font 'Verdana,10'
set label '110 requests' at 8,200 rotate center font 'Verdana,10'
set label '130 requests' at 9,200 rotate center font 'Verdana,10'
set label '150 requests' at 10,200 rotate center font 'Verdana,10'
set label '180 requests' at 11,200 rotate center font 'Verdana,10'
set label '210 requests' at 12,200 rotate center font 'Verdana,10'
set label '240 requests' at 13,200 rotate center font 'Verdana,10'
set label '270 requests' at 14,200 rotate center font 'Verdana,10'
set label '300 requests' at 15,200 rotate center font 'Verdana,10'
set label '340 requests' at 16,200 rotate center font 'Verdana,10'
set label '380 requests' at 17,200 rotate center font 'Verdana,10'
set label '420 requests' at 18,200 rotate center font 'Verdana,10'
set label '460 requests' at 19,200 rotate center font 'Verdana,10'
set label '500 requests' at 20,200 rotate center font 'Verdana,10'
set xrange [0:20.5]
input1 = sprintf("throughput-%s-%s-%s.csv", compare, profile, type)
input2 = sprintf("throughput-%s-%s-%s.csv", current, profile, type)

plot \
    input1 using (0.8):2:(0.1):1 title compare ,\
    input2 using (1):2:(0.1):1 title current