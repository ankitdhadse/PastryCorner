
namespace PastryCorner.Contracts.Definitions
{
    using System.ComponentModel;

    public enum ServerMessagesType
    {
        None = 1,
        [Description("Error: Creating Server Messages")]
        Error = 2,
        [Description("Client Version Deprecated")]
        ClientVersionDeprecated = 4
    }
}
