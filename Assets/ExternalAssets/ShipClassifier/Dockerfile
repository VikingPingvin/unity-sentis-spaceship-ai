FROM python:3.8-slim
ENV FLASK_APP=server.py

WORKDIR /app
EXPOSE 5000

COPY . /app

RUN pip install --no-cache-dir -r requirements.txt

CMD ["flask", "run", "--host","0.0.0.0"]