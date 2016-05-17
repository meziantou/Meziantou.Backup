# About `Meziantou.Backup`

Allows to backup your files from a source to a destination.
For instance you can save your local files to your OneDrive account or an external disk.

# Usage

```
Meziantou.Backup.Console.exe
    sourceProviderName=<provider name>
    sourcePath=<path>
    sourceConfiguration-<name>=<value>
    targetProviderName=<provider name>
    targetPath=<path>
    targetConfiguration-<name>=<value>
```

- `ProviderName`: Currently it supports `FileSystem` and `OneDrive`
- `Path`: Path to the directory to synchronize
- `Configuration-<name>`: Key-Value to configure the provider.

# Providers

## FileSystem

Configuration:

- `Path` can be a relative path or a rooted path. Example: `C:\Users\meziantou\ToBeBackedUp\`

*Note: This provider supports long path (>260 characters)*

## OneDrive

Configuration:

- `Path`: Path from the root of the user's root path. The directory is created if needed. Example: `/Backup/Musics/`
- `Configuration-ApplicationName` (optional): This allows to store the credential (Refresh token) in the [Windows Credential Manager](http://windows.microsoft.com/en-us/windows7/what-is-credential-manager) so you don't need to enter your credential every time. Use a different name for different OneDrive account. The name is not related to your OneDrive account.

# Examples

```
Meziantou.Backup.Console.exe sourceProviderName=FileSystem sourcePath="C:\Users\meziantou\ToBeBackedUp" targetProviderName=OneDrive targetPath="/Backup/meziantou/" targetConfiguration-ApplicationName="Meziantou.Backup.OneDrive.Meziantou"
```

Copy files from the local hard drive (directory `C:\Users\meziantou\ToBeBackedUp`) to OneDrive (directory `/Backup/meziantou/`) using the credential saved with the name `Meziantou.Backup.OneDrive.Meziantou`