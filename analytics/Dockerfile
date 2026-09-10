FROM python:3.13-alpine
WORKDIR /app
COPY server.py .
RUN pip install --no-cache-dir "psycopg[binary]>=3.2,<4" && mkdir -p /data
EXPOSE 8090
CMD ["python", "server.py"]
