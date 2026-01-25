namespace BedrockLauncher.UpdateProcessor.Authentication
{
    public class AuthenticationManager
    {

        public static AuthenticationManager Default { get; set; } = new AuthenticationManager();

        public string GetWUToken(int relativeIndex)
        {
            int index = relativeIndex - 1;
            if (index <= -1) return string.Empty;

            string token;
            int status = AuthenticationTokenHelper.GetWUToken(index, out token);
            AuthenticationTokenException.Test(status);
            return token;
        }
    }
}
