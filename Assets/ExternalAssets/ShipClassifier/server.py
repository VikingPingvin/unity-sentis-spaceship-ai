from flask import Flask, request, jsonify
import logging
import json
import numpy as np
import onnxruntime as ort
from sklearn.feature_extraction.text import CountVectorizer
import pickle

app = Flask(__name__)

logging.basicConfig(level=logging.DEBUG,
                    format='%(asctime)s %(levelname)s : %(message)s')

cv = CountVectorizer()

# Load your trained ONNX model
ort_session = ort.InferenceSession("spaceship_ai_alternate_dense.onnx")

# Load your fitted CountVectorizer
with open('vocabulary.pkl', 'rb') as f:
    vocab = pickle.load(f)
    cv.vocabulary_ = vocab

def preprocess_input(raw_input):
    """Convert raw string input to a format suitable for model inference."""
    transformed_input = cv.transform([raw_input])
    return transformed_input.toarray().astype(np.float32)

def predict(transformed_input):
    """Run model inference on the preprocessed input."""
    ort_inputs = {ort_session.get_inputs()[0].name: transformed_input}
    ort_outs = ort_session.run(None, ort_inputs)
    return ort_outs

@app.before_request
def log_request_info():
    app.logger.info(f"Request: {request.method} {request.path}")


@app.route('/predict', methods=['POST'])
def handle_predict():
    data = request.json
    raw_input = data['input']
    transformed_input = preprocess_input(raw_input)
    prediction = predict(transformed_input)
    app.logger.info('Predicted: "%s" => %s', raw_input, prediction[0])
    app.logger.info('Probabilities: %s', prediction[1][0])
    return jsonify({'prediction': prediction[0][0],
                    'probabilities': prediction[1][0]})

if __name__ == '__main__':
    app.logger.setLevel('DEBUG')
    app.logger.info("Starting server...")
    app.run(host='127.0.0.1',port=5000,debug=True)
