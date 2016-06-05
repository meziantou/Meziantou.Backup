# About `Meziantou.Backup`

Copy your files from a source to a destination.
For instance you can copy your local files to your OneDrive account or an external disk.

# Usage

```cmd
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
- `Configuration-<name>`: Key-Value to configure the provider
- `EqualityMethods` (optional, default: `LastWriteTime | Length`): Methods to compare two files. Valid values: `Length`, `LastWriteTime`, `Content`, `ContentMd5`, `ContentSha1`, `ContentSha256`, `ContentSha512`
- `RetryCount` (optional, default: `3`): Number of attempts to execute a file operation
- `CanCreateDirectories` (optional, default: `true`)
- `CanDeleteDirectories` (optional, default: `false`)
- `CanCreateFiles` (optional, default: `true`)
- `CanUpdateFiles` (optional, default: `true`)
- `CanDeleteFiles` (optional, default: `false`)

# Providers

## FileSystem

Configuration:

- `Path` can be a relative path or a rooted path. Example: `C:\Users\meziantou\ToBeBackedUp\`

*Note: This provider supports long path (>260 characters)*

## OneDrive

Configuration:

- `Path`: Path from the root of the user's root path. The directory is created if needed. Example: `/Backup/Musics/`
- `Configuration-ApplicationName` (optional, default `null`): This allows to store the credential (Refresh token) in the [Windows Credential Manager](http://windows.microsoft.com/en-us/windows7/what-is-credential-manager) so you don't need to enter your credential every time. Use a different name for different OneDrive account. The name is not related to your OneDrive account.
- `Configuration-AuthenticateOnUnauthenticatedError` (optional, default `true`): Re-authenticate when the API call fails with error code `Unauthenticated`

## SFTP

Configuration:
    
- `Path`: Path from the root. The directory is created if needed. Example: `/Backup/Musics/`
- `Configuration-Host`: SFTP host (ex: sample.com)
- `Configuration-Port` (optional, default `22`)
- `Configuration-Username`
- `Configuration-Password`

## AES Encryption

You can encrypt file content and name (optional) using AES128 or AES256.

```cmd
targetAesMethod=Aes256 targetAesPassword=123456 targetAesEncryptFileName=true targetAesEncryptDirectoryName=true
```

- `AesMethod`: `Aes128` or `Aes256`
- `AesPassword`: Password to encrypt or decrypt
- `AesEncryptFileName`: Indicates whether file names must be encrypted
- `AesEncryptDirectoryName`: Indicates whether directory names must be encrypted

*Note 1: you can also decrypt files if you replace `target` by `source`*

*Note 2: The file length of an encrypted file cannot be computed correctly so you should not use it to compare to file. Instead you may want to use the `LastWriteTime`: `EqualityMethods=LastWriteTime`*


# Examples

From the local file system to OneDrive:
```cmd
Meziantou.Backup.Console.exe sourceProviderName=FileSystem sourcePath="C:\Users\meziantou\ToBeBackedUp" targetProviderName=OneDrive targetPath="/Backup/meziantou/" targetConfiguration-ApplicationName="Meziantou.Backup.OneDrive.Meziantou"
```

From the local file system to OneDrive using AES 256 with the password 123456:
```cmd
Meziantou.Backup.Console.exe sourceProviderName=FileSystem sourcePath="C:\Users\meziantou\ToBeBackedUp" targetProviderName=OneDrive targetPath="/Backup/meziantou/" targetConfiguration-ApplicationName="Meziantou.Backup.OneDrive.Meziantou" sourceProviderName=FileSystem sourcePath="C:\Users\meziantou\Desktop\New folder" targetProviderName=FileSystem targetPath="C:\Users\meziantou\Desktop\New folder - Backup" targetAesMethod=Aes256 targetAesPassword=123456 targetAesEncryptFileName=true targetAesEncryptDirectoryName=true EqualityMethods=LastWriteTime
```


