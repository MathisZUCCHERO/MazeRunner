import onnx
import os
import glob

# Find the latest ONNX file
search_path = "results/RunHard/MazeRunner/*.onnx"
files = glob.glob(search_path)
if not files:
    print("No ONNX file found")
    exit(1)

latest_file = max(files, key=os.path.getctime)
print(f"Inspecting file: {latest_file}")

try:
    model = onnx.load(latest_file)
    print(f"IR Version: {model.ir_version}")
    print(f"Producer Name: {model.producer_name}")
    print(f"Producer Version: {model.producer_version}")
    for imp in model.opset_import:
        print(f"Opset Import: {imp.domain} - {imp.version}")
except Exception as e:
    print(f"Error loading model: {e}")
