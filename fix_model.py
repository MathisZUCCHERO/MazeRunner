import torch
import onnx
from onnx import version_converter, helper
import glob
import os

# 1. Find the latest file
latest_file = "results/RunSmart_v2/MazeRunner/MazeRunner-500042.onnx"
output_file = "Assets/MazeRunner_Fixed.onnx"

print(f"Loading: {latest_file} (Opset 18)")

# 2. Simplest Fix for ML-Agents Pytorch export:
# We just need to load it and re-export/convert?
# Actually, since we don't have the original PyTorch model object easily loaded here without the whole ML-Agents graph,
# we will try to use onnx.convert_version.

try:
    model = onnx.load(latest_file)
    
    # Check if we can convert it
    # Note: version_converter from Opset 18 to 9 is hard directly.
    # But usually mlagents just uses simple layers.
    
    # Strategy B: Creating a copy and forcing the opset (can be risky but works for simple graphs)
    # Strategy A: Use onnx.version_converter
    
    print("Attempting conversion to Opset 12 (stable for Barracuda)...")
    try:
        converted_model = version_converter.convert_version(model, 12)
        onnx.save(converted_model, output_file)
        print(f"Success! Saved to {output_file}")
    except Exception as e:
        print(f"Conversion failed: {e}")
        print("Falling back to direct copy but with patched IR version...")
        
        # Fallback: Just save it to Assets and hope Unity 2.3.0 + Barracuda 3.0.0 handles it better if we strip metadata?
        # Actually, let's just copy it to Assets so user sees it, but the real fix might still be the Package.
        
        # Let's try to strip producer version?
        model.producer_name = "Unity"
        model.producer_version = "2.3.0"
        # We can't easily change opset without reconverting logic.
        
        onnx.save(model, output_file)
        print(f"Saved copy to {output_file}")

except Exception as e:
    print(f"Critical Error: {e}")
