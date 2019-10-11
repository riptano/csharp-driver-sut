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
set label '128 requests' at 1,200 rotate center font 'Verdana,10'
set label '256 requests' at 2,200 rotate center font 'Verdana,10'
set label '512 requests' at 3,200 rotate center font 'Verdana,10'
set xrange [0:20.5]
input1 = sprintf("throughput-%s-%s-%s-%s.csv", compare, profile, framework, type)
input2 = sprintf("throughput-%s-%s-%s-%s.csv", current, profile, framework, type)

plot \
    input1 using (0.8):2:(0.1):1 title compare ,\
    input2 using (1):2:(0.1):1 title current