# Trinnov Nova StreamDeck Controller

![Trinnov Nova StreamDeck Controller](docs/controller.png)

The **Trinnov Nova StreamDeck Controller** is a tool designed to enhance your interaction with the Trinnov Nova by
enabling streamlined control via a StreamDeck device.

It communicates directly with the Nova unit using WebSockets and binary messages, ensuring seamless synchronization and
real-time command execution. The controller also features a user-friendly visualization system powered by the [VirtualBoard](https://github.com/OpenMacroBoard/OpenMacroBoard.SDK/tree/master/src/OpenMacroBoard.VirtualBoard)
from [OpenMacroBoard.SDK](https://github.com/OpenMacroBoard/OpenMacroBoard.SDK), in case you don't have a
StreamDeck.

---

## Features

- **WebSocket Communication**: Uses WebSocket connections for reliable and efficient communication between the
  controller and the Nova unit.
- **Synchronization**: Automatically keeps the StreamDeck in sync with the Nova unit's current state.
- **Binary Messaging**: Uses binary commands to interact with the Nova for precise and fast control.
- **Customizable IP Address**: The IP address for connecting to the Nova can be provided.
- **Visual Feedback**: Leverages the [VirtualBoard](https://github.com/OpenMacroBoard/OpenMacroBoard.SDK/tree/master/src/OpenMacroBoard.VirtualBoard) from [OpenMacroBoard.SDK](https://github.com/OpenMacroBoard/OpenMacroBoard.SDK) to provide a dynamic and interactive UI without a StreamDeck.

---

## Requirements

- Trinnov Nova with 1.2 Firmware
- .NET 8.0 runtime
- A StreamDeck device (optional)
- Access to the Nova unit via WebSocket

---

## Setup and Installation

1. Clone this repository:
   ```bash
   git clone https://github.com/codebrain/trinnov-stream-deck.git
   cd trinnov-stream-deck

2. `dotnet run`
