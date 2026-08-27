# WolfRadio - FM Stereo Modulator

Self-contained Windows 10 x64 application for generating a 192 kHz FM stereo composite (MPX) signal. A single administrator installer requests UAC elevation, installs to Program Files, creates a desktop shortcut, and automatically replaces earlier versions.

Copyright © Freewaves. All rights reserved.  
[www.freewaves.it](https://www.freewaves.it) · max@freewaves.it

## Included features

- stereo test tone, selectable Windows audio input, or audio-file input;
- enumeration and selection of all Windows capture devices, with internal conversion to 192 kHz;
- continuous MP3, WAV, AAC/M4A, WMA, and AIFF playback with automatic decoding and resampling;
- stereo MPX generation: L+R, 19 kHz pilot, and 38 kHz DSB-SC L-R;
- GUI-configurable RDS PI, PS, and RadioText;
- physical 57 kHz RDS generation with differential encoding, Manchester coding, and 0A/2A groups;
- 192 kHz, 16-bit PCM audio-device output through native Windows APIs;
- MPX WAV recording for testing and analysis;
- stereo VU meters, 50/75 µs pre-emphasis, and real-time 0–96 kHz MPX spectrum;
- HackRF backend with 1–6000 MHz tuning, centered 2.4 MS/s IQ output, ±75 kHz FM deviation, 0–47 dB TX gain, and RF amplifier control;
- carrier-only mode for separate RF noise and spur measurements;
- self-contained x64 deployment with no external .NET runtime.

> Use only with a dummy load, in a laboratory, or under the applicable radio authorization. An audio device outputs baseband MPX, not radiated RF.

## Build

Open `FmStereoModulator.sln` with Visual Studio 2022 or run:

```powershell
.\build.ps1
```

Published output is written to `artifacts\publish`. `package.ps1` also creates a distributable ZIP and, when WiX Toolset is available, an MSI installer.

## Architecture

`Audio source -> normalization/pre-emphasis -> stereo MPX + RDS -> limiter -> output backend`

- `Dsp/`: real-time engine independent of the GUI.
- `Audio/`: native Windows input and output backends.
- `Rds/`: groups, CRC/checkwords, differential coding, and Manchester coding.
- `MainWindow`: configuration and monitoring.

## HackRF

The HackRF backend is operational and includes its native libraries. The device's WinUSB driver remains an unavoidable hardware requirement. Always verify the dummy load, frequency, authorization, and gain before transmitting.
