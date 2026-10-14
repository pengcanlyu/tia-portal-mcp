using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siemens.Engineering.SW.Tags;
using System.Xml.Linq;

namespace TiaMcpServer.OpennessWorker.Openness.OpcUa.SiemensGenerator
{
    internal static class UserConstants
    {
        // Dictionary to map the name of a user constant with its value
        public static Dictionary<string, int> UserConstantValues = new Dictionary<string, int>();

        /// <summary>
        /// Stores all user constants in a dictionary with their respective values.
        /// Note: User constants are defined inside each tag table of the project.
        /// </summary>
        /// <param name="tagTableGroup"></param>
        public static void GetUserConstants(PlcTagTableGroup tagTableGroup)
        {
            UserConstantValues.Clear();
            CollectUserConstants(tagTableGroup);
        }

        private static void CollectUserConstants(PlcTagTableGroup tagTableGroup)
        {
            foreach (PlcTagTable table in tagTableGroup.TagTables)
            {
                foreach (PlcUserConstant userConstant in table.UserConstants)
                {
                    string userConstantName = userConstant.Name;
                    // Try to parse the string into an int
                    if (int.TryParse(userConstant.Value, out int value))
                    {
                        // Parsing was successful
                        UserConstantValues[userConstantName] = value;
                    }
                }
            }
            foreach (PlcTagTableGroup tableGroup in tagTableGroup.Groups)
            {
                CollectUserConstants(tableGroup);
            }
        }
    }
}
