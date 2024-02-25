using UnityEngine;
using Unity.Sentis;

public class CreateRuntimeModel : MonoBehaviour
{
    public ModelAsset modelAsset;
    Model runtimeModel;

    Ops ops;
    ITensorAllocator allocator;

    IWorker classifierEngine;
    Model classifier;

    const BackendType backend = BackendType.GPUCompute;


    void Start()
    {
        allocator = new TensorCachingAllocator();
        ops = WorkerFactory.CreateOps(backend, allocator);
        classifier = ModelLoader.Load(Application.streamingAssetsPath + "/spaceship_ai_alternate.onnx");
        classifierEngine = WorkerFactory.CreateWorker(backend, classifier);
        // runtimeModel = ModelLoader.Load(modelAsset);
    }
}