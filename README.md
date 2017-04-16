# About `Meziantou.Backup`

Backup your files from a source to a destination (File System, OneDrive, SFTP).
For instance you can copy your local files to your OneDrive account, or an external hard disk, or an USB flash drive.

# Usage

```cmd
Meziantou.Backup.Console.exe backup "d:\Data" "onedrive://backup/" --ignoreErrors --targetAesAskPassword
```

Use `--help` to show all options

# Providers

Currently, 3 file systems are supported: Local File System, OneDrive, SFTP. Each provider has specific configuration options.
Use `-sc conf=value` (source file system configuration) or `-tc setting=value` (target file system configuration) to set a setting value

For instance:
```cmd
Meziantou.Backup.Console.exe backup "d:\Data" "onedrive://backup/" -tc UploadChunkSize=1024
```

## FileSystem

Configuration:

- `Path` can be a relative path or a rooted path. Example: `C:\Users\meziantou\ToBeBackedUp\`

*Note: This provider supports long path (>260 characters)*

## OneDrive

Configuration:

- `Path`: Path from the root of the user's root path. The directory is created if needed. Example: `/Backup/Musics/`
- `ApplicationName` (optional, default `null`): This allows to store the credential (Refresh token) in the [Windows Credential Manager](http://windows.microsoft.com/en-us/windows7/what-is-credential-manager) so you don't need to enter your credential every time. Use a different name for different OneDrive account. The name is not related to your OneDrive account.
- `AuthenticateOnUnauthenticatedError` (optional, default `true`): Re-authenticate when the API call fails with error code `Unauthenticated`
- `UploadChunkSize` (optional, default 5MB): The size of the file chunks

## SFTP

Configuration:
    
- `Path`: Path from the root. The directory is created if needed. Example: `/Backup/sample/`
- `Host`: SFTP host (ex: sample.com)
- `Port` (optional, default `22`)
- `Username`
- `PrivateKeyFile`
- `Password`

# AES Encryption

You can encrypt file content and name (optional) using AES128 or AES256.

```cmd
--sourceAesPassword <PASSWORD>
--sourceAesPasswordName <NAME>
--sourceAesAskPassword
--sourceAesEncryptFileNames
--sourceAesEncryptDirectoryNames

--targetAesMethod <METHOD>
--targetAesPassword <PASSWORD>
--targetAesPasswordName <NAME>
--targetAesAskPassword
--targetAesEncryptFileNames
--targetAesEncryptDirectoryNames
```

- `AesMethod`: `Aes128` or `Aes256`
- `AesPassword`: Password to encrypt or decrypt
- `AesAskPassword`: Enter the password in the console
- `AesPasswordName`: Read the password from the creadential manager. If combined with `--aesAskPassword`, the password is prompted and save in the credential manager.
- `AesEncryptFileName`: Indicates whether file names must be encrypted
- `AesEncryptDirectoryName`: Indicates whether directory names must be encrypted

*Note 1: you can also decrypt files if you replace `target` by `source`*

*Note 2: The file length of an encrypted file cannot be computed correctly so you should not use it to compare files. Instead you may want to use the `LastWriteTime`: `EqualityMethods=LastWriteTime`*

# Examples

From the local file system to OneDrive:
```cmd
Meziantou.Backup.Console.exe "C:\Users\meziantou\ToBeBackedUp" "onedrive://Backup/meziantou/" -tc ApplicationName=Meziantou.Backup.OneDrive.Meziantou
```

From the local file system to OneDrive using `AES 256` with the password `123456`:
```cmd
Meziantou.Backup.Console.exe "C:\Users\meziantou\ToBeBackedUp" "onedrive://Backup/meziantou/" -tc ApplicationName="Meziantou.Backup.OneDrive.Meziantou" targetAesMethod=Aes256 targetAesPassword=123456 targetAesEncryptFileName=true targetAesEncryptDirectoryName=true EqualityMethods=LastWriteTime
```
