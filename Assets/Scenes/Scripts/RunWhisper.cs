using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.IO;
using Newtonsoft.Json;
using System.Text;

/*
 *              Whisper Inference Code
 *              ======================
 *  
 *  Put this script on the Main Camera
 *  
 *  In Assets/StreamingAssets put:
 *  
 *  AudioDecoder_Tiny.sentis
 *  AudioEncoder_Tiny.sentis
 *  LogMelSepctro.sentis
 *  vocab.json
 * 
 *  Drag a 30s 16khz mono uncompressed audioclip into the audioClip field. 
 * 
 *  Install package com.unity.nuget.newtonsoft-json from packagemanger
 *  Install package com.unity.sentis
 * 
 */


public class RunWhisper : MonoBehaviour
{
    IWorker decoderEngine, encoderEngine, spectroEngine;

    const BackendType backend = BackendType.GPUCompute;

    Model decoder;
    Model encoder;
    Model spectro;

    // Link your audioclip here. Format must be 16Hz mono non-compressed.
    public AudioClip audioClip;

    // This is how many tokens you want. It can be adjusted.
    const int maxTokens = 100;

    //Special tokens see added tokens file for details
    const int END_OF_TEXT = 50257;
    const int START_OF_TRANSCRIPT = 50258;
    const int ENGLISH = 50259;
    const int GERMAN = 50261;
    const int FRENCH = 50265;
    const int TRANSCRIBE = 50359; //for speech-to-text in specified language
    const int TRANSLATE = 50358;  //for speech-to-text then translate to English
    const int NO_TIME_STAMPS = 50363;
    const int START_TIME = 50364;


    Ops ops;
    ITensorAllocator allocator;

    int numSamples;
    float[] data;
    string[] tokens;

    int currentToken = 0;
    int[] outputTokens = new int[maxTokens];

    // Used for special character decoding
    int[] whiteSpaceCharacters = new int[256];

    TensorFloat encodedAudio;

    bool transcribe = false;
    string outputString = "";

    // Maximum size of audioClip (30s at 16kHz)
    const int maxSamples = 30 * 16000;


    // My own stuff
    bool voiceClipAvailable = false;
    DockerModelRunner dockerModelRunner;

    public void SetAudioClip(AudioClip clip)
    {
        this.audioClip = clip;
        outputString = "";
        voiceClipAvailable = true;
        ResetTokens();
        LoadAudio();
        EncodeAudio();
        transcribe = true;
    }

    public void clearAudioClip()
    {
        voiceClipAvailable = false;
        this.audioClip = null;
    }
    void Start()
    {
        dockerModelRunner = this.GetComponent<DockerModelRunner>();

        allocator = new TensorCachingAllocator();
        ops = WorkerFactory.CreateOps(backend, allocator);

        SetupWhiteSpaceShifts();

        GetTokens();

        decoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioDecoder_Tiny.sentis");
        encoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioEncoder_Tiny.sentis");
        spectro = ModelLoader.Load(Application.streamingAssetsPath + "/LogMelSepctro.sentis");

        decoderEngine = WorkerFactory.CreateWorker(backend, decoder);
        encoderEngine = WorkerFactory.CreateWorker(backend, encoder);
        spectroEngine = WorkerFactory.CreateWorker(backend, spectro);

        ResetTokens();
        // outputTokens[0] = START_OF_TRANSCRIPT;
        // outputTokens[1] = ENGLISH;// GERMAN;//FRENCH;//
        // outputTokens[2] = TRANSCRIBE; //TRANSLATE;//
        // outputTokens[3] = NO_TIME_STAMPS;// START_TIME;//
        // currentToken = 3;

        // LoadAudio();
        // EncodeAudio();
        transcribe = true;
    }

