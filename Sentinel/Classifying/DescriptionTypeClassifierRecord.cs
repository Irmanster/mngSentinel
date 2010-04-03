#region License
//
// � Copyright Ray Hayes
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.
//
#endregion

namespace Sentinel.Classifying
{
    public class DescriptionTypeClassifierRecord
    {
        public DescriptionTypeClassifierRecord(string type, string description)
        {
            Type = type;
            Description = description;
        }

        public string Description { get; private set; }

        public string Type { get; private set; }
    }
}