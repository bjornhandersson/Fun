# 🎧 Soundcheck — test signals for hi-fi geeks

Two static pages: `index.html` is the front page, `compare.html` is the tool — a way to put
headphones through the same five reference signals and line the results up on a scoreboard,
from any browser, phone first. Every signal is **synthesized in the
browser** with the Web Audio API — no samples are downloaded, nothing is lossy, and
every waveform is built from first principles in plain JavaScript:

- **Sine sweeps** — exponential (equal time per octave) for bass, mids, treble and full range
- **Steady tones** — 30 Hz sub, 50 + 65 Hz beat, 1 kHz / 3 kHz reference, 8–18 kHz steps
- **Tone generator** — any frequency, sine / triangle / square / sawtooth
- **Kick drum** — a sine with a pitch envelope 160 → 45 Hz plus a noise click (transient/punch)
- **Plucked strings** — Karplus–Strong (noise burst in a delay line) arpeggio and chord (mids)
- **Hi-hats** — high-passed noise bursts (sibilance/air)
- **Pink noise** — Paul Kellet's −3 dB/oct filter (balance, A/B, RTA reference)
- **Left/right, phase (in / inverted), stereo pan** — channel, polarity and imaging checks

Level control is shown in dBFS; the output sample rate is reported once the audio engine
starts (first tap — browsers require a user gesture).

## Run it

Open `index.html` in a browser (it links to the tool), or serve the folder with anything static:

```sh
python3 -m http.server -d soundcheck 8000
# → http://localhost:8000  (use your Mac's LAN IP on the phone)
```

Deploy = copy the folder to any static host (GitHub Pages, Netlify, S3…).

## Roadmap

- Speakers & room: warble tones for room modes, RT60 impulse, subwoofer crossover check
- DAC / amp: jitter-sensitive tones, intersample-peak test, dither/noise-floor at −60 dBFS
- Optional real music excerpts (needs licensed material)
