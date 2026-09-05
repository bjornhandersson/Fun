# Soundcheck

Compare headphones with your own ears, in numbers.

Reviews describe headphones in adjectives. Measurement sites give curves taken on a dummy
head. Soundcheck plays the same synthesised signals into every pair you own and lets you
mark where you hear the edge. Slide a sine down until it stops being a pitch: bass
extension, in Hz. Alternate 100 Hz with 1 kHz and adjust until they sound equally loud: bass
tilt, in dB. Marks go on a scoreboard, one column per headphone-and-DAC chain. Same signals,
same ears, same volume, so whatever differs between columns is the gear.

Two static pages: `index.html` explains, `compare.html` is the tool. Built phone-first.

## The signals

All synthesised in the browser with the Web Audio API at the DAC's native sample rate.
Nothing downloaded, nothing lossy.

- Exponential sine sweeps (equal time per octave) for bass, mids, treble, full range.
- Steady tones: 30 Hz sub, 50 + 65 Hz beat, 1 kHz and 3 kHz references, 8 to 18 kHz steps.
- Tone generator, any frequency, sine / triangle / square / sawtooth.
- Kick drum: sine with a 160 to 45 Hz pitch envelope plus a noise click.
- Karplus-Strong plucked strings (noise burst in a delay line).
- Hi-hats: high-passed noise bursts.
- Pink noise, Paul Kellet's -3 dB/octave filter.
- Left/right, inverted phase, stereo pan.

Levels in dBFS. Sample rate is reported once the audio context starts on the first tap.

## Running it

Open `index.html`, or serve the folder and open it on a phone over the LAN:

```sh
python3 -m http.server -d soundcheck 8000
```

Deploy by copying the folder to any static host.

## Roadmap

- Speakers and rooms: warble tones for room modes, RT60 impulse, subwoofer crossover check.
- DACs and amps: jitter-sensitive tones, intersample-peak test, noise floor at -60 dBFS.
- Real music excerpts, licensing permitting.
