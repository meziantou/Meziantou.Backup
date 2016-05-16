namespace Meziantou.OneDrive
{
    internal class Credential
    {
        private readonly string _applicationName;
        private readonly string _userName;
        private readonly string _password;
        private readonly CredentialType _credentialType;

        public CredentialType CredentialType
        {
            get { return _credentialType; }
        }

        public string ApplicationName
        {
            get { return _applicationName; }
        }

        public string UserName
        {
            get { return _userName; }
        }

        public string Password
        {
            get { return _password; }
        }

        public Credential(CredentialType credentialType, string applicationName, string userName, string password)
        {
            _applicationName = applicationName;
            _userName = userName;
            _password = password;
            _credentialType = credentialType;
        }

        public override string ToString()
        {
            return string.Format("CredentialType: {0}, ApplicationName: {1}, UserName: {2}, Password: {3}", CredentialType, ApplicationName, UserName, Password);
        }
    }
}