# Project Guide

Project instructions are recorded in this repository. They do not assume any external personal memory or account connection.

## Project overview
Record the exhibition name, installation site and scene flow here.

## Hardware and external integrations
Record sensors, RFID, serial and API endpoints here. Do not commit secret keys.

- RFID readers: readers are TCP clients and Unity is the server. Settings and card mapping are in `Assets/StreamingAssets/RfidSettings.json`; usage is in `docs/Rfid-Tcp.md`.
- Game button: `NextButton` in the Input Manager; usage is in `docs/Button-Mapping.md`.

## Project-specific rules
Record development and verification rules here.

- All Debug.Log/LogWarning/LogError messages written by the project use English and the `[ClassName] message` format.
- Comments, documents and asset file names are written in English.
