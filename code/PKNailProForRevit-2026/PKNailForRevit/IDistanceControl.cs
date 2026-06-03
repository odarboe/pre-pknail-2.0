using System;
using System.Collections.Generic;
using System.Text;

namespace PKNailForRevit
{
    [System.Reflection.Obfuscation(Feature="renaming", ApplyToMembers=true)]
    internal interface IDistanceControl
    {
        Distance RoundingIncrement  { get; set; }
        Distance DistanceValue  { get; }
        Distance DistanceValueNotRounded  { get; }
        bool IsDirty  { get; }
        bool IsDistanceNoValue  { get; }

        void SetDistanceClean(Distance d);
        void SetDistanceDirty(Distance d);
        void ClearDistance();   // Set to clean
    }
}
