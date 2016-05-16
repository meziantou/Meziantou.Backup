using System;

namespace Meziantou.OneDrive
{
    public class RefreshTokenInfo
    {
        public RefreshTokenInfo(string refreshToken)
        {
            if (refreshToken == null) throw new ArgumentNullException(nameof(refreshToken));
            RefreshToken = refreshToken;
        }

        public string RefreshToken { get; }
    }
}