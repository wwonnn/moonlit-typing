"""Original, loopable paper/brush friction; no oscillator or beep tone."""
from pathlib import Path
import wave
import numpy as np
sr = 32000
count = sr * 2
rng = np.random.default_rng(418)
noise = rng.normal(size=count)
# Circular filtering keeps the loop seam continuous.
coarse = sum(np.roll(noise,k) for k in range(18))/18
fine = sum(np.roll(noise,k) for k in range(3))/3
sound = (coarse*.7+fine*.3) * (.7+.3*np.sin(np.arange(count)*2*np.pi/count)**2)
sound *= .13/max(abs(sound))
root = Path(__file__).resolve().parents[1]
target = root/'Assets/Resources/Audio/BrushFriction.wav'
with wave.open(str(target),'wb') as wav:
    wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(sr)
    wav.writeframes(np.round(sound*32767).astype('<i2').tobytes())
print(target)
