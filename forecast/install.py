import sys
import subprocess
import platform

def get_platform_info():
    """Detects the platform and returns a platform string."""
    try:
        # Check for NVIDIA GPU
        subprocess.run(["nvidia-smi"], check=True, capture_output=True)
        return "cuda"
    except (subprocess.CalledProcessError, FileNotFoundError):
        try:
            # Check for AMD GPU
            subprocess.run(["rocminfo"], check=True, capture_output=True)
            return "rocm"
        except (subprocess.CalledProcessError, FileNotFoundError):
            return "cpu"

def install_dependencies():
    """Installs dependencies based on hardware detection."""
    platform_type = get_platform_info()
    print(f"Detected platform: {platform_type.upper()}")
    
    # Base command for all platforms
    base_command = [sys.executable, "-m", "pip", "install"]
    # Specific commands based on platform
    if platform_type == "cuda":
        command = base_command + [".[cuda]", "--extra-index-url", "https://download.pytorch.org/whl/cu126"]
    elif platform_type == "rocm":
        command = base_command + [".[rocm]", "--extra-index-url", "https://download.pytorch.org/whl/rocm6.4"]
    else:  # 'cpu'
        command = base_command + [".[cpu]"]
    
    try:
        print(f"Running command: {' '.join(command)}")
        subprocess.run(command, check=True)
        print("Installation successful.")
    except subprocess.CalledProcessError as e:
        print(f"Installation failed: {e}")
        sys.exit(1)

if __name__ == "__main__":
    install_dependencies()
