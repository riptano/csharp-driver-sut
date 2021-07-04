set yrange [:]
set xrange [:]
set offsets graph 0, graph 0, graph 0.15, graph 0.1

set term png size 800,800
set output outputfile
set title sprintf("%s vs. number of concurrent requests | %s %s", label, platform, framework) noenhanced
set noxtics
set ytics nomirror
set key left
set bmargin 2
set style fill solid 0.25 border lt -1
set style data boxplot
set style boxplot nooutliers
set bars 0.2
set boxwidth 0.3
set border 2
set label '128 requests' at graph 0.225, 0.025 rotate center font 'Verdana,10'
set label '256 requests' at graph 0.525, 0.025 rotate center font 'Verdana,10'
set label '512 requests' at graph 0.825, 0.025 rotate center font 'Verdana,10'
input1 = sprintf("throughput-%s-%s-%s-%s.csv", compare, profile, framework, type)
input2 = sprintf("throughput-%s-%s-%s-%s.csv", current, profile, framework, type)

plot \
    input1 using (0.2):2:(0.3):1 title compare ,\
    input2 using (0.6):2:(0.3):1 title current