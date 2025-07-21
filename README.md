# Ayllu Backend API

Este repositorio contiene la API del backend del proyecto **Ayllu Academy**, desarrollado en .NET con una arquitectura limpia basada en capas.

## 📦 Estructura del Proyecto

```
Ayllu.Backend/
├── Ayllu.Backend.Api              # Capa de presentación (controladores HTTP)
├── Ayllu.Backend.Application      # Lógica de negocio (casos de uso)
├── Ayllu.Backend.Domain           # Entidades y lógica del dominio
├── Ayllu.Backend.Infrastructure   # Acceso a datos y servicios externos
└── Ayllu.Backend.sln              # Solución de Visual Studio
```

## ⚙️ Requisitos Previos

- [.NET 7.0+](https://dotnet.microsoft.com/)
- [Cardano Node + CLI](https://docs.cardano.org/)
- Ubuntu / WSL (recomendado para ejecución de scripts)
- Conexión a red testnet (Preview o Preprod)
- Node en sincronización
- Billetera Cardano configurada

## 🔐 Configuración de Claves y Direcciones

Para poder interactuar con la blockchain, necesitas generar los archivos:

```bash
# Crear directorio para la wallet
mkdir wallets && cd wallets

# Generar key pair
cardano-cli address key-gen \
  --verification-key-file payment.vkey \
  --signing-key-file payment.skey

# Crear la dirección
cardano-cli address build \
  --payment-verification-key-file payment.vkey \
  --testnet-magic 2 \
  --out-file payment.addr
```

Luego, debes enviar fondos testnet a esa dirección desde el [faucet de Cardano](https://docs.cardano.org/cardano-testnet/tools/faucet/).

## 🔁 Consultar UTxOs disponibles

```bash
cardano-cli query utxo \
  --address $(cat payment.addr) \
  --testnet-magic 2
```

Usarás este UTxO como input para tus transacciones on-chain.

## 🚀 Despliegue Local

```bash
# Restaurar dependencias
dotnet restore

# Compilar la solución
dotnet build

# Ejecutar la API
cd Ayllu.Backend.Api
dotnet run
```

La API estará disponible en: `https://localhost:5001` o `http://localhost:5000`

## 📌 Variables de entorno

Asegúrate de definir las siguientes variables en `appsettings.Development.json` o en tu entorno:

```json
{
  "CardanoSettings": {
    "Network": "preview",
    "NodeSocketPath": "/path/to/db/node.socket",
    "Magic": 2,
    "WalletPath": "./wallets/payment",
    "WorkingDirectory": "./tmp"
  }
}
```

## ✍️ Contribución

Si deseas contribuir:

1. Crea un branch desde `dev`.
2. Realiza tu cambio.
3. Haz pull request hacia `dev`.

---

**Autor:** [David Tacuri] · Proyecto Ayllu · 2025
