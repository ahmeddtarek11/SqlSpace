"""Quick diagnostic to test Gemini API key and available models."""
import os
os.environ["GEMINI_API_KEY"] = "AIzaSyD3-QBm3AmSqAEKqC6hJQ1OoqiR0Q57zzQ"

from google import genai

client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])

# List available models
print("=== Available Gemini models ===")
for m in client.models.list():
    if "flash" in m.name.lower() or "gemini" in m.name.lower():
        print(f"  {m.name}")

# Try a simple generation
print("\n=== Testing generation ===")
try:
    response = client.models.generate_content(
        model="gemini-2.0-flash",
        contents="Say hello",
    )
    print(f"gemini-2.0-flash: SUCCESS -> {response.text[:50]}")
except Exception as e:
    print(f"gemini-2.0-flash: FAILED -> {type(e).__name__}: {e}")

try:
    response = client.models.generate_content(
        model="models/gemini-2.5-flash",
        contents="Say hello",
    )
    print(f"models/gemini-2.5-flash: SUCCESS -> {response.text[:50]}")
except Exception as e:
    print(f"models/gemini-2.5-flash: FAILED -> {type(e).__name__}: {e}")
