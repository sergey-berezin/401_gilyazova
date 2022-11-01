using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EmotionFerPlus;

public class Emotion
{
    private InferenceSession session;
    private static readonly string MODEL_RESOUCE = "lib.emotion-ferplus-7.onnx";
    private static object obj = new Object(); //locks session
    public Emotion()
    {
        using var modelStream = typeof(Emotion).Assembly.GetManifestResourceStream(MODEL_RESOUCE) ?? 
            throw new Exception("Cannot find resource");
        using var memoryStream = new MemoryStream();
        modelStream.CopyTo(memoryStream);
        var sessionOptions = new SessionOptions();
        sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
        this.session = new InferenceSession(memoryStream.ToArray(), sessionOptions);
    }

    public async Task<Dictionary<string, float>> ProcessAsync(Image<Rgb24> image, CancellationToken token){
        return await Task<Dictionary<string, float>>.Factory.StartNew(() => {
            image.Mutate(ctx => {
                ctx.Resize(new ResizeOptions 
                            {
                                Size = new Size(64, 64),
                                Mode = ResizeMode.Crop
                            });
                });
            
            var inputs = ImageTransform(image);

            token.ThrowIfCancellationRequested();

            float [] results;
            lock(obj){
                token.ThrowIfCancellationRequested();
                using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results_model = session.Run(inputs);
                results = results_model.First(v => v.Name == "Plus692_Output_0").AsEnumerable<float>().ToArray();
            }

            token.ThrowIfCancellationRequested();

            var emotions = Softmax(results);
            string[] keys = { "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt" };
            var tupleList = new (String Name, float Value)[emotions.Length];
            Dictionary<string, float> output = new Dictionary<string, float>();
            for (int i = 0; i < emotions.Length; i++)
            {
                output[keys[i]] = emotions[i];
            }
            var ordered_out = output.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            return ordered_out;
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public Dictionary<string, float> ProcessWithoutParallel(Image<Rgb24> image){
        image.Mutate(ctx =>
            {
                ctx.Resize(new Size(64, 64));
            });

        var inputs = ImageTransform(image);
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

        var emotions = Softmax(results.First(v => v.Name == "Plus692_Output_0").AsEnumerable<float>().ToArray());

        string[] keys = { "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt" };

        var tupleList = new (String Name, float Value)[emotions.Length];

        Dictionary<string, float> result = new Dictionary<string, float>();

        for (int i = 0; i < emotions.Length; i++)
        {
            result[keys[i]] = emotions[i];
        }
        var ordered = result.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        return ordered;
    }
        
    private List<NamedOnnxValue> ImageTransform(Image<Rgb24> image){
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("Input3", GrayscaleImageToTensor(image)) };
        return inputs;
    }

    public DenseTensor<float> GrayscaleImageToTensor(Image<Rgb24> img)
    {
        var w = img.Width;
        var h = img.Height;
        var t = new DenseTensor<float>(new[] { 1, 1, h, w });

        img.ProcessPixelRows(pa =>
        {
            for (int y = 0; y < h; y++)
            {
                Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    t[0, 0, y, x] = pixelSpan[x].R;
                }
            }
        });

        return t;
    }

    private float[] Softmax(float[] z)
    {
        var exps = z.Select(x => Math.Exp(x)).ToArray();
        var sum = exps.Sum();
        return exps.Select(x => (float)(x / sum)).ToArray();
    }

}

