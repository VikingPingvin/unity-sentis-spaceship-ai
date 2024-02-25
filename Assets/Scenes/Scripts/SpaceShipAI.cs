using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class SpaceShipAI : MonoBehaviour
{
    private struct commands
    {
        public const string PowerOn = "PowerOn";
        public const string LightsOn = "LightsOn";
    }

    private struct ShipState
    {
        public bool isPowerOn;
        public bool isLightsOn;
        public bool isEngineOn;
        public bool isAirlockOpen;
        public bool isEjectCargo;

    }

    private float commandProbabilityThreshold = 0.2f;
    ShipState shipState;

    [SerializeField] private GameObject shipLightGameObject;

    private void Start()
    {
        shipState = new ShipState
        {
            isPowerOn = false,
            isLightsOn = false,
            isEngineOn = false,
            isAirlockOpen = false,
            isEjectCargo = false,
        };
    }
    public void TryGiveCommand(DockerModelRunner.CommandPredictionResponse data)
    {
        if (MatchPrediction(commands.PowerOn, data))
        {
            PowerOn();
        }

        if (MatchPrediction(commands.LightsOn, data))
        {
            LightsOn();
        }
    }

    private bool MatchPrediction(string wantCommand, DockerModelRunner.CommandPredictionResponse data)
    {
        if (data.Prediction == wantCommand && data.Probabilities[wantCommand] >= commandProbabilityThreshold)
        {
            return true;
        }
        return false;
    }

    private void PowerOn()
    {
        Debug.Log("-------- > TURNING ON POWER");
        shipState.isPowerOn = true;
    }

    private void LightsOn()
    {
        // shipLight = shipLightGameObject.GetComponent<Light>();
        if (shipState.isPowerOn)
        {
            Debug.Log("-------- > TURNING ON LIGHTS");
            shipLightGameObject.SetActive(true);
        }
    }
}
