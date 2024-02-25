using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.DeviceSimulation;
using UnityEngine;
using UnityEngine.iOS;

public class VoiceInputHandler : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    private bool isVoiceRecording = false;
    private AudioClip recordedClip;

    [SerializeField] RunWhisper whisperRunner;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Starting voice record...");
            StartVoiceRecord();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            StopRecording();
            SaveRecording();
        }
    }

    private void SaveRecording()
    {
        if (recordedClip == null) return;

        whisperRunner.SetAudioClip(recordedClip);
        // Debug.Log($"Saved clip samples: {recordedClip.length}");
        _audioSource.clip = recordedClip;
        _audioSource.Play();
    }

    private void StopRecording()
    {
        if (isVoiceRecording)
        {
            Microphone.End(null);
            isVoiceRecording = false;
        }
    }

    private void StartVoiceRecord()
    {
        whisperRunner.clearAudioClip();
        isVoiceRecording = true;
        string device = Microphone.devices[0];
        // Debug.Log($"Devices:  {Microphone.devices}");
        recordedClip = Microphone.Start(device, false, 10, 16000);
    }
}