    void ResetTokens()
    {
        // decoderEngine = WorkerFactory.CreateWorker(backend, decoder);
        // encoderEngine = WorkerFactory.CreateWorker(backend, encoder);
        // spectroEngine = WorkerFactory.CreateWorker(backend, spectro);

        outputTokens = new int[maxTokens];
        outputTokens[0] = START_OF_TRANSCRIPT;
        outputTokens[1] = ENGLISH;// GERMAN;//FRENCH;//
        outputTokens[2] = TRANSCRIBE; //TRANSLATE;//
        outputTokens[3] = NO_TIME_STAMPS;// START_TIME;//
        currentToken = 3;
    }

    void LoadAudio()
    {
        if (audioClip.frequency != 16000)
        {
            Debug.Log($"The audio clip should have frequency 16kHz. It has frequency {audioClip.frequency / 1000f}kHz");
            return;
        }

        numSamples = audioClip.samples;

        if (numSamples > maxSamples)
        {
            Debug.Log($"The AudioClip is too long. It must be less than 30 seconds. This clip is {numSamples / audioClip.frequency} seconds.");
            return;
        }

        data = new float[numSamples];
        audioClip.GetData(data, 0);
        Debug.Log("Audioclip is OK");
    }


    void GetTokens()
    {
        var jsonText = File.ReadAllText(Application.streamingAssetsPath + "/vocab.json");
        var vocab = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonText);
        tokens = new string[vocab.Count];
        foreach (var item in vocab)
        {
            tokens[item.Value] = item.Key;
        }
    }

    void EncodeAudio()
    {
        using var input = new TensorFloat(new TensorShape(1, numSamples), data);

        // Pad out to 30 seconds at 16khz if necessary
        using var input30seconds = ops.Pad(input, new int[] { 0, 0, 0, maxSamples - numSamples });

        spectroEngine.Execute(input30seconds);
        var spectroOutput = spectroEngine.PeekOutput() as TensorFloat;

        encoderEngine.Execute(spectroOutput);
        encodedAudio = encoderEngine.PeekOutput() as TensorFloat;
    }


    // Update is called once per frame
    void Update()
    {
        Transcribe();
    }

    void Transcribe()
    {
        // Debug.Log($"Transcribe: {transcribe}\n LengthCheck: {currentToken < outputTokens.Length - 1}\n Clip Available: {voiceClipAvailable}");
        if (transcribe && currentToken < outputTokens.Length - 1 && voiceClipAvailable)
        {
            using var tokensSoFar = new TensorInt(new TensorShape(1, outputTokens.Length), outputTokens);

            var inputs = new Dictionary<string, Tensor>
            {
                {"encoded_audio",encodedAudio },
                {"tokens" , tokensSoFar }
            };

            decoderEngine.Execute(inputs);
            var tokensOut = decoderEngine.PeekOutput() as TensorFloat;

            using var tokensPredictions = ops.ArgMax(tokensOut, 2, false);
            tokensPredictions.MakeReadable();

            int ID = tokensPredictions[currentToken];

            outputTokens[++currentToken] = ID;

            if (ID == END_OF_TEXT)
            {
                transcribe = false;
            }
            else if (ID >= tokens.Length)
            {
                outputString += $"(time={(ID - START_TIME) * 0.02f})";
            }
            else outputString += GetUnicodeText(tokens[ID]);

            Debug.Log(outputString);
        }
        else if (!transcribe)
        {
            if (this.audioClip != null)
            {
                Debug.Log("Clearing AudioClip");
                clearAudioClip();
                Debug.Log("Calling Docker inference runner");
                dockerModelRunner.StartDockerInference(outputString);
            }
        }
    }

    // Translates encoded special characters to Unicode
    string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }

    string ShiftCharacterDown(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += ((int)letter <= 256) ? letter :
                (char)whiteSpaceCharacters[(int)(letter - 256)];
        }
        return outText;
    }

    void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
        }
    }

    bool IsWhiteSpace(char c)
    {
        return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
    }

    private void OnApplicationQuit()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }

    private void OnDestroy()
    {
        decoderEngine?.Dispose();
        encoderEngine?.Dispose();
        spectroEngine?.Dispose();
        ops?.Dispose();
        allocator?.Dispose();
    }
}
