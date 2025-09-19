# Setup Instructions

## Before installing
If you want to use the GPU instead of CPU AND have a compatible GPU install the needed libraries if not already installed. (but it should be available with your drivers)
- Nvidia
[CUDA](https://developer.nvidia.com/cuda-downloads)

- AMD 
[ROCm](https://rocm.docs.amd.com/en/latest/)


## 1. Create a virtual environment
```bash
python -m venv .venv
```
## 2. Activate virtual enviroment
- Linux/macOS
```bash
source .venv/bin/activate
```
- Windows
```powershell
.venv\Scripts\Activate.ps1
```
## 3. Install dependencies
```python
python install.py
```
