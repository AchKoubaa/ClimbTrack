# ClimbTrack

[![CI](https://github.com/AchKoubaa/ClimbTrack/actions/workflows/ci.yml/badge.svg)](https://github.com/AchKoubaa/ClimbTrack/actions/workflows/ci.yml)
[![CD](https://github.com/AchKoubaa/ClimbTrack/actions/workflows/cd.yml/badge.svg)](https://github.com/AchKoubaa/ClimbTrack/actions/workflows/cd.yml)
[![Latest Release](https://img.shields.io/github/v/release/AchKoubaa/ClimbTrack)](https://github.com/AchKoubaa/ClimbTrack/releases/latest)

## Descrizione

ClimbTrack è un'applicazione MAUI per il tracciamento delle attività di arrampicata, che utilizza Firebase per l'autenticazione e il database in tempo reale. L'app permette agli scalatori di registrare le loro sessioni, monitorare i progressi e analizzare le performance attraverso grafici intuitivi.

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

## Installazione

### Per utenti
Le release dell'app sono disponibili nella [sezione Releases](https://github.com/AchKoubaa/ClimbTrack/releases) di questo repository.

### Per sviluppatori
Segui le istruzioni nella sezione Configurazione per impostare l'ambiente di sviluppo.

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

## CI/CD

Questo progetto utilizza GitHub Actions per l'integrazione continua (CI) e la distribuzione continua (CD):

- **CI**: Compila automaticamente l'app e verifica che non ci siano errori di build quando viene fatto push su `main`, `develop` o quando viene creata una pull request.
- **CD**: Crea automaticamente una release quando viene creato un tag che inizia con "v" (es. v1.0.0).

## Contributi

I contributi sono benvenuti! Se desideri contribuire a questo progetto:

1. Forka il repository
2. Crea un branch per la tua feature (`git checkout -b feature/amazing-feature`)
3. Commit delle tue modifiche (`git commit -m 'Aggiungi una feature incredibile'`)
4. Push al branch (`git push origin feature/amazing-feature`)
5. Apri una Pull Request

## Licenza

Distribuito sotto la Licenza MIT. Vedi `LICENSE` per maggiori informazioni.