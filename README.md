# WolfRadio - FM Stereo Modulator

Applicazione Windows 10 x64 self-contained per generare un segnale composito FM stereo (MPX) a 192 kHz. Sono disponibili un installer standard per utente e un installer Admin per tutti gli utenti, con richiesta UAC e installazione in Program Files.

Copyright © Freewaves. All rights reserved.  
[www.freewaves.it](https://www.freewaves.it) · max@freewaves.it

## MVP incluso

- ingresso test stereo oppure ingresso audio Windows (`waveIn`);
- elenco e selezione di tutte le periferiche di acquisizione Windows, con acquisizione compatibile a 48 kHz e conversione interna a 192 kHz;
- codifica stereo: L+R, pilot 19 kHz, L-R DSB-SC a 38 kHz;
- RDS configurabile dalla GUI: PI, PS e RadioText;
- generazione RDS fisica a 57 kHz con codifica differenziale, Manchester e gruppi 0A/2A;
- uscita sulla scheda audio tramite API native `waveOut` in PCM 16 bit / 192 kHz;
- registrazione WAV dell'MPX per test e analisi;
- VU meter stereo, scelta pre-enfasi 50/75 µs e spettro MPX 0–96 kHz in tempo reale;
- backend HackRF: frequenza 1–6000 MHz, sample rate IQ 2 MHz, deviazione FM ±75 kHz, guadagno TX 0–47 dB e amplificatore RF con comando hardware invertito;
- pubblicazione self-contained x64, senza runtime .NET esterno.

> Usare soltanto su carico fittizio, in laboratorio o secondo le autorizzazioni radio applicabili. Una scheda audio produce MPX in banda base, non un segnale RF FM irradiabile.

## Avvio e build

Aprire `FmStereoModulator.sln` con Visual Studio 2022 oppure eseguire:

```powershell
.\build.ps1
```

L'output pubblicato si trova in `artifacts\publish`. `package.ps1` crea inoltre uno ZIP distribuibile e, se WiX Toolset è disponibile, un MSI.

## Architettura

`Audio source -> normalizzazione/pre-enfasi -> MPX stereo + RDS -> limiter -> output backend`

- `Dsp/`: motore real-time indipendente dalla GUI.
- `Audio/`: ingressi e uscite Windows native.
- `Rds/`: gruppi, CRC/checkword, codifica differenziale e Manchester.
- `MainWindow`: configurazione e controllo.

## Roadmap

1. Backend ASIO: interfaccia opzionale caricata dinamicamente; ASIO richiede comunque il driver del produttore.
2. Decoder di verifica interno, resampling degli stream compressi, limiter oversampled e salvataggio profili.

## HackRF

Il backend HackRF è operativo e le librerie native sono incluse. Il driver WinUSB del dispositivo rimane un requisito hardware inevitabile. Prima di trasmettere verificare sempre carico fittizio, frequenza, autorizzazioni e guadagno. La casella amplificatore applica intenzionalmente il comando hardware invertito richiesto dal progetto.
