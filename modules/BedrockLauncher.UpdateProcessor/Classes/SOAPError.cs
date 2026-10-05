using System;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public class SOAPError : Exception
    {
        public string code;
        public SOAPError(string code) : base($"SOAP Error: {code}")
        {
            this.code = code;
        }
    };
}
