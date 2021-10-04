using System;
using System.Reflection;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.NerdbankGitVersioning;

namespace DefaultNamespace
{
    public class GitVersionAttribute : NerdbankGitVersioningAttribute
    {
        public override object GetValue(MemberInfo member, object instance)
        {
            try
            {
                return base.GetValue(member, instance);
            }
            catch (ProcessException)
            {
            }

            return null;
        }
    }
}