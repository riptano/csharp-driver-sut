$current = 'master'
$compare = 'metrics_master_ft'

pushd

cd C:\Users\JoaoReis\Desktop\lastbenchmarks\net461

$platform = 'windows'
$framework = 'net461'

gnuplot -e "label='throughput'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='read'" -e "framework='$framework'" -e "outputfile='read.png'" -e "platform='$platform'" -e "label='read throughput'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

gnuplot -e "label='cpu %'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='readcpu'" -e "framework='$framework'" -e "outputfile='readcpu.png'" -e "platform='$platform'" -e "label='read cpu %'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

gnuplot -e "label='cpu %'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='writecpu'" -e "framework='$framework'" -e "outputfile='writecpu.png'" -e "platform='$platform'" -e "label='write cpu %'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

gnuplot -e "label='throughput'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='write'" -e "framework='$framework'" -e "outputfile='write.png'" -e "platform='$platform'" -e "label='write throughput'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

cd C:\Users\JoaoReis\Desktop\lastbenchmarks\netcoreapp2.1

$platform = 'windows'
$framework = 'netcoreapp2.1'

gnuplot -e "label='throughput'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='read'" -e "framework='$framework'" -e "outputfile='read.png'" -e "platform='$platform'" -e "label='read throughput'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

gnuplot -e "label='cpu %'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='readcpu'" -e "framework='$framework'" -e "outputfile='readcpu.png'" -e "platform='$platform'" -e "label='read cpu %'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

gnuplot -e "label='cpu %'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='writecpu'" -e "framework='$framework'" -e "outputfile='writecpu.png'" -e "platform='$platform'" -e "label='write cpu %'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

gnuplot -e "label='throughput'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='write'" -e "framework='$framework'" -e "outputfile='write.png'" -e "platform='$platform'" -e "label='write throughput'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

cd C:\Users\JoaoReis\Desktop\csharp

$platform = 'bionic'
$framework = 'netcoreapp2.1'

gnuplot -e "label='throughput'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='read'" -e "framework='$framework'" -e "outputfile='read.png'" -e "platform='$platform'" -e "label='read throughput'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

gnuplot -e "label='cpu %'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='readcpu'" -e "framework='$framework'" -e "outputfile='readcpu.png'" -e "platform='$platform'" -e "label='read cpu %'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

gnuplot -e "label='cpu %'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='writecpu'" -e "framework='$framework'" -e "outputfile='writecpu.png'" -e "platform='$platform'" -e "label='write cpu %'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

gnuplot -e "label='throughput'" -e "compare='$compare'" -e "current='$current'" -e "profile='standard'" -e "type='write'" -e "framework='$framework'" -e "outputfile='write.png'" -e "platform='$platform'" -e "label='write throughput'" C:\Github\Riptano\csharp-driver-sut\compare.gnuplot

popd