<img width="512" height="512" alt="wolfradio" src="https://github.com/user-attachments/assets/28263df8-22fd-441e-b549-e166867f471b" />


# WolfRadio - FM Stereo Modulator

[English](#english) · [Italiano](#italiano) · [Deutsch](#deutsch)

Copyright © Freewaves. All rights reserved.  
[www.freewaves.it](https://www.freewaves.it) · max@freewaves.it

---

## English

Self-contained Windows 10 x64 application for generating a 192 kHz FM stereo composite (MPX) signal. A single administrator installer requests UAC elevation, installs to Program Files, creates a desktop shortcut, and automatically replaces earlier versions.

## Included features

- stereo test tone, selectable Windows audio input, or audio-file input;
- enumeration and selection of all Windows capture devices, with internal conversion to 192 kHz;
- looping playlists of one or more MP3, WAV, FLAC, AAC/M4A, WMA, or AIFF files, with automatic decoding and resampling;
- HTTP/HTTPS network-stream input through the Windows Media Foundation decoder;
- optional soft compressor and adjustable input gain from 0% to 250%;
- stereo MPX generation: L+R, 19 kHz pilot, and 38 kHz DSB-SC L-R;
- selectable stereo or mono transmission while keeping RDS independently configurable;
- GUI-configurable RDS PI, PS, and RadioText;
- automatic RadioText updates with artist/title tags from local files or ICY metadata from network streams;
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

---

## Italiano

Applicazione autonoma per Windows 10 x64 che genera un segnale composito FM stereo (MPX) a 192 kHz. L’installer unico richiede i privilegi di amministratore, installa il programma in Program Files, crea un collegamento sul desktop e sostituisce automaticamente le versioni precedenti.

### Funzionalità incluse

- tono di prova stereo, ingresso audio Windows selezionabile oppure riproduzione da file;
- rilevamento e selezione di tutti i dispositivi di acquisizione Windows, con conversione interna a 192 kHz;
- playlist in loop di uno o più file MP3, WAV, FLAC, AAC/M4A, WMA o AIFF, con decodifica e ricampionamento automatici;
- ingresso streaming HTTP/HTTPS tramite il decoder Windows Media Foundation;
- compressore soft opzionale e guadagno d’ingresso regolabile in tempo reale dallo 0% al 250%;
- generazione MPX stereo: L+R, pilota a 19 kHz e L-R DSB-SC a 38 kHz;
- trasmissione stereo o mono selezionabile, con RDS configurabile indipendentemente;
- configurazione da interfaccia di PI, PS e RadioText RDS;
- aggiornamento automatico del RadioText con artista e titolo dei file locali o metadati ICY degli stream;
- generazione RDS fisica a 57 kHz con codifica differenziale, Manchester e gruppi 0A/2A;
- uscita su scheda audio PCM a 192 kHz e 16 bit tramite API native Windows;
- registrazione MPX in formato WAV per prove e analisi;
- VU meter stereo, pre-enfasi 50/75 µs e spettro MPX 0–96 kHz in tempo reale;
- uscita HackRF con sintonia 1–6000 MHz, IQ centrato a 2,4 MS/s, deviazione FM ±75 kHz, guadagno TX 0–47 dB e controllo amplificatore RF;
- modalità solo portante per misurare separatamente rumore e spurie RF;
- distribuzione x64 autonoma, senza necessità di installare il runtime .NET.

> Utilizzare esclusivamente con un carico fittizio, in laboratorio o nel rispetto delle autorizzazioni radio applicabili. L’uscita su scheda audio fornisce il segnale MPX in banda base, non un segnale RF irradiato.

### Compilazione

Aprire `FmStereoModulator.sln` con Visual Studio 2022 oppure eseguire:

```powershell
.\build.ps1
```

I file pubblicati vengono salvati in `artifacts\publish`. Lo script `package.ps1` crea anche un archivio ZIP distribuibile e, se WiX Toolset è disponibile, un installer MSI.

### Architettura

`Sorgente audio -> normalizzazione/pre-enfasi -> MPX stereo + RDS -> limitatore -> uscita`

- `Dsp/`: motore in tempo reale indipendente dall’interfaccia.
- `Audio/`: sistemi di ingresso e uscita nativi per Windows.
- `Rds/`: gruppi, CRC/checkword, codifica differenziale e Manchester.
- `MainWindow`: configurazione e monitoraggio.

### HackRF

Il supporto HackRF è operativo e comprende le librerie native necessarie. Il driver WinUSB del dispositivo rimane un requisito hardware inevitabile. Prima di trasmettere, verificare sempre carico fittizio, frequenza, autorizzazione e guadagno.

---

## Deutsch

Eigenständige Windows-10-x64-Anwendung zur Erzeugung eines FM-Stereo-Multiplexsignals (MPX) mit 192 kHz. Das Installationsprogramm fordert Administratorrechte an, installiert die Anwendung unter „Program Files“, erstellt eine Desktopverknüpfung und ersetzt frühere Versionen automatisch.

### Enthaltene Funktionen

- Stereo-Testton, auswählbarer Windows-Audioeingang oder Wiedergabe aus Audiodateien;
- Erkennung und Auswahl aller Windows-Aufnahmegeräte mit interner Umwandlung auf 192 kHz;
- Endloswiedergabe von Playlists mit MP3-, WAV-, FLAC-, AAC/M4A-, WMA- oder AIFF-Dateien einschließlich automatischer Decodierung und Abtastratenwandlung;
- HTTP/HTTPS-Netzwerkstream als Eingang über den Windows-Media-Foundation-Decoder;
- optionaler Soft-Kompressor und während der Übertragung regelbare Eingangsverstärkung von 0 bis 250 %;
- Stereo-MPX-Erzeugung mit L+R, 19-kHz-Pilotton und 38-kHz-DSB-SC für L-R;
- wählbare Stereo- oder Monoübertragung bei unabhängig konfigurierbarem RDS;
- RDS-PI, PS und RadioText über die Benutzeroberfläche einstellbar;
- automatische RadioText-Aktualisierung mit Interpret und Titel aus lokalen Dateien oder ICY-Metadaten von Netzwerkstreams;
- echte 57-kHz-RDS-Erzeugung mit Differenzcodierung, Manchester-Codierung und 0A/2A-Gruppen;
- 192-kHz-/16-Bit-PCM-Ausgabe über native Windows-Audioschnittstellen;
- MPX-Aufzeichnung als WAV-Datei für Tests und Analysen;
- Stereo-VU-Meter, 50/75-µs-Vorverzerrung und MPX-Echtzeitspektrum von 0 bis 96 kHz;
- HackRF-Ausgabe mit 1–6000 MHz Abstimmungsbereich, zentriertem IQ-Signal bei 2,4 MS/s, ±75 kHz FM-Hub, 0–47 dB TX-Verstärkung und RF-Verstärkersteuerung;
- reiner Trägermodus zur getrennten Messung von RF-Rauschen und Störsignalen;
- eigenständige x64-Auslieferung ohne zusätzlich zu installierende .NET-Laufzeit.

> Nur an einer Dummy-Last, im Labor oder im Rahmen der geltenden Funkgenehmigung verwenden. Ein Audiogerät gibt das MPX-Basisbandsignal aus, keine abgestrahlte Hochfrequenz.

### Kompilieren

`FmStereoModulator.sln` mit Visual Studio 2022 öffnen oder folgenden Befehl ausführen:

```powershell
.\build.ps1
```

Die Veröffentlichung wird unter `artifacts\publish` abgelegt. `package.ps1` erstellt außerdem ein verteilbares ZIP-Archiv und, sofern WiX Toolset verfügbar ist, ein MSI-Installationsprogramm.

### Architektur

`Audioquelle -> Normalisierung/Vorverzerrung -> Stereo-MPX + RDS -> Begrenzer -> Ausgabe`

- `Dsp/`: von der Benutzeroberfläche unabhängige Echtzeit-Engine.
- `Audio/`: native Windows-Ein- und Ausgabesysteme.
- `Rds/`: Gruppen, CRC/Prüfwörter, Differenz- und Manchester-Codierung.
- `MainWindow`: Konfiguration und Überwachung.

### HackRF

Das HackRF-Ausgabemodul ist betriebsbereit und enthält die benötigten nativen Bibliotheken. Der WinUSB-Gerätetreiber bleibt eine unvermeidbare Hardwarevoraussetzung. Vor dem Senden stets Dummy-Last, Frequenz, Genehmigung und Verstärkung prüfen.
