using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;

namespace PKNailForRevit
{
    internal static class CustomFailures
    {
        private static FailureDefinitionId m_unconvertedWallsWarningId = null;

        internal static FailureDefinitionId UnconvertedWallsWarningId
        {
            get
            {
                if (m_unconvertedWallsWarningId == null)
                { throw new HardException(@"Custom failures were not registered prior to use"); }
                return m_unconvertedWallsWarningId;
            }
        }

        internal static void Register()
        {
            m_unconvertedWallsWarningId = new FailureDefinitionId(new Guid(@"52041109-17E4-4B75-8D74-5A7222B9B22B"));
            FailureDefinition def = FailureDefinition.CreateFailureDefinition(m_unconvertedWallsWarningId, FailureSeverity.Warning,
                                                                              @"Some walls could not be converted and must be converted manually");
        }
    }
}
