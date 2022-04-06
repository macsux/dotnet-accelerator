using System;
using Nuke.Common;
using Nuke.Common.Utilities.Collections;
using Serilog;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem
namespace Extensions
{
    public static class SerilogExtensions
    {
        public static void Block(this ILogger logger, string text)
        {
            var formattedBlockText = text
                .Split(new[] { EnvironmentInfo.NewLine }, StringSplitOptions.None);

            logger.Information("");
            logger.Information("╬" + new string(c: '═', text.Length + 5));
            formattedBlockText.ForEach(x => logger.Information($"║ {x}"));
            logger.Information("╬" + new string(c: '═', Math.Max(text.Length - 4, 2)));
            logger.Information("");
        }
    }
}
