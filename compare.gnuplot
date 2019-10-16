set term png size 1920,1024
set output outputfile
set title sprintf("%s vs. number of concurrent requests | %s %s", label, platform, framework) noenhanced
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
set label '128' at 0.5 rotate center font 'Verdana,10'
set label '256' at 1.5 rotate center font 'Verdana,10'
set label '512' at 2.5 rotate center font 'Verdana,10'
set xrange [0:]
input1 = sprintf("throughput-%s-%s-%s-%s-old.csv", current, profile, framework, type)
input2 = sprintf("throughput-%s-%s-%s-%s-false.csv", compare, profile, framework, type)
input3 = sprintf("throughput-%s-%s-%s-%s-true.csv", compare, profile, framework, type)
input4 = sprintf("throughput-%s-%s-%s-%s-timers.csv", compare, profile, framework, type)

plot \
    input1 using (0.3):2:(0.1):1 title sprintf("%s-old", current) noenhanced ,\
    input2 using (0.5):2:(0.1):1 title sprintf("%s-false", compare) noenhanced ,\
    input3 using (0.7):2:(0.1):1 title sprintf("%s-true", compare) noenhanced ,\
    input4 using (0.9):2:(0.1):1 title sprintf("%s-timers", compare) noenhanced