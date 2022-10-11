
using EmotionFerPlus;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;

const int TEST_SIZE = 100;

Emotion t = new Emotion();
var cancelTokenSource = new CancellationTokenSource();
var token = cancelTokenSource.Token;
using Image<Rgb24> image1 = Image.Load<Rgb24>("face1.png");
using Image<Rgb24> Image2 = Image.Load<Rgb24>("face2.png");


List<Task> tasks = new List<Task>();

// start async test
var stopwatch = new Stopwatch();
stopwatch.Start();

for (int k = 0; k < TEST_SIZE; ++k)
{
    Task new_task = t.ProcessAsync(image1, token);
    tasks.Add(new_task);
}
Task.WaitAll(tasks.ToArray());

stopwatch.Stop();

Console.WriteLine($"Time for ProcessAsync is {stopwatch.ElapsedMilliseconds} ms");



// start sync test
var stopwatch_s = new Stopwatch();
stopwatch_s.Start();

for (int k = 0; k < TEST_SIZE; ++k)
{
    var result = t.ProcessWithoutParallel(image1);
}

stopwatch_s.Stop();

Console.WriteLine($"Time without using parallel tasks is {stopwatch_s.ElapsedMilliseconds} ms");


//Test the work on different iamges
var task1 = t.ProcessAsync(image1, token);
var task2 = t.ProcessAsync(Image2, token);

Console.WriteLine();
Console.WriteLine("Task1 results:");
foreach (KeyValuePair<string, float> entry in task1.Result)
    Console.WriteLine($"{entry.Key}: {entry.Value}");
Console.WriteLine();


Console.WriteLine("Task2 results:");
foreach (KeyValuePair<string, float> entry in task2.Result)
    Console.WriteLine($"{entry.Key}: {entry.Value}");
Console.WriteLine();

var r = await Task.WhenAll(task1, task1);
