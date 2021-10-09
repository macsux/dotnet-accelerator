using Steeltoe.Extensions.Logging;

namespace MyProjectGroup.Common
{
    public class NullLogProcessor : IDynamicMessageProcessor
    {
        public string Process(string inputLogMessage) => inputLogMessage;
    }
}