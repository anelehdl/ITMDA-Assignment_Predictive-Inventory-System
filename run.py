"""
run_services.py
Starts Data Service and Predict Service.
Equivalent to run_services.sh (without Consul)
"""

import os
import subprocess
from pathlib import Path

# -----------------------------
# Environment Variables
# -----------------------------
os.environ["HOST"] = "localhost"
os.environ["PORT"] = "8420"
os.environ["PNAME"] = "predict-service"

# os.environ["DATA_DIR"] = str(Path.cwd() / "data")
# os.environ["MODEL_DIR"] = str(Path.cwd() / "models")

os.environ["DHOST"] = "localhost"
os.environ["DPORT"] = "8520"
os.environ["DNAME"] = "data-service"


# -----------------------------
# Helper function
# -----------------------------
def start_process(command: str, log_file: str):
    """Start a subprocess and redirect its output to a log file."""
    log = open(log_file, "w")
    process = subprocess.Popen(
        command, stdout=log, stderr=subprocess.STDOUT, shell=True, env=os.environ
    )
    return process


def main():
    # -----------------------------
    # Start Data Service
    # -----------------------------
    print(f"Starting Data Service on {os.environ['DHOST']}:{os.environ['DPORT']}...")
    data_service_cmd = f"uvicorn data_service:app --host {os.environ['DHOST']} --port {os.environ['DPORT']}"
    data_proc = start_process(data_service_cmd, "data_service.log")

    # -----------------------------
    # Start Predict Service
    # -----------------------------
    print(f"Starting Predict Service on {os.environ['HOST']}:{os.environ['PORT']}...")
    predict_service_cmd = f"uvicorn predict_service:app --host {os.environ['HOST']} --port {os.environ['PORT']}"
    predict_proc = start_process(predict_service_cmd, "predict_service.log")

    print("\nAll services started. Logs:")
    print("  data_service.log")
    print("  predict_service.log\n")

    try:
        data_proc.wait()
        predict_proc.wait()
    except KeyboardInterrupt:
        print("\nShutting down services...")
        data_proc.terminate()
        predict_proc.terminate()


if __name__ == "__main__":
    main()
