using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using FleetManagement.Core;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;

if(args.Length>2){Console.Error.WriteLine("Usage: FleetManagement.ModelTrainer [registry-directory] [seed]");return 2;}
var root=Path.GetFullPath(args.Length>0?args[0]:"model-registry");
var seed=42;
if(args.Length==2&&!int.TryParse(args[1],NumberStyles.Integer,CultureInfo.InvariantCulture,out seed)){Console.Error.WriteLine("Seed must be an integer.");return 2;}
var stamp=DateTime.UtcNow;
var runId=$"{stamp:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}";
var pending=Path.Combine(root,".pending-"+runId);
var final=Path.Combine(root,runId);
Directory.CreateDirectory(root);Directory.CreateDirectory(pending);
try{
    var ml=new MLContext(seed:seed);
    var train=MaintenanceLearning.Generate(seed,0,40,60);
    var test=MaintenanceLearning.Generate(unchecked(seed^0x5A17),1000,12,60);
    if(train.Select(r=>r.VehicleId).Intersect(test.Select(r=>r.VehicleId)).Any())throw new InvalidOperationException("Vehicle leakage.");
    var trainView=ml.Data.LoadFromEnumerable(train.Select((r,i)=>MaintenanceRow.From(r,i)));
    var testView=ml.Data.LoadFromEnumerable(test.Select((r,i)=>MaintenanceRow.From(r,i)));
    var binary=ml.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options{
        LabelColumnName="Label",FeatureColumnName="Features",NumberOfLeaves=16,NumberOfTrees=100,
        MinimumExampleCountPerLeaf=20,LearningRate=.1,NumberOfThreads=1
    }).Fit(trainView);
    var binaryPath=Path.Combine(pending,"maintenance-fasttree.zip");
    ml.Model.Save(binary,trainView.Schema,binaryPath);
    var binaryLoaded=ml.Model.Load(binaryPath,out _);
    var original=Probabilities(binary);var loaded=Probabilities(binaryLoaded);
    CheckReload(original,loaded);
    var maintenance=MaintenanceLearning.Train(seed);
    var fastMetrics=MaintenanceLearning.Evaluate(test,r=>loaded[(r.VehicleId-1000)*60+r.Day]);
    if(fastMetrics.TP+fastMetrics.FP+fastMetrics.TN+fastMetrics.FN!=test.Count)throw new InvalidOperationException("Wrong binary evaluation count.");
    Console.WriteLine($"Maintenance held-out: logistic F1={maintenance.Test.F1:F3}, FastTree F1={fastMetrics.F1:F3}; logistic Brier={maintenance.Test.Brier:F3}, FastTree Brier={fastMetrics.Brier:F3}");
    Dictionary<int,double> Probabilities(ITransformer model)=>ml.Data.CreateEnumerable<BinaryOutput>(model.Transform(testView),reuseRowObject:false).ToDictionary(r=>r.RowKey,r=>(double)r.Probability);

    var etaTrain=EtaLearning.Generate(seed,0,1600);
    var etaValidation=EtaLearning.Generate(unchecked(seed^0xB71),2000,400);
    var etaTest=EtaLearning.Generate(unchecked(seed^0x99A),3000,400);
    var etaTrainView=ml.Data.LoadFromEnumerable(etaTrain.Select(EtaRow.From));
    var etaTestView=ml.Data.LoadFromEnumerable(etaTest.Select(EtaRow.From));
    var etaValidationView=ml.Data.LoadFromEnumerable(etaValidation.Select(EtaRow.From));
    var regression=ml.Regression.Trainers.FastTree(new FastTreeRegressionTrainer.Options{
        LabelColumnName="Label",FeatureColumnName="Features",NumberOfLeaves=24,NumberOfTrees=150,
        MinimumExampleCountPerLeaf=15,LearningRate=.1,NumberOfThreads=1
    }).Fit(etaTrainView);
    var etaPath=Path.Combine(pending,"eta-fasttree.zip");
    ml.Model.Save(regression,etaTrainView.Schema,etaPath);
    var etaLoaded=ml.Model.Load(etaPath,out _);
    var etaOriginal=Durations(regression,etaTestView);var etaReloaded=Durations(etaLoaded,etaTestView);
    CheckReload(etaOriginal,etaReloaded);
    var etaFast=EtaLearning.Evaluate(etaTest,r=>etaReloaded[r.Id]);
    var etaBaseline=EtaLearning.Train(seed);
    var validationScores=Durations(etaLoaded,etaValidationView);
    var residuals=etaValidation.Select(r=>Math.Abs(r.ActualMinutes-validationScores[r.Id])).Order().ToArray();
    var residual90=residuals[(int)Math.Ceiling(.9*residuals.Length)-1];
    Console.WriteLine($"ETA held-out: linear MAE={etaBaseline.Test.Mae:F2} min, FastTree MAE={etaFast.Mae:F2} min");
    Dictionary<int,double> Durations(ITransformer model,IDataView input)=>ml.Data.CreateEnumerable<RegressionOutput>(model.Transform(input),reuseRowObject:false).ToDictionary(r=>r.RowKey,r=>Math.Max(1,(double)r.Score));

    var manifest=new{
        schemaVersion=1,runId,createdUtc=stamp,seed,synthetic=true,mlNetVersion="5.0.0",
        sourceRevision=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local-unrecorded",
        maintenance=new{
            trainRows=train.Count,testRows=test.Count,trainVehicles=40,testVehicles=12,threshold=.5,
            features=LogisticMaintenanceModel.FeatureNames,trainSha256=Fingerprint(train),testSha256=Fingerprint(test),
            baseline=maintenance.Baseline,logistic=maintenance.Test,fastTree=fastMetrics,
            settings=new{leaves=16,trees=100,minExamplesPerLeaf=20,learningRate=.1,threads=1},
            artifact=new{file="maintenance-fasttree.zip",sha256=FileHash(binaryPath)},reloadVerified=true
        },
        eta=new{
            trainRows=etaTrain.Count,validationRows=etaValidation.Count,testRows=etaTest.Count,
            features=new[]{"Free-flow minutes","Free-flow minutes × traffic","Stops","Free-flow minutes × load"},
            trainSha256=Fingerprint(etaTrain),validationSha256=Fingerprint(etaValidation),testSha256=Fingerprint(etaTest),
            baseline=etaBaseline.Baseline,linear=etaBaseline.Test,fastTree=etaFast,
            linearValidationResidual90=etaBaseline.ValidationResidual90,fastTreeValidationResidual90=residual90,
            settings=new{leaves=24,trees=150,minExamplesPerLeaf=15,learningRate=.1,threads=1},
            artifact=new{file="eta-fasttree.zip",sha256=FileHash(etaPath)},reloadVerified=true
        },
        limitations="Synthetic labels only. No real-world accuracy or safety claim. Validation residual percentiles are not guaranteed intervals. Models are not automatically promoted or loaded by the browser."
    };
    File.WriteAllText(Path.Combine(pending,"manifest.json"),JsonSerializer.Serialize(manifest,new JsonSerializerOptions{WriteIndented=true}));
    // Only completed, reload-verified experiments become registry entries.
    Directory.Move(pending,final);
    Console.WriteLine($"Registry entry: {final}");
    return 0;
}catch(Exception ex){
    Console.Error.WriteLine(ex);
    if(Directory.Exists(pending))Directory.Delete(pending,true);
    return 1;
}
static string Fingerprint<T>(T value)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();
static string FileHash(string path){using var stream=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();}
static void CheckReload(Dictionary<int,double> original,Dictionary<int,double> loaded){
    if(original.Count==0||original.Count!=loaded.Count||original.Any(p=>!double.IsFinite(p.Value)||!loaded.TryGetValue(p.Key,out var value)||!double.IsFinite(value)||Math.Abs(p.Value-value)>1e-6))
        throw new InvalidOperationException("Model reload predictions differ or are invalid.");
}
public sealed class MaintenanceRow{
    public int RowKey{get;set;}
    public bool Label{get;set;}
    [VectorType(5)] public float[] Features{get;set;}=[];
    public static MaintenanceRow From(TelemetryObservation r,int i)=>new(){RowKey=i,Label=r.FailureWithin7Days,Features=r.Features.Vector().Select(x=>(float)x).ToArray()};
}
public sealed class EtaRow{
    public int RowKey{get;set;}
    public float Label{get;set;}
    [VectorType(4)] public float[] Features{get;set;}=[];
    public static EtaRow From(EtaObservation r)=>new(){RowKey=r.Id,Label=(float)r.ActualMinutes,Features=r.Features.Vector().Select(x=>(float)x).ToArray()};
}
public sealed class BinaryOutput{
    public int RowKey{get;set;}
    public float Probability{get;set;}
}
public sealed class RegressionOutput{
    public int RowKey{get;set;}
    public float Score{get;set;}
}
