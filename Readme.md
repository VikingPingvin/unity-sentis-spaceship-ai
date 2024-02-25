# AI and ML Integration in Unity Using Sentis

## Overview
This is a very rough proof-of-concept inspired by the Unity Sentis presentation: [LINK](https://youtu.be/VSEk5gc-q_g?t=2077)

> **_NOTE:_**  The focus of this project was learning and showing that it can be done even by complete beginners in this field. Don't expect well defined code or a nicely structured Unity project from this.

Sentis version used: `1.2.0-exp.2`

The core of this project lies in its ability to process real-time microphone input from the user, interpret the speech using ML models, and convert these interpretations into actionable commands within a Unity environment. This is achieved through the integration of two primary components:

1. **OpenAI Whisper Sentis Model**: Utilized for its exceptional speech-to-text capabilities, allowing for accurate transcription of user voice inputs. [LINK](https://huggingface.co/unity/sentis-whisper-tiny/tree/main)
2. **Custom Multi-class Classifier Model**: Since the custom classifier model type of `LinearClassifier` is not (yet, I assume) supported by Sentis, I've created a Docker container running `ONNXRUNTIME`, that also preprocesses data

## How It Works

1. **Voice Input**: While pressing `spacebar`, microphone input is captured as an `AudioClip` and sent to `whisperRunner.cs`
2. **Speech-to-Text Conversion**: After preprocessing the `AudioClip`, the Whisper Sentis model processes it into text.
3. **Command Classification**: The transcribed text is sent to the custom classifier model, which interprets the text and assigns a label or prediction.
4. **Command Execution**: The determined command is communicated back to Unity, where it triggers specific logic tied to GameObjects in the game.


## MultiClass Classification Model
### Training
Training is done in a very rough Google Colab notebook: [LINK](https://colab.research.google.com/drive/1fRoIEa1Jf9hHUxlNri1Dz6Z8bRSc7w_4?usp=sharing)
(this is my first adventure with ML models, so the Colab code includes many unnecesary and bad code)
- Upload a csv training data that contains the following format:   

    | Text               | Label    |
    |--------------------|----------|
    | Turn Power On      | PowerOn  |
    | Batteries On       | PowerOn  |
    | Turn the lights on | LightsOn |
    etc...

- Run the notebook, and save the generated `.onnx` model and the `vocabulary.pkl`
### Running the inference API
The [Included Docker Flask app](./Assets/ExternalAssets/ShipClassifier/) does the following:
- Listen on `/predict` for a `POST` request with a data schema:
    ```json
    {
        "input": "<your-text-here>"
    }
    ```
- Since it expects a `string` data from the request, the code automatically preprocesses the input data into a float tensor using the `sklearn` library and passes it along into the inference model
- The response contains the predicted label and the full probability vector