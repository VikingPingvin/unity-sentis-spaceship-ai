using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class DockerModelRunner : MonoBehaviour
{

    [SerializeField] private SpaceShipAI spaceshipAI;

    public struct CommandPredictionResponse
    {
        [JsonProperty("prediction")]
        public string Prediction { get; set; }

        [JsonProperty("probabilities")]
        public Dictionary<string, double> Probabilities { get; set; }
    }


    const string inferenceRunnerURL = "http://localhost:5000/predict";


    private void Start()
    {
        // StartDockerInference("Turn on power, please");
        // StartDockerInference("Also turn on the lights.");
    }
    private void ModelResponseHandler(string data)
    {
        Debug.Log("Recieved data to process: " + data);
        CommandPredictionResponse commandPredictionResp = JsonConvert.DeserializeObject<CommandPredictionResponse>(data);
        spaceshipAI.TryGiveCommand(commandPredictionResp);
    }

    public void StartDockerInference(string input)
    {
        string jsonInput = ConvertTextToModelInput(input);
        StartCoroutine(PostRequest(inferenceRunnerURL, jsonInput));
    }

    private string ConvertTextToModelInput(string input)
    {
        var data = new { input = input };
        string jsonString = JsonConvert.SerializeObject(data);
        return jsonString;
    }

    IEnumerator PostRequest(string url, string jsonData)
    {
        UnityWebRequest request = UnityWebRequest.Post(url, jsonData, "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ModelResponseHandler(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }
}