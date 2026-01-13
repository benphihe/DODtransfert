# DOD Transfert

Application Windows WPF pour le transfert sécurisé de fichiers (photos et PDF) sur réseau local avec système de comptes UUID.

## Fonctionnalités

- **Transfert sécurisé de fichiers** : Transfert de photos et PDF avec chiffrement AES-256
- **Système de comptes UUID** : Chaque utilisateur possède un identifiant unique (UUID)
- **Architecture hybride** : Chaque instance peut fonctionner en mode serveur ou client
- **Transfert normal** : Envoi de fichiers à un destinataire sélectionné
- **Transfert "Ajout de produits"** : Mode spécial avec validation stricte :
  - Au moins une photo requise
  - Au moins un PDF requis
  - Nom de la marque requis
  - Nom du produit requis
  - Le bouton d'envoi est désactivé tant que tous les champs ne sont pas remplis

## Prérequis

- .NET 8.0 SDK
- Windows 10/11
- Visual Studio 2022 ou Visual Studio Code (recommandé)

## Installation

1. Clonez le dépôt :
```bash
git clone https://github.com/benphihe/DODtransfert.git
cd DODtransfert
```

2. Restaurez les packages NuGet :
```bash
dotnet restore
```

3. Compilez le projet :
```bash
dotnet build
```

## Utilisation

### Démarrage de l'application

1. Lancez l'application :
```bash
dotnet run --project DODtransfert.Client
```

2. **Première connexion** :
   - Entrez votre nom d'utilisateur
   - Un UUID sera automatiquement généré pour votre compte
   - Cliquez sur "Se connecter"

3. **Démarrer le serveur** :
   - Cliquez sur "Démarrer serveur" dans la barre de statut
   - Le serveur écoute sur le port 8888 par défaut

4. **Se connecter à un serveur** :
   - Cliquez sur "Se connecter" dans la barre de statut
   - L'application se connectera au serveur local (127.0.0.1:8888)

### Transfert normal

1. Naviguez vers "Transfert" dans le menu
2. Sélectionnez un destinataire dans la liste
3. Cliquez sur "Sélectionner des fichiers" et choisissez vos photos/PDF
4. Cliquez sur "Envoyer"

### Transfert "Ajout de produits"

1. Naviguez vers "Ajout de produits" dans le menu
2. Sélectionnez un destinataire
3. Remplissez les champs obligatoires :
   - **Nom de la marque** : Nom de la marque du produit
   - **Nom du produit** : Nom du produit
4. Ajoutez au moins une photo
5. Ajoutez au moins un PDF
6. Le bouton "Envoyer le produit" sera activé uniquement lorsque tous les champs sont remplis
7. Cliquez sur "Envoyer le produit"

## Architecture

### Structure du projet

```
DODtransfert/
├── DODtransfert.Client/      # Application WPF principale
│   ├── Models/               # Modèles de données
│   ├── Services/             # Services (réseau, chiffrement, utilisateurs)
│   ├── ViewModels/           # ViewModels MVVM
│   └── Views/                # Vues XAML
├── DODtransfert.Server/       # Service serveur TCP/IP
└── DODtransfert.Shared/       # Code partagé (protocole, constantes)
```

### Technologies utilisées

- **.NET 8.0** : Framework principal
- **WPF** : Interface utilisateur
- **CommunityToolkit.Mvvm** : Pattern MVVM
- **Newtonsoft.Json** : Sérialisation JSON
- **System.Net.Sockets** : Communication TCP/IP
- **System.Security.Cryptography** : Chiffrement AES-256

### Sécurité

- **Chiffrement AES-256** : Tous les fichiers sont chiffrés avant l'envoi
- **Authentification par UUID** : Chaque utilisateur est identifié par un UUID unique
- **Clé partagée** : Une clé de chiffrement est générée pour chaque session

## Développement

### Compilation

```bash
dotnet build DODtransfert.sln
```

### Exécution

```bash
dotnet run --project DODtransfert.Client
```

### Tests

Pour tester l'application avec plusieurs utilisateurs :
1. Lancez plusieurs instances de l'application
2. Sur une instance, démarrez le serveur
3. Sur les autres instances, connectez-vous au serveur
4. Effectuez des transferts entre les utilisateurs

## Licence

Ce projet est sous licence MIT.
