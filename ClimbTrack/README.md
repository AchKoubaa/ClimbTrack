# ClimbTrack

[![CI](https://github.com/tuousername/ClimbTrack/actions/workflows/ci.yml/badge.svg)](https://github.com/tuousername/ClimbTrack/actions/workflows/ci.yml)
[![CD](https://github.com/tuousername/ClimbTrack/actions/workflows/cd.yml/badge.svg)](https://github.com/tuousername/ClimbTrack/actions/workflows/cd.yml)

## Descrizione

ClimbTrack è un'applicazione MAUI per il tracciamento delle attività di arrampicata, che utilizza Firebase per l'autenticazione e il database in tempo reale.

## Caratteristiche

- Autenticazione utente con Firebase
- Tracciamento delle sessioni di arrampicata
- Statistiche e grafici delle performance
- Supporto per Android e Windows

## Tecnologie utilizzate

- .NET MAUI
- Firebase Authentication
- Firebase Realtime Database
- Firebase Storage
- SkiaSharp per i grafici

## Requisiti di sviluppo

- .NET 9.0
- MAUI workload
- Account Firebase configurato

## Configurazione

1. Clona il repository
2. Configura Firebase seguendo le istruzioni nella documentazione
3. Posiziona il file `google-services.json` nella cartella `Platforms/Android`
4. Esegui `dotnet restore` per ripristinare le dipendenze
5. Esegui `dotnet build` per compilare il progetto